using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Helpers;
using AutoMapper;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BilletController : ControllerBase
    {
        private readonly IBilletRepository _billetRepository;
        private readonly IBilletPricingEnrichmentService _billetPricingEnrichment;
        private readonly IMapper _mapper;
        private readonly ILogger<BilletController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public BilletController(
            IBilletRepository billetRepository,
            IBilletPricingEnrichmentService billetPricingEnrichment,
            IMapper mapper,
            ILogger<BilletController> logger,
            ICurrentUserService currentUserService)
        {
            _billetRepository = billetRepository;
            _billetPricingEnrichment = billetPricingEnrichment;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        private async Task<BilletResponseDto> MapBilletResponseAsync(Billet billet)
        {
            var dto = _mapper.Map<BilletResponseDto>(billet);
            await _billetPricingEnrichment.EnrichPrixVoyageAsync(new[] { billet }, new List<BilletResponseDto> { dto });
            return dto;
        }

        private async Task<List<BilletResponseDto>> MapBilletResponsesAsync(IEnumerable<Billet> billets)
        {
            var list = billets.ToList();
            var dtos = _mapper.Map<List<BilletResponseDto>>(list);
            await _billetPricingEnrichment.EnrichPrixVoyageAsync(list, dtos);
            return dtos;
        }

        // GET: api/billet
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BilletResponseDto>>> GetAll([FromQuery] int? idSociete = null)
        {
            try
            {
                var societeId = TenantGuard.ResolveListSocieteId(
                    _currentUserService.SocieteId,
                    _currentUserService.IsSuperAdmin,
                    idSociete);
                var billets = await _billetRepository.GetAllBySocieteAsync(societeId);
                return Ok(await MapBilletResponsesAsync(billets));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les billets");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Enregistre l’embarquement après scan du billet (met <c>IsUsed</c> et historise).</summary>
        [HttpPost("societe/{idSociete:int}/passager/{idReservationPassenger:int}/billet/{idBillet:int}/embarquer")]
        public async Task<ActionResult<EmbarquerBilletResponseDto>> EnregistrerEmbarquement(
            int idSociete,
            int idReservationPassenger,
            int idBillet,
            [FromQuery] int? idVoyageCible = null)
        {
            try
            {
                int? idUtilisateur = null;
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (int.TryParse(claim, out var uid))
                    idUtilisateur = uid;

                var result = await _billetRepository.EnregistrerEmbarquementAsync(
                    idSociete, idBillet, idReservationPassenger, idVoyageCible, idUtilisateur);

                if (!result.Success)
                    return StatusCode(result.StatusCode, new { message = result.Message });

                if (result.Billet == null || result.Embarquement == null)
                    return StatusCode(500, new { message = "Réponse incohérente après embarquement." });

                var dto = new EmbarquerBilletResponseDto
                {
                    IdEmbarquement = result.Embarquement.IdEmbarquement,
                    DateEmbarquementUtc = result.Embarquement.DateEmbarquementUtc,
                    IdUtilisateurEnregistrement = result.Embarquement.IdUtilisateurEnregistrement,
                    Billet = await MapBilletResponseAsync(result.Billet)
                };
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur embarquement société {IdSociete}, billet {IdBillet}, passager {IdPassager}",
                    idSociete, idBillet, idReservationPassenger);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Vérifie si un billet est encore valide (<see cref="Billet.IsUsed"/>) ou déjà utilisé ; billet inconnu = message type contrefaçon. Résolution par <see cref="Billet.QrCode"/> (égalité exacte).</summary>
        [HttpGet("{QrCode}/check")]
        [ProducesResponseType(typeof(BilletCheckResponseDto), 200)]
        public async Task<ActionResult<BilletCheckResponseDto>> CheckBillet(string QrCode, [FromQuery] int? idVoyageCible = null)
        {
            try
            {
                var dto = await _billetRepository.CheckBilletByQrCodeAsync(QrCode, idVoyageCible);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du contrôle du billet (QrCode)");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpPost("societe/{idSociete:int}/billet/{idBillet:int}/reaffecter")]
        public async Task<ActionResult<object>> ReaffecterBillet(
            int idSociete,
            int idBillet,
            [FromBody] ReaffecterBilletRequestDto request)
        {
            try
            {
                int? idUtilisateur = null;
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (int.TryParse(claim, out var uid))
                    idUtilisateur = uid;

                var result = await _billetRepository.ReaffecterBilletAsync(
                    idSociete,
                    idBillet,
                    request.IdVoyageCible,
                    idUtilisateur,
                    request.ConfirmerPaiementDifferentiel,
                    request.MethodePaiement,
                    request.ReferenceTransaction,
                    request.Commentaire);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new
                    {
                        message = result.Message,
                        differentielTarifaire = result.DifferentielTarifaire,
                        penalite = result.Penalite,
                        penaliteAppliquee = result.PenaliteAppliquee,
                        penalitePourcentageApplique = result.PenalitePourcentageApplique,
                        montantPayeReference = result.MontantPayeReference,
                        montantTotalRegularisation = result.MontantTotalRegularisation,
                        heuresLimiteReaffectation = result.HeuresLimiteReaffectation,
                        departVoyageSource = result.DepartVoyageSource,
                        deadlineReaffectation = result.DeadlineReaffectation,
                        paiementDifferentielRequis = result.PaiementDifferentielRequis,
                        paiementDifferentielConfirme = result.PaiementDifferentielConfirme
                    });
                }

                if (result.Billet == null)
                    return StatusCode(500, new { message = "Réaffectation effectuée mais billet introuvable au rechargement." });

                return Ok(new
                {
                    message = result.Message,
                    differentielTarifaire = result.DifferentielTarifaire,
                    penalite = result.Penalite,
                    penaliteAppliquee = result.PenaliteAppliquee,
                    penalitePourcentageApplique = result.PenalitePourcentageApplique,
                    montantPayeReference = result.MontantPayeReference,
                    montantTotalRegularisation = result.MontantTotalRegularisation,
                    heuresLimiteReaffectation = result.HeuresLimiteReaffectation,
                    departVoyageSource = result.DepartVoyageSource,
                    deadlineReaffectation = result.DeadlineReaffectation,
                    paiementDifferentielRequis = result.PaiementDifferentielRequis,
                    paiementDifferentielConfirme = result.PaiementDifferentielConfirme,
                    billet = await MapBilletResponseAsync(result.Billet)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réaffectation du billet {IdBillet}", idBillet);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BilletResponseDto>> GetById(int id)
        {
            try
            {
                var billet = await _billetRepository.GetByIdAsync(id);
                if (billet == null)
                    return NotFound(new { message = $"Billet avec l'ID {id} non trouvé" });

                return Ok(await MapBilletResponseAsync(billet));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du billet {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/paged
        [HttpPost("paged")]
        public async Task<ActionResult<PagedResult<BilletResponseDto>>> GetPaged([FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _billetRepository.GetPagedAsync(request);
                var pagedDtos = _mapper.Map<PagedResult<BilletResponseDto>>(pagedResult);
                await _billetPricingEnrichment.EnrichPrixVoyageAsync(
                    pagedResult.Data.ToList(),
                    pagedDtos.Data.ToList());
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/reservation/{idReservation}
        [HttpGet("reservation/{idReservation}")]
        public async Task<ActionResult<IEnumerable<BilletResponseDto>>> GetByReservation(int idReservation)
        {
            try
            {
                var billets = await _billetRepository.GetByReservationAsync(idReservation);
                return Ok(await MapBilletResponsesAsync(billets));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets pour la réservation {idReservation}", idReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/qrcode/{qrCode}
        [HttpGet("qrcode/{qrCode}")]
        public async Task<ActionResult<IEnumerable<BilletResponseDto>>> GetByQrCode(string qrCode)
        {
            try
            {
                var list = (await _billetRepository.GetByQrCodeAsync(qrCode)).ToList();
                var dtos = await MapBilletResponsesAsync(list);
                for (var i = 0; i < list.Count; i++)
                    BilletPassengerIdentityCompat.ApplyPassengerIdentityToClientFields(dtos[i], list[i]);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets avec QR Code {qrCode}", qrCode);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/date/{date}
        [HttpGet("date/{date}")]
        public async Task<ActionResult<IEnumerable<BilletResponseDto>>> GetByDate(DateTime date)
        {
            try
            {
                var billets = await _billetRepository.GetByDateGenerationAsync(date);
                return Ok(await MapBilletResponsesAsync(billets));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/daterange
        [HttpGet("daterange")]
        public async Task<ActionResult<IEnumerable<BilletResponseDto>>> GetByDateRange([FromQuery] DateTime dateDebut, [FromQuery] DateTime dateFin)
        {
            try
            {
                var billets = await _billetRepository.GetByDateRangeAsync(dateDebut, dateFin);
                return Ok(await MapBilletResponsesAsync(billets));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des billets entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // POST: api/billet
      /*
        [HttpPost]
        public async Task<ActionResult<BilletResponseDto>> Create([FromBody] CreateBilletDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var billet = _mapper.Map<Billet>(createDto);
                var createdBillet = await _billetRepository.CreateAsync(billet);
                var resultDto = _mapper.Map<BilletResponseDto>(createdBillet);
                
                return CreatedAtAction(nameof(GetById), new { id = resultDto.IdBillet }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du billet");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
        
        */

        // PUT: api/billet/{id}
      
        /*
         [HttpPut("{id}")]
        public async Task<ActionResult<BilletResponseDto>> Update(int id, [FromBody] UpdateBilletDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != updateDto.IdBillet)
                    return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });

                var billet = _mapper.Map<Billet>(updateDto);
                var updatedBillet = await _billetRepository.UpdateAsync(billet);
                
                if (updatedBillet == null)
                    return NotFound(new { message = $"Billet avec l'ID {id} non trouvé" });

                var resultDto = _mapper.Map<BilletResponseDto>(updatedBillet);
                return Ok(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du billet {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
        */

        // DELETE: api/billet/{id}
       /*
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _billetRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Billet avec l'ID {id} non trouvé" });

                return Ok(new { message = "Billet supprimé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du billet {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
        */

        // GET: api/billet/reservation/{idReservation}/paged
        [HttpPost("reservation/{idReservation}/paged")]
        public async Task<ActionResult<PagedResult<BilletResponseDto>>> GetByReservationPaged(int idReservation, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _billetRepository.GetByReservationPagedAsync(idReservation, request);
                var pagedDtos = _mapper.Map<PagedResult<BilletResponseDto>>(pagedResult);
                await _billetPricingEnrichment.EnrichPrixVoyageAsync(
                    pagedResult.Data.ToList(),
                    pagedDtos.Data.ToList());
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets pour la réservation {idReservation}", idReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/date/{date}/paged
        [HttpPost("date/{date}/paged")]
        public async Task<ActionResult<PagedResult<BilletResponseDto>>> GetByDatePaged(DateTime date, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _billetRepository.GetByDateGenerationPagedAsync(date, request);
                var pagedDtos = _mapper.Map<PagedResult<BilletResponseDto>>(pagedResult);
                await _billetPricingEnrichment.EnrichPrixVoyageAsync(
                    pagedResult.Data.ToList(),
                    pagedDtos.Data.ToList());
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des billets pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount()
        {
            try
            {
                var count = await _billetRepository.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/reservation/{idReservation}/count
        [HttpGet("reservation/{idReservation}/count")]
        public async Task<ActionResult<int>> GetCountByReservation(int idReservation)
        {
            try
            {
                var count = await _billetRepository.CountByReservationAsync(idReservation);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets pour la réservation {idReservation}", idReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/date/{date}/count
        [HttpGet("date/{date}/count")]
        public async Task<ActionResult<int>> GetCountByDate(DateTime date)
        {
            try
            {
                var count = await _billetRepository.CountByDateGenerationAsync(date);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/billet/daterange/count
        [HttpGet("daterange/count")]
        public async Task<ActionResult<int>> GetCountByDateRange([FromQuery] DateTime dateDebut, [FromQuery] DateTime dateFin)
        {
            try
            {
                var count = await _billetRepository.CountByDateRangeAsync(dateDebut, dateFin);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des billets entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
