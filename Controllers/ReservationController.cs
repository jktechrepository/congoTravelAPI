using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Helpers;
using CongoTravel.Data;
using CongoTravel.Helpers;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReservationController : ControllerBase
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IBilletRepository _billetRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ReservationController> _logger;
        private readonly ICashReservationWithPaiementService _cashReservationWithPaiementService;
        private readonly IFlexPayReservationService _flexPayReservationService;
        private readonly IAllerRetourReservationService _allerRetourReservationService;
        private readonly IReservationWithPaiementReadService _reservationWithPaiementReadService;
        private readonly IBilletPricingEnrichmentService _billetPricingEnrichment;
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public ReservationController(
            IReservationRepository reservationRepository,
            IBilletRepository billetRepository,
            IMapper mapper,
            ILogger<ReservationController> logger,
            ICashReservationWithPaiementService cashReservationWithPaiementService,
            IFlexPayReservationService flexPayReservationService,
            IAllerRetourReservationService allerRetourReservationService,
            IReservationWithPaiementReadService reservationWithPaiementReadService,
            IBilletPricingEnrichmentService billetPricingEnrichment,
            CongoTravelDbContext context,
            ICurrentUserService currentUserService)
        {
            _reservationRepository = reservationRepository;
            _billetRepository = billetRepository;
            _mapper = mapper;
            _logger = logger;
            _cashReservationWithPaiementService = cashReservationWithPaiementService;
            _flexPayReservationService = flexPayReservationService;
            _allerRetourReservationService = allerRetourReservationService;
            _reservationWithPaiementReadService = reservationWithPaiementReadService;
            _billetPricingEnrichment = billetPricingEnrichment;
            _context = context;
            _currentUserService = currentUserService;
        }

        // GET: api/reservation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetAll([FromQuery] int? idSociete = null)
        {
            try
            {
                var societeId = TenantGuard.ResolveListSocieteId(
                    _currentUserService.SocieteId,
                    _currentUserService.IsSuperAdmin,
                    idSociete);
                var reservations = await _reservationRepository.GetAllBySocieteAsync(societeId);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les réservations");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationWithPaiementResponseDto>> GetById(int id)
        {
            try
            {
                var response = await _reservationWithPaiementReadService.BuildByReservationIdAsync(
                    id,
                    transactionId: $"GET-RES-{id}",
                    message: "Réservation récupérée avec succès");

                if (response == null)
                {
                    var reservation = await _reservationRepository.GetByIdAsync(id);
                    if (reservation == null)
                        return NotFound(new { message = $"Réservation avec l'ID {id} non trouvée" });

                    return NotFound(new
                    {
                        message = $"Aucun paiement associé à la réservation {id}.",
                        reservationId = id
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la réservation {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/{id}/passagers
        /// <summary>
        /// Passagers liés à la réservation (workflow V2).
        /// </summary>
        [HttpGet("{id:int}/passagers")]
        [ProducesResponseType(typeof(IReadOnlyList<ReservationPassengerReadDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<ReservationPassengerReadDto>>> GetPassagers(int id)
        {
            try
            {
                if (!await _reservationRepository.ExistsAsync(id))
                    return NotFound(new { message = $"Réservation avec l'ID {id} non trouvée" });

                var passagers = await _reservationRepository.GetPassagersByReservationAsync(id);
                var dtos = _mapper.Map<List<ReservationPassengerReadDto>>(passagers);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des passagers pour la réservation {ReservationId}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/Societe/{idSociete}
        /// <summary>
        /// Liste des réservations d'une société, avec les passagers (workflow V2).
        /// </summary>
        [HttpGet("Societe/{idSociete:int}")]
        [ProducesResponseType(typeof(IReadOnlyList<ReservationResponseDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<ReservationResponseDto>>> GetBySociete(int idSociete)
        {
            try
            {
                var reservations = await _reservationRepository.GetBySocieteWithPassagersAsync(idSociete);
                if (reservations == null)
                {
                    return NotFound(new
                    {
                        message = $"Société {idSociete} introuvable."
                    });
                }

                var dtos = new List<ReservationResponseDto>(reservations.Count);
                foreach (var r in reservations)
                {
                    var dto = _mapper.Map<ReservationResponseDto>(r);
                    dto.Passagers = r.Passagers != null && r.Passagers.Count > 0
                        ? _mapper.Map<List<ReservationPassengerReadDto>>(r.Passagers.OrderBy(p => p.IdReservationPassenger))
                        : new List<ReservationPassengerReadDto>();
                    dtos.Add(dto);
                }

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des réservations pour la société {IdSociete}",
                    idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/Societe/{idSociete}/voyage/{idVoyage}
        /// <summary>
        /// Liste des réservations d’un voyage pour une société, avec les passagers (workflow V2).
        /// </summary>
        [HttpGet("Societe/{idSociete:int}/voyage/{idVoyage:int}")]
        [ProducesResponseType(typeof(IReadOnlyList<ReservationResponseDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<ReservationResponseDto>>> GetBySocieteAndVoyage(
            int idSociete,
            int idVoyage)
        {
            try
            {
                var reservations = await _reservationRepository.GetBySocieteAndVoyageWithPassagersAsync(idSociete, idVoyage);
                if (reservations == null)
                {
                    return NotFound(new
                    {
                        message = $"Voyage {idVoyage} introuvable ou n'appartient pas à la société {idSociete}."
                    });
                }

                var dtos = new List<ReservationResponseDto>(reservations.Count);
                foreach (var r in reservations)
                {
                    var dto = _mapper.Map<ReservationResponseDto>(r);
                    dto.Passagers = r.Passagers != null && r.Passagers.Count > 0
                        ? _mapper.Map<List<ReservationPassengerReadDto>>(r.Passagers.OrderBy(p => p.IdReservationPassenger))
                        : new List<ReservationPassengerReadDto>();
                    dtos.Add(dto);
                }

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des réservations société {IdSociete} voyage {IdVoyage}",
                    idSociete, idVoyage);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/{id}/billets
        /// <summary>
        /// Billets associés à la réservation (siège / passager si workflow V2).
        /// </summary>
        [HttpGet("{id:int}/billets")]
        [ProducesResponseType(typeof(IEnumerable<CongoTravel.Models.DTOs.BilletResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<CongoTravel.Models.DTOs.BilletResponseDto>>> GetBilletsForReservation(int id)
        {
            try
            {
                if (!await _reservationRepository.ExistsAsync(id))
                    return NotFound(new { message = $"Réservation avec l'ID {id} non trouvée" });

                var billets = await _billetRepository.GetByReservationAsync(id);
                var billetsList = billets.ToList();
                var dtos = _mapper.Map<List<CongoTravel.Models.DTOs.BilletResponseDto>>(billetsList);
                await _billetPricingEnrichment.EnrichPrixVoyageAsync(billetsList, dtos);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des billets pour la réservation {ReservationId}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/paged
        [HttpPost("paged")]
        public async Task<ActionResult<PagedResult<ReservationResponseDto>>> GetPaged([FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _reservationRepository.GetPagedAsync(request);
                var pagedDtos = _mapper.Map<PagedResult<ReservationResponseDto>>(pagedResult);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/utilisateur/{idUtilisateur}
        [HttpGet("utilisateur/{idUtilisateur}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByUtilisateur(int idUtilisateur)
        {
            try
            {
                var reservations = await _reservationRepository.GetByUtilisateurAsync(idUtilisateur);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour l'utilisateur {idUtilisateur}", idUtilisateur);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/client/{idClient}
        /// <summary>Réservations du client avec passagers (workflow V2) et infos client.</summary>
        [HttpGet("client/{idClient}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByClient(int idClient)
        {
            try
            {
                var reservations = await _reservationRepository.GetByClientAsync(idClient);
                return Ok(MapReservationsWithPassagers(reservations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le client {idClient}", idClient);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/voyage/{idVoyage}
        [HttpGet("voyage/{idVoyage}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByVoyage(int idVoyage)
        {
            try
            {
                var reservations = await _reservationRepository.GetByVoyageAsync(idVoyage);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le voyage {idVoyage}", idVoyage);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/statutreservation/{statutReservation}
        [HttpGet("statutreservation/{statutReservation}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByStatutReservation(string statutReservation)
        {
            try
            {
                var reservations = await _reservationRepository.GetByStatutReservationAsync(statutReservation);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations avec statut {statutReservation}", statutReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/date/{date}
        [HttpGet("date/{date}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByDate(DateTime date)
        {
            try
            {
                var reservations = await _reservationRepository.GetByDateAsync(date);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/daterange
        [HttpGet("daterange")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByDateRange([FromQuery] DateTime dateDebut, [FromQuery] DateTime dateFin)
        {
            try
            {
                var reservations = await _reservationRepository.GetByDateRangeAsync(dateDebut, dateFin);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/utilisateur/{idUtilisateur}/client/{idClient}
        [HttpGet("utilisateur/{idUtilisateur}/client/{idClient}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByUtilisateurAndClient(int idUtilisateur, int idClient)
        {
            try
            {
                var reservations = await _reservationRepository.GetByUtilisateurAndClientAsync(idUtilisateur, idClient);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour l'utilisateur {idUtilisateur} et le client {idClient}", idUtilisateur, idClient);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/voyage/{idVoyage}/statut/{statutReservation}
        [HttpGet("voyage/{idVoyage}/statut/{statutReservation}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByVoyageAndStatut(int idVoyage, string statutReservation)
        {
            try
            {
                var reservations = await _reservationRepository.GetByVoyageAndStatutAsync(idVoyage, statutReservation);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations pour le voyage {idVoyage} et statut {statutReservation}", idVoyage, statutReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/statut/{statut}
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetByStatut(bool statut)
        {
            try
            {
                var reservations = await _reservationRepository.GetByStatutAsync(statut);
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations avec statut {statut}", statut);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetActive()
        {
            try
            {
                var reservations = await _reservationRepository.GetActiveAsync();
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations actives");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/inactive
        [HttpGet("inactive")]
        public async Task<ActionResult<IEnumerable<ReservationResponseDto>>> GetInactive()
        {
            try
            {
                var reservations = await _reservationRepository.GetInactiveAsync();
                var reservationDtos = _mapper.Map<IEnumerable<ReservationResponseDto>>(reservations);
                return Ok(reservationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des réservations inactives");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // POST: api/reservation
        [HttpPost]
        public async Task<ActionResult<ReservationResponseDto>> Create([FromBody] CreateReservationDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (createDto.IdSite.HasValue)
                {
                    try
                    {
                        await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                            _context, createDto.IdSite, createDto.IdSociete);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                }

                var reservation = _mapper.Map<Reservation>(createDto);
                reservation.Origine = OrigineOperationResolver.Resolve(_currentUserService);
                var createdReservation = await _reservationRepository.CreateAsync(reservation);
                var resultDto = _mapper.Map<ReservationResponseDto>(createdReservation);
                
                return CreatedAtAction(nameof(GetById), new { id = resultDto.IdReservation }, resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la réservation");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // PUT: api/reservation/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ReservationResponseDto>> Update(int id, [FromBody] UpdateReservationDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != updateDto.IdReservation)
                    return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });

                if (updateDto.IdSite.HasValue)
                {
                    try
                    {
                        await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                            _context, updateDto.IdSite, updateDto.IdSociete);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                }

                var reservation = _mapper.Map<Reservation>(updateDto);
                var updatedReservation = await _reservationRepository.UpdateAsync(reservation);
                
                if (updatedReservation == null)
                    return NotFound(new { message = $"Réservation avec l'ID {id} non trouvée" });

                var resultDto = _mapper.Map<ReservationResponseDto>(updatedReservation);
                return Ok(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la réservation {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // DELETE: api/reservation/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _reservationRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Réservation avec l'ID {id} non trouvée" });

                return Ok(new { message = "Réservation supprimée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la réservation {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/utilisateur/{idUtilisateur}/paged
        [HttpPost("utilisateur/{idUtilisateur}/paged")]
        public async Task<ActionResult<PagedResult<ReservationResponseDto>>> GetByUtilisateurPaged(int idUtilisateur, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _reservationRepository.GetByUtilisateurPagedAsync(idUtilisateur, request);
                var pagedDtos = _mapper.Map<PagedResult<ReservationResponseDto>>(pagedResult);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour l'utilisateur {idUtilisateur}", idUtilisateur);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/client/{idClient}/paged
        [HttpPost("client/{idClient}/paged")]
        public async Task<ActionResult<PagedResult<ReservationResponseDto>>> GetByClientPaged(int idClient, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _reservationRepository.GetByClientPagedAsync(idClient, request);
                var dtos = MapReservationsWithPassagers(pagedResult.Data);
                var pagedDtos = new PagedResult<ReservationResponseDto>(
                    dtos,
                    pagedResult.TotalCount,
                    pagedResult.PageNumber,
                    pagedResult.PageSize);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour le client {idClient}", idClient);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/voyage/{idVoyage}/paged
        [HttpPost("voyage/{idVoyage}/paged")]
        public async Task<ActionResult<PagedResult<ReservationResponseDto>>> GetByVoyagePaged(int idVoyage, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _reservationRepository.GetByVoyagePagedAsync(idVoyage, request);
                var pagedDtos = _mapper.Map<PagedResult<ReservationResponseDto>>(pagedResult);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations pour le voyage {idVoyage}", idVoyage);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/statutreservation/{statutReservation}/paged
        [HttpPost("statutreservation/{statutReservation}/paged")]
        public async Task<ActionResult<PagedResult<ReservationResponseDto>>> GetByStatutReservationPaged(string statutReservation, [FromBody] PagedRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var pagedResult = await _reservationRepository.GetByStatutReservationPagedAsync(statutReservation, request);
                var pagedDtos = _mapper.Map<PagedResult<ReservationResponseDto>>(pagedResult);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des réservations avec statut {statutReservation}", statutReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount()
        {
            try
            {
                var count = await _reservationRepository.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/utilisateur/{idUtilisateur}/count
        [HttpGet("utilisateur/{idUtilisateur}/count")]
        public async Task<ActionResult<int>> GetCountByUtilisateur(int idUtilisateur)
        {
            try
            {
                var count = await _reservationRepository.CountByUtilisateurAsync(idUtilisateur);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour l'utilisateur {idUtilisateur}", idUtilisateur);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/client/{idClient}/count
        [HttpGet("client/{idClient}/count")]
        public async Task<ActionResult<int>> GetCountByClient(int idClient)
        {
            try
            {
                var count = await _reservationRepository.CountByClientAsync(idClient);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour le client {idClient}", idClient);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/voyage/{idVoyage}/count
        [HttpGet("voyage/{idVoyage}/count")]
        public async Task<ActionResult<int>> GetCountByVoyage(int idVoyage)
        {
            try
            {
                var count = await _reservationRepository.CountByVoyageAsync(idVoyage);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour le voyage {idVoyage}", idVoyage);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/statutreservation/{statutReservation}/count
        [HttpGet("statutreservation/{statutReservation}/count")]
        public async Task<ActionResult<int>> GetCountByStatutReservation(string statutReservation)
        {
            try
            {
                var count = await _reservationRepository.CountByStatutReservationAsync(statutReservation);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations avec statut {statutReservation}", statutReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/date/{date}/count
        [HttpGet("date/{date}/count")]
        public async Task<ActionResult<int>> GetCountByDate(DateTime date)
        {
            try
            {
                var count = await _reservationRepository.CountByDateAsync(date);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/statut/{statut}/count
        [HttpGet("statut/{statut}/count")]
        public async Task<ActionResult<int>> GetCountByStatut(bool statut)
        {
            try
            {
                var count = await _reservationRepository.CountByStatutAsync(statut);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations avec statut {statut}", statut);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/active/count
        [HttpGet("active/count")]
        public async Task<ActionResult<int>> GetCountActive()
        {
            try
            {
                var count = await _reservationRepository.CountActiveAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations actives");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/reservation/inactive/count
        [HttpGet("inactive/count")]
        public async Task<ActionResult<int>> GetCountInactive()
        {
            try
            {
                var count = await _reservationRepository.CountInactiveAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des réservations inactives");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // POST: api/reservation/reservation_with_paiement
        /// <summary>
        /// Crée une réservation avec paiement (workflow V2) : passagers, attribution automatique des sièges,
        /// paiement et émission d’un billet par passager si le paiement est complet.
        /// </summary>
        /// <param name="dto">Réservation (<c>nombreDePlace</c>, <c>passagers</c> requis avec <c>idCategorieSiege</c>), paiement</param>
        /// <returns>Réservation, paiement, <c>billets</c> et <c>billet</c> (premier billet, compatibilité)</returns>
        [HttpPost("reservation_with_paiement")]
        [HttpPost("with-passengers-and-paiement")]
        [ProducesResponseType(typeof(ReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ReservationWithPaiementResponseDto>> CreateReservationWithPaiement(
            [FromBody] CreateReservationWithPaiementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalide pour la création de réservation avec paiement");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Début création réservation avec paiement - Voyage: {VoyageId}, Client: {ClientId}, Montant: {Montant}", 
                    dto.Reservation.IdVoyage, dto.Reservation.IdClient, dto.Paiement.MontantPaye);

                var result = await _cashReservationWithPaiementService.CreateAsync(dto);

                if (result.Statut == TransactionStatut.Echec)
                {
                    _logger.LogError("Échec de la création de réservation avec paiement - TransactionID: {TransactionId}", result.TransactionId);
                    return StatusCode(500, result);
                }

                _logger.LogInformation("Réservation avec paiement créée avec succès - TransactionID: {TransactionId}, Réservation: {ReservationId}, Paiement: {PaiementId}", 
                    result.TransactionId, result.Reservation.IdReservation, result.Paiement.IdPaiement);

                return Ok(result);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("endpoint électronique", StringComparison.OrdinalIgnoreCase)
                                                       || ex.Message.Contains("MOBILE_MONEY", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de réservation avec paiement");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Initie un paiement électronique FlexPay (hold sièges, pas de réservation avant callback).
        /// </summary>
        [HttpPost("reservation_with_paiement_electronique")]
        [ProducesResponseType(typeof(ReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ReservationWithPaiementResponseDto>> CreateReservationWithPaiementElectronique(
            [FromBody] InitiateFlexPayReservationDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _flexPayReservationService.InitiateAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initiation du paiement électronique");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Crée une réservation aller-retour avec paiement cash (2 voyages, 1 paiement).
        /// </summary>
        [HttpPost("reservation_aller_retour_with_paiement")]
        [ProducesResponseType(typeof(ReservationAllerRetourWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ReservationAllerRetourWithPaiementResponseDto>> CreateReservationAllerRetourWithPaiement(
            [FromBody] CreateReservationAllerRetourWithPaiementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _allerRetourReservationService.CreateCashAsync(dto);
                if (result.Statut == TransactionStatut.Echec)
                    return StatusCode(500, result);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur création réservation aller-retour cash");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Initie un paiement FlexPay pour une réservation aller-retour (holds sur 2 voyages).
        /// </summary>
        [HttpPost("reservation_aller_retour_with_paiement_electronique")]
        [ProducesResponseType(typeof(ReservationAllerRetourWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<ReservationAllerRetourWithPaiementResponseDto>> CreateReservationAllerRetourElectronique(
            [FromBody] InitiateFlexPayReservationAllerRetourDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _allerRetourReservationService.InitiateFlexPayAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur initiation FlexPay aller-retour");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Détail d'un dossier aller-retour.
        /// </summary>
        [HttpGet("aller-retour/{id:int}")]
        [ProducesResponseType(typeof(ReservationAllerRetourResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ReservationAllerRetourResponseDto>> GetAllerRetour(int id)
        {
            try
            {
                var result = await _allerRetourReservationService.GetByIdAsync(id);
                if (result == null)
                    return NotFound(new { message = $"Aller-retour {id} introuvable." });
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lecture aller-retour {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Annule atomiquement un dossier aller-retour (2 legs + libération sièges).
        /// </summary>
        [HttpPost("aller-retour/{id:int}/cancel")]
        [ProducesResponseType(typeof(ReservationAllerRetourResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<ReservationAllerRetourResponseDto>> CancelAllerRetour(int id)
        {
            try
            {
                var exists = await _allerRetourReservationService.GetByIdAsync(id);
                if (exists == null)
                    return NotFound(new { message = $"Aller-retour {id} introuvable." });

                var result = await _allerRetourReservationService.CancelAsync(id);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur annulation aller-retour {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        private List<ReservationResponseDto> MapReservationsWithPassagers(IEnumerable<Reservation> reservations)
        {
            var dtos = new List<ReservationResponseDto>();
            foreach (var r in reservations)
            {
                var dto = _mapper.Map<ReservationResponseDto>(r);
                dto.Passagers = r.Passagers != null && r.Passagers.Count > 0
                    ? _mapper.Map<List<ReservationPassengerReadDto>>(r.Passagers.OrderBy(p => p.IdReservationPassenger))
                    : new List<ReservationPassengerReadDto>();
                dtos.Add(dto);
            }

            return dtos;
        }
    }
}
