using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Attributes;
using CongoTravel.Models.DTOs.Voyage;
using CongoTravel.Models.DTOs.VoyageTarification;
using CongoTravel.Helpers;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Data;
using AutoMapper;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class VoyageController : ControllerBase
    {
        private readonly IVoyageRepository _voyageRepository;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly IVoyageReportService _voyageReportService;
        private readonly ICurrentUserService _currentUserService;
        private readonly CongoTravelDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<VoyageController> _logger;

        public VoyageController(
            IVoyageRepository voyageRepository,
            IVoyageTarifService voyageTarifService,
            IVoyageReportService voyageReportService,
            ICurrentUserService currentUserService,
            CongoTravelDbContext context,
            IMapper mapper,
            ILogger<VoyageController> logger)
        {
            _voyageRepository = voyageRepository;
            _voyageTarifService = voyageTarifService;
            _voyageReportService = voyageReportService;
            _currentUserService = currentUserService;
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        private VoyageResponseDto MapVoyageResponse(Voyage voyage)
        {
            var dto = _mapper.Map<VoyageResponseDto>(voyage);
            if (voyage.VoyageDestinations != null && voyage.VoyageDestinations.Count > 0)
            {
                dto.EtapesDestinations = _mapper.Map<List<VoyageEtapeReadDto>>(
                    voyage.VoyageDestinations.OrderBy(e => e.Ordre).ToList());
            }

            if (voyage.VoyageTarifsCategorieSiege != null && voyage.VoyageTarifsCategorieSiege.Count > 0)
            {
                dto.Tarifs = voyage.VoyageTarifsCategorieSiege
                    .OrderBy(t => t.IdCategorieSiege)
                    .Select(t => new VoyageTarifCategorieSiegeResponseItemDto
                    {
                        IdCategorieSiege = t.IdCategorieSiege,
                        Libelle = t.CategorieSiege != null
                            ? (!string.IsNullOrWhiteSpace(t.CategorieSiege.CodeCategorieSiege)
                                ? t.CategorieSiege.CodeCategorieSiege
                                : t.CategorieSiege.Libelle)
                            : string.Empty,
                        Prix = t.Prix
                    })
                    .ToList();
            }

            return dto;
        }

        private static VoyageTarifCategorieSiegeReadDto MapTarifReadDto(VoyageTarifCategorieSiege t) =>
            new()
            {
                IdVoyageTarifCategorieSiege = t.IdVoyageTarifCategorieSiege,
                IdCategorieSiege = t.IdCategorieSiege,
                CodeCategorieSiege = t.CategorieSiege?.CodeCategorieSiege ?? string.Empty,
                LibelleCategorie = t.CategorieSiege?.Libelle ?? string.Empty,
                Prix = t.Prix
            };

        private async Task EnrichRepartitionSiegesDisponiblesAsync(IReadOnlyList<VoyageResponseDto> dtos)
        {
            if (dtos.Count == 0)
                return;

            var repartitionByVoyage = await _voyageRepository.GetRepartitionSiegesDisponiblesParVoyagesAsync(
                dtos.Select(v => v.Id).ToList());

            foreach (var dto in dtos)
            {
                if (repartitionByVoyage.TryGetValue(dto.Id, out var repartition))
                    dto.RepartitionCategorieSiegesDisponible = repartition;
            }
        }

        private async Task EnrichVoyageResponseDtosAsync(IReadOnlyList<VoyageResponseDto> dtos, CancellationToken cancellationToken = default)
        {
            await EnrichRepartitionSiegesDisponiblesAsync(dtos);
            await VoyageConfigEnrichmentHelper.EnrichElectronicSupplementAsync(_context, dtos, cancellationToken);
        }

        private async Task<VoyageResponseDto> MapVoyageResponseAsync(Voyage voyage, CancellationToken cancellationToken = default)
        {
            var dto = MapVoyageResponse(voyage);
            await EnrichVoyageResponseDtosAsync(new[] { dto }, cancellationToken);
            return dto;
        }

        private async Task<List<VoyageResponseDto>> MapVoyageResponsesAsync(
            IEnumerable<Voyage> voyages,
            CancellationToken cancellationToken = default)
        {
            var list = voyages.Select(MapVoyageResponse).ToList();
            await EnrichVoyageResponseDtosAsync(list, cancellationToken);
            return list;
        }

        private async Task<PagedResult<VoyageResponseDto>> MapPagedVoyagesAsync(
            PagedResult<Voyage> pagedResult,
            CancellationToken cancellationToken = default)
        {
            var mapped = pagedResult.Data.Select(MapVoyageResponse).ToList();
            await EnrichVoyageResponseDtosAsync(mapped, cancellationToken);

            return new PagedResult<VoyageResponseDto>(
                mapped,
                pagedResult.TotalCount,
                pagedResult.PageNumber,
                pagedResult.PageSize);
        }

        private static (DateTime? DateDebut, DateTime? DateFin) ResolveDateDepartFilter(
            DateTime? date,
            VoyageListePeriode periode) =>
            VoyageListeDateFilter.Resolve(date, periode);

        private async Task<ActionResult<PagedResult<VoyageResponseDto>>> ExecutePagedQueryAsync(
            PagedRequest request,
            DateTime? date,
            VoyageListePeriode periode,
            Func<PagedRequest, DateTime?, DateTime?, Task<PagedResult<Voyage>>> fetch)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
            var pagedResult = await fetch(request, dateDebut, dateFin);
            return Ok(await MapPagedVoyagesAsync(pagedResult));
        }

        // GET: api/voyage/paged — route officielle (liste paginée)
        /// <summary>
        /// Liste paginée des voyages (recommandé). Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("paged")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetPagedAsync(r, d, f));

        /// <summary>
        /// Recherche paginée de voyages par ville de départ, ville d'arrivée et/ou société.
        /// Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, villeDepart, villeArrivee, idSociete, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> SearchPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] string? villeDepart = null,
            [FromQuery] string? villeArrivee = null,
            [FromQuery] int? idSociete = null,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(
                request,
                date,
                periode,
                (r, d, f) => _voyageRepository.SearchPagedAsync(r, villeDepart, villeArrivee, idSociete, d, f));

        /// <summary>
        /// Liste paginée des voyages d'une société. Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("societe/{idSociete}/paged")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetBySocietePaged(
            int idSociete,
            [FromQuery] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetBySocietePagedAsync(idSociete, r, d, f));

        /// <summary>
        /// Liste paginée des voyages d'un site. Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("site/{idSite}/paged")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetBySitePaged(
            int idSite,
            [FromQuery] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetBySitePagedAsync(idSite, r, d, f));

        /// <summary>
        /// Liste paginée des voyages d'un véhicule. Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("vehicule/{idVehicule}/paged")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetByVehiculePaged(
            int idVehicule,
            [FromQuery] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetByVehiculePagedAsync(idVehicule, r, d, f));

        /// <summary>
        /// Liste paginée des voyages d'une destination. Query : pageNumber, pageSize, searchTerm, sortBy, sortDescending, date, periode (Jour | Hebdomadaire | Mensuel | Tout).
        /// </summary>
        [HttpGet("destination/{idDestination}/paged")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<VoyageResponseDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetByDestinationPaged(
            int idDestination,
            [FromQuery] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetByDestinationPagedAsync(idDestination, r, d, f));

        // GET: api/voyage — legacy (tableau non paginé)
        /// <summary>Legacy : tableau complet. Préférer <c>GET /api/Voyage/paged</c>.</summary>
        [HttpGet]
        [AllowAnonymous]
        [Obsolete("Utiliser GET /api/Voyage/paged pour les listes paginées.")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetAll(
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour)
        {
            try
            {
                var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
                var voyages = await _voyageRepository.GetAllAsync(dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les voyages");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Tarifs par catégorie de siège pour ce voyage (prix par place).</summary>
        [HttpGet("{id:int}/tarifs-categorie-siege")]
        [Permission("Voyage.Read")]
        public async Task<ActionResult<IEnumerable<VoyageTarifCategorieSiegeReadDto>>> GetTarifsCategorieSiege(int id)
        {
            try
            {
                var voyage = await _voyageRepository.GetByIdAsync(id);
                if (voyage == null)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var tarifs = await _voyageTarifService.GetTarifsByVoyageAsync(id);
                return Ok(tarifs.Select(MapTarifReadDto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des tarifs voyage {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Liste des passagers ayant enregistré un embarquement pour le voyage identifié par destination, véhicule et date de départ (jour civil).
        /// </summary>
        /// <param name="idDestination">Identifiant de la destination principale du voyage (<see cref="Voyage.IdDestination"/>).</param>
        /// <param name="idVehicule">Identifiant du véhicule.</param>
        /// <param name="dateDepart">Date de départ du voyage (seul le jour est pris en compte).</param>
        /// <param name="heureDepart">Optionnel : heure de départ du voyage (<see cref="Voyage.HeureDepart"/>), ex. <c>08:30:00</c> en query string.</param>
        [HttpGet("passagers-embarques")]
        [ProducesResponseType(typeof(IEnumerable<PassagerEmbarqueVoyageItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<PassagerEmbarqueVoyageItemDto>>> GetPassagersEmbarques(
            [FromQuery] int idDestination,
            [FromQuery] int idVehicule,
            [FromQuery] DateTime dateDepart,
            [FromQuery] TimeSpan? heureDepart = null)
        {
            try
            {
                if (idDestination <= 0 || idVehicule <= 0)
                    return BadRequest(new { message = "idDestination et idVehicule doivent être des identifiants valides (> 0)." });

                if (dateDepart == default)
                    return BadRequest(new { message = "dateDepart est obligatoire (ex. 2026-05-13)." });

                var result = await _voyageRepository.GetPassagersEmbarquesPourCriteresVoyageAsync(
                    idDestination, idVehicule, dateDepart, heureDepart);

                if (!result.Success)
                    return StatusCode(result.ErrorStatusCode, new { message = result.ErrorMessage });

                return Ok(result.Items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors de la lecture des passagers embarqués (destination {IdDestination}, véhicule {IdVehicule}, date {Date}, heure {Heure})",
                    idDestination, idVehicule, dateDepart, heureDepart);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Remplace l'ensemble des tarifs (par catégorie) pour un voyage.</summary>
        [HttpPut("{id:int}/tarifs-categorie-siege")]
        [Permission("Voyage.Update")]
        public async Task<ActionResult<IEnumerable<VoyageTarifCategorieSiegeReadDto>>> PutTarifsCategorieSiege(
            int id,
            [FromBody] VoyageTarifsCategorieSiegeUpsertDto body)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var voyage = await _voyageRepository.GetByIdAsync(id);
                if (voyage == null)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var lignes = body.Tarifs.Select(t => (t.IdCategorieSiege, t.Prix)).ToList();
                await _voyageTarifService.ReplaceTarifsForVoyageAsync(id, voyage.IdSociete, lignes);
                await _voyageRepository.SyncVoyagePrixReferenceFromTarifsAsync(id);

                var tarifs = await _voyageTarifService.GetTarifsByVoyageAsync(id);
                return Ok(tarifs.Select(MapTarifReadDto));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des tarifs voyage {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Met à jour le tarif d'une seule catégorie de siège pour ce voyage.</summary>
        [HttpPatch("{id:int}/tarifs-categorie-siege/{idCategorieSiege:int}")]
        [Permission("Voyage.Update")]
        public async Task<ActionResult<VoyageTarifCategorieSiegeReadDto>> PatchTarifCategorieSiege(
            int id,
            int idCategorieSiege,
            [FromBody] VoyageTarifCategorieSiegePatchDto body)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var voyage = await _voyageRepository.GetByIdAsync(id);
                if (voyage == null)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var row = await _voyageTarifService.UpsertTarifForVoyageAsync(
                    id,
                    voyage.IdSociete,
                    idCategorieSiege,
                    body.Prix);

                await _voyageRepository.SyncVoyagePrixReferenceFromTarifsAsync(id);

                return Ok(MapTarifReadDto(row));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erreur lors du PATCH tarif voyage {id} catégorie {idCategorieSiege}",
                    id,
                    idCategorieSiege);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<VoyageResponseDto>> GetById(int id)
        {
            try
            {
                var voyage = await _voyageRepository.GetByIdAsync(id);
                if (voyage == null)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                return Ok(await MapVoyageResponseAsync(voyage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du voyage {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/{id}/destinations
        /// <summary>
        /// Étapes ordonnées du voyage (<see cref="VoyageDestination"/>).
        /// </summary>
        [HttpGet("{id:int}/destinations")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<VoyageEtapeReadDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<VoyageEtapeReadDto>>> GetDestinationsOrdered(int id)
        {
            try
            {
                if (!await _voyageRepository.ExistsAsync(id))
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var steps = await _voyageRepository.GetOrderedDestinationsAsync(id);
                var dtos = _mapper.Map<List<VoyageEtapeReadDto>>(steps);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des destinations du voyage {VoyageId}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/{id}/sieges-disponibles
        /// <summary>
        /// Sièges libres pour ce voyage (non couverts par une allocation CONFIRME), regroupés par catégorie.
        /// </summary>
        [HttpGet("{id:int}/sieges-disponibles")]
        [ProducesResponseType(typeof(VoyageSiegesDisponiblesResponseDto), 200)]
        public async Task<ActionResult<VoyageSiegesDisponiblesResponseDto>> GetSiegesDisponibles(int id)
        {
            try
            {
                if (!await _voyageRepository.ExistsAsync(id))
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var response = await _voyageRepository.GetSiegesDisponiblesResponsePourVoyageAsync(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des sièges disponibles pour le voyage {VoyageId}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/{id}/sieges-indisponibles
        /// <summary>
        /// Sièges déjà attribués sur ce voyage (allocations CONFIRME).
        /// </summary>
        [HttpGet("{id:int}/sieges-indisponibles")]
        [ProducesResponseType(typeof(IReadOnlyList<SiegeIndisponibleReadDto>), 200)]
        public async Task<ActionResult<IReadOnlyList<SiegeIndisponibleReadDto>>> GetSiegesIndisponibles(int id)
        {
            try
            {
                if (!await _voyageRepository.ExistsAsync(id))
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                var allocations = await _voyageRepository.GetAllocationsConfirmePourVoyageAsync(id);
                var dtos = _mapper.Map<List<SiegeIndisponibleReadDto>>(allocations);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture des sièges indisponibles pour le voyage {VoyageId}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Legacy — préférer <c>GET /api/Voyage/societe/{idSociete}/paged</c>.</summary>
        [HttpGet("societe/{idSociete}")]
        [AllowAnonymous]
        [Obsolete("Utiliser GET /api/Voyage/societe/{idSociete}/paged pour la liste paginée.")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetBySociete(
            int idSociete,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour)
        {
            try
            {
                var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
                var voyages = await _voyageRepository.GetBySocieteAsync(idSociete, dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la société {idSociete}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Legacy — préférer <c>GET /api/Voyage/site/{idSite}/paged</c>.</summary>
        [HttpGet("site/{idSite}")]
        [AllowAnonymous]
        [Obsolete("Utiliser GET /api/Voyage/site/{idSite}/paged pour la liste paginée.")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetBySite(
            int idSite,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour)
        {
            try
            {
                var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
                var voyages = await _voyageRepository.GetBySiteAsync(idSite, dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le site {idSite}", idSite);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Legacy — préférer <c>GET /api/Voyage/vehicule/{idVehicule}/paged</c>.</summary>
        [HttpGet("vehicule/{idVehicule}")]
        [Obsolete("Utiliser GET /api/Voyage/vehicule/{idVehicule}/paged pour la liste paginée.")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByVehicule(
            int idVehicule,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour)
        {
            try
            {
                var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
                var voyages = await _voyageRepository.GetByVehiculeAsync(idVehicule, dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le véhicule {idVehicule}", idVehicule);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Legacy — préférer <c>GET /api/Voyage/destination/{idDestination}/paged</c>.</summary>
        [HttpGet("destination/{idDestination}")]
        [AllowAnonymous]
        [Obsolete("Utiliser GET /api/Voyage/destination/{idDestination}/paged pour la liste paginée.")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByDestination(
            int idDestination,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour)
        {
            try
            {
                var (dateDebut, dateFin) = ResolveDateDepartFilter(date, periode);
                var voyages = await _voyageRepository.GetByDestinationAsync(idDestination, dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la destination {idDestination}", idDestination);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/date/{date}
        [HttpGet("date/{date}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByDate(DateTime date)
        {
            try
            {
                var voyages = await _voyageRepository.GetByDateAsync(date);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/vehicule/{idVehicule}/destination/{idDestination}
        [HttpGet("vehicule/{idVehicule}/destination/{idDestination}")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByVehiculeAndDestination(int idVehicule, int idDestination)
        {
            try
            {
                var voyages = await _voyageRepository.GetByVehiculeAndDestinationAsync(idVehicule, idDestination);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages pour le véhicule {idVehicule} et destination {idDestination}", idVehicule, idDestination);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/daterange
        [HttpGet("daterange")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByDateRange([FromQuery] DateTime dateDebut, [FromQuery] DateTime dateFin)
        {
            try
            {
                var voyages = await _voyageRepository.GetByDateRangeAsync(dateDebut, dateFin);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages entre {dateDebut} et {dateFin}", dateDebut, dateFin);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/statut/{statut}
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByStatut(bool statut)
        {
            try
            {
                var voyages = await _voyageRepository.GetByStatutAsync(statut);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages avec statut {statut}", statut);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/pricerange
        [HttpGet("pricerange")]
        public async Task<ActionResult<IEnumerable<VoyageResponseDto>>> GetByPriceRange([FromQuery] int prixMin, [FromQuery] int prixMax)
        {
            try
            {
                var voyages = await _voyageRepository.GetByPriceRangeAsync(prixMin, prixMax);
                return Ok(await MapVoyageResponsesAsync(voyages));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des voyages avec prix entre {prixMin} et {prixMax}", prixMin, prixMax);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // POST: api/voyage
        [HttpPost]
        public async Task<ActionResult<VoyageResponseDto>> Create([FromBody] CreateVoyageDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                IReadOnlyList<CreateVoyageEtapeDto>? etapesPayload = createDto.EtapesDestinations is { Count: > 0 }
                    ? createDto.EtapesDestinations.OrderBy(e => e.Ordre).ToList()
                    : null;

                var principalDest = etapesPayload != null
                    ? etapesPayload[0].IdDestination
                    : createDto.IdDestination!.Value;

                var voyage = _mapper.Map<Voyage>(createDto);
                voyage.IdDestination = principalDest;

                await _voyageRepository.CreateAsync(voyage, etapesPayload);

                if (createDto.Tarifs is { Count: > 0 })
                {
                    var lignes = createDto.Tarifs
                        .Select(t => (t.IdCategorieSiege, t.Prix))
                        .ToList();
                    await _voyageTarifService.ReplaceTarifsForVoyageAsync(voyage.Id, voyage.IdSociete, lignes);
                }

                var reloaded = await _voyageRepository.GetByIdAsync(voyage.Id);
                if (reloaded == null)
                    return StatusCode(500, new { message = "Voyage créé mais rechargement impossible." });

                var resultDto = await MapVoyageResponseAsync(reloaded);
                return CreatedAtAction(nameof(GetById), new { id = resultDto.Id }, resultDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du voyage");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // PUT: api/voyage/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<VoyageResponseDto>> Update(int id, [FromBody] UpdateVoyageDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != updateDto.Id)
                    return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });

                IReadOnlyList<CreateVoyageEtapeDto>? etapesPayload = updateDto.EtapesDestinations is { Count: > 0 }
                    ? updateDto.EtapesDestinations.OrderBy(e => e.Ordre).ToList()
                    : null;

                var principalDest = etapesPayload != null
                    ? etapesPayload[0].IdDestination
                    : updateDto.IdDestination!.Value;

                var voyage = _mapper.Map<Voyage>(updateDto);
                voyage.IdDestination = principalDest;

                try
                {
                    await _voyageRepository.EnsurePrixUpdateAllowedAsync(
                        id,
                        updateDto.Prix,
                        updateDto.Tarifs is { Count: > 0 });
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                var updatedVoyage = await _voyageRepository.UpdateAsync(voyage, etapesPayload);

                if (updatedVoyage == null)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                if (updateDto.Tarifs is { Count: > 0 })
                {
                    var lignes = updateDto.Tarifs
                        .Select(t => (t.IdCategorieSiege, t.Prix))
                        .ToList();
                    await _voyageTarifService.ReplaceTarifsForVoyageAsync(updatedVoyage.Id, updatedVoyage.IdSociete, lignes);
                    await _voyageRepository.SyncVoyagePrixReferenceFromTarifsAsync(updatedVoyage.Id);
                }

                var reloaded = await _voyageRepository.GetByIdAsync(updatedVoyage.Id);
                if (reloaded == null)
                    return StatusCode(500, new { message = "Voyage mis à jour mais rechargement impossible." });

                return Ok(await MapVoyageResponseAsync(reloaded));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du voyage {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Reporte la date/heure d'un voyage (Admin/Gérant). Recalcule les validités billet et notifie les clients réservés.</summary>
        [HttpPost("{id}/reporter")]
        [Permission("Voyage.Update")]
        public async Task<ActionResult<ReporterVoyageResultDto>> Reporter(int id, [FromBody] ReporterVoyageDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _voyageReportService.ReporterAsync(
                    id,
                    _currentUserService.SocieteId,
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    dto);

                if (!result.Success)
                {
                    return StatusCode(result.StatusCode, new
                    {
                        message = result.Message,
                        billetsUtilises = result.BilletsUtilises
                    });
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du report du voyage {IdVoyage}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // DELETE: api/voyage/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var deleted = await _voyageRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Voyage avec l'ID {id} non trouvé" });

                return Ok(new { message = "Voyage supprimé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du voyage {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>Legacy — préférer <c>GET /api/Voyage/societe/{idSociete}/paged</c>.</summary>
        [HttpPost("societe/{idSociete}/paged")]
        [Obsolete("Utiliser GET /api/Voyage/societe/{idSociete}/paged avec PagedRequest en query string.")]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetBySocietePagedPost(
            int idSociete,
            [FromBody] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetBySocietePagedAsync(idSociete, r, d, f));

        [HttpPost("site/{idSite}/paged")]
        [Obsolete("Utiliser GET /api/Voyage/site/{idSite}/paged avec PagedRequest en query string.")]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetBySitePagedPost(
            int idSite,
            [FromBody] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetBySitePagedAsync(idSite, r, d, f));

        [HttpPost("vehicule/{idVehicule}/paged")]
        [Obsolete("Utiliser GET /api/Voyage/vehicule/{idVehicule}/paged avec PagedRequest en query string.")]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetByVehiculePagedPost(
            int idVehicule,
            [FromBody] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetByVehiculePagedAsync(idVehicule, r, d, f));

        [HttpPost("destination/{idDestination}/paged")]
        [Obsolete("Utiliser GET /api/Voyage/destination/{idDestination}/paged avec PagedRequest en query string.")]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetByDestinationPagedPost(
            int idDestination,
            [FromBody] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetByDestinationPagedAsync(idDestination, r, d, f));

        [HttpPost("paged")]
        [Obsolete("Utiliser GET /api/Voyage/paged avec PagedRequest en query string.")]
        public Task<ActionResult<PagedResult<VoyageResponseDto>>> GetPagedPost(
            [FromBody] PagedRequest request,
            [FromQuery] DateTime? date = null,
            [FromQuery, DefaultValue(VoyageListePeriode.Jour)] VoyageListePeriode periode = VoyageListePeriode.Jour) =>
            ExecutePagedQueryAsync(request, date, periode, (r, d, f) => _voyageRepository.GetPagedAsync(r, d, f));

        // GET: api/voyage/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetCount()
        {
            try
            {
                var count = await _voyageRepository.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/vehicule/{idVehicule}/count
        [HttpGet("vehicule/{idVehicule}/count")]
        public async Task<ActionResult<int>> GetCountByVehicule(int idVehicule)
        {
            try
            {
                var count = await _voyageRepository.CountByVehiculeAsync(idVehicule);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour le véhicule {idVehicule}", idVehicule);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/destination/{idDestination}/count
        [HttpGet("destination/{idDestination}/count")]
        public async Task<ActionResult<int>> GetCountByDestination(int idDestination)
        {
            try
            {
                var count = await _voyageRepository.CountByDestinationAsync(idDestination);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour la destination {idDestination}", idDestination);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/date/{date}/count
        [HttpGet("date/{date}/count")]
        public async Task<ActionResult<int>> GetCountByDate(DateTime date)
        {
            try
            {
                var count = await _voyageRepository.CountByDateAsync(date);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages pour la date {date}", date);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/voyage/statut/{statut}/count
        [HttpGet("statut/{statut}/count")]
        public async Task<ActionResult<int>> GetCountByStatut(bool statut)
        {
            try
            {
                var count = await _voyageRepository.CountByStatutAsync(statut);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du comptage des voyages avec statut {statut}", statut);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
