using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/reservations")]
    [Authorize]
    public class EvenementReservationController : ControllerBase
    {
        private readonly IEvenementReservationService _reservationService;
        private readonly IEvenementReservationWithPaiementService _reservationWithPaiementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementReservationController> _logger;

        public EvenementReservationController(
            IEvenementReservationService reservationService,
            IEvenementReservationWithPaiementService reservationWithPaiementService,
            ICurrentUserService currentUserService,
            ILogger<EvenementReservationController> logger)
        {
            _reservationService = reservationService;
            _reservationWithPaiementService = reservationWithPaiementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Liste les réservations événement de la société (JWT ou idSociete).
        /// Client voyageur : <c>?idSociete=</c> = organisateur autorisé ; filtre forcé sur son JWT.
        /// Sans <c>status</c> : uniquement <c>CONFIRMED</c>. Utiliser <c>status=ALL</c> pour tous les statuts.
        /// </summary>
        [HttpGet]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idEvenementSession,
            [FromQuery] string? customerRef,
            [FromQuery] int? idUtilisateur,
            [FromQuery] int? idClient,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUserService,
                    idSociete);

                if (!SatelliteReservationListStatusParser.TryParse(
                        status,
                        EvenementReservationStatus.CONFIRMED,
                        out var parsedStatus,
                        out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new EvenementReservationListFilter
                {
                    Status = parsedStatus,
                    IdEvenementSession = idEvenementSession,
                    CustomerRef = customerRef,
                    IdUtilisateur = idUtilisateur,
                    IdClient = idClient
                };
                EvenementTenancyGuard.ApplyClientSelfScopeToListFilter(_currentUserService, filter);

                var reservations = await _reservationService.ListAsync(
                    effectiveSocieteId,
                    filter,
                    cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste réservations événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Réservations événement d'un client sur <b>toutes</b> les sociétés organisatrices.
        /// Client voyageur : <paramref name="idClient"/> doit égaler le ClientId JWT.
        /// Sans <c>status</c> : uniquement <c>CONFIRMED</c> ; <c>status=ALL</c> pour tous.
        /// </summary>
        [HttpGet("client/{idClient:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetByClient(
            int idClient,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                EvenementTenancyGuard.EnsureClientMayQueryByClientId(_currentUserService, idClient);

                if (!SatelliteReservationListStatusParser.TryParse(
                        status,
                        EvenementReservationStatus.CONFIRMED,
                        out var parsedStatus,
                        out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new EvenementReservationListFilter { Status = parsedStatus };
                var reservations = await _reservationService.ListByClientAsync(
                    idClient,
                    filter,
                    cancellationToken);
                return Ok(reservations);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations événement client {IdClient}", idClient);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Parcours CASH (miroir Transport) : hold + confirm en un appel.
        /// </summary>
        [HttpPost("with-paiement")]
        [Permission("Evenement.Hold.Create")]
        [Permission("Evenement.Reservation.Confirm")]
        [ProducesResponseType(typeof(EvenementReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<EvenementReservationWithPaiementResponseDto>> CreateWithPaiement(
            [FromBody] EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Société résolue dans le service : staff = JWT ; Client = session Published.
                var result = await _reservationWithPaiementService.CreateCashAsync(
                    request,
                    cancellationToken);
                return Ok(result);
            }
            catch (EvenementHoldConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur with-paiement réservation événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Parcours FlexPay (miroir Transport électronique) : hold + initiate en un appel.
        /// Finalisation via callback / verify.
        /// </summary>
        [HttpPost("with-paiement-electronique")]
        [Permission("Evenement.Hold.Create")]
        [Permission("Evenement.Reservation.Confirm")]
        [ProducesResponseType(typeof(EvenementReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<EvenementReservationWithPaiementResponseDto>> CreateWithPaiementElectronique(
            [FromBody] EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Société résolue dans le service : staff = JWT ; Client = session Published.
                var result = await _reservationWithPaiementService.InitiateElectronicAsync(
                    request,
                    cancellationToken);
                return Ok(result);
            }
            catch (EvenementHoldConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur with-paiement-electronique réservation événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Liste les réservations événement d'une société (alias explicite).
        /// Défaut : uniquement <c>CONFIRMED</c> (même règle que <see cref="GetList"/>).
        /// </summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var reservations = await _reservationService.ListAsync(
                    effectiveSocieteId,
                    new EvenementReservationListFilter { Status = EvenementReservationStatus.CONFIRMED },
                    cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations événement société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations d'une session pour une société.</summary>
        [HttpGet("societe/{idSociete:int}/session/{idEvenementSession:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetBySocieteAndSession(
            int idSociete,
            int idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var reservations = await _reservationService.ListBySocieteAndSessionAsync(
                    effectiveSocieteId,
                    idEvenementSession,
                    cancellationToken);

                if (reservations == null)
                {
                    return NotFound(new
                    {
                        message = $"Session {idEvenementSession} introuvable ou n'appartient pas à la société {effectiveSocieteId}."
                    });
                }

                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur GET réservations événement société {IdSociete} session {IdSession}",
                    idSociete,
                    idEvenementSession);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations d'une session (société JWT).</summary>
        [HttpGet("session/{idEvenementSession:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetBySession(
            int idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListBySessionAsync(
                    idEvenementSession,
                    idSociete,
                    cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations événement session {IdSession}", idEvenementSession);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations par statut (HOLD, CONFIRMED, CANCELLED, EXPIRED).</summary>
        [HttpGet("status/{status}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<EvenementReservationStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : HOLD, CONFIRMED, CANCELLED, EXPIRED."
                    });
                }

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListByStatusAsync(
                    parsedStatus,
                    idSociete,
                    cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations événement statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Recherche une réservation par référence (unique par société).</summary>
        [HttpGet("reference/{reference}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(EvenementReservationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementReservationResponseDto>> GetByReference(
            string reference,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return BadRequest(new { message = "Le paramètre reference est obligatoire." });

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservation = await _reservationService.GetByReferenceAsync(
                    reference,
                    idSociete,
                    cancellationToken);

                if (reservation == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucune réservation événement avec la référence '{reference.Trim()}'."
                    });
                }

                return Ok(reservation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservation événement référence {Reference}", reference);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations créées à une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations événement date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations créées entre deux dates (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetByDateRange(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dateFin < dateDebut)
                {
                    return BadRequest(new { message = "dateFin doit être supérieure ou égale à dateDebut." });
                }

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListByDateRangeAsync(
                    dateDebut,
                    dateFin,
                    idSociete,
                    cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur GET réservations événement plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Tickets associés à une réservation événement.</summary>
        [HttpGet("{id:int}/tickets")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<EvenementTicketResponseDto>>> GetTickets(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUserService,
                    idSociete);
                var reservation = await _reservationService.GetByIdAsync(id, effectiveSocieteId, cancellationToken);
                if (reservation == null)
                    return NotFound(new { message = $"Réservation événement {id} introuvable." });

                EvenementTenancyGuard.EnsureClientOwnsReservation(
                    _currentUserService,
                    reservation.IdUtilisateur,
                    reservation.IdClient);

                var tickets = await _reservationService.GetTicketsByReservationAsync(
                    id,
                    effectiveSocieteId,
                    cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets réservation événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'une réservation événement (lignes, tickets, paiements).</summary>
        [HttpGet("{id:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(EvenementReservationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementReservationResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUserService,
                    idSociete);
                var reservation = await _reservationService.GetByIdAsync(id, effectiveSocieteId, cancellationToken);

                if (reservation == null)
                    return NotFound(new { message = $"Réservation événement {id} introuvable." });

                EvenementTenancyGuard.EnsureClientOwnsReservation(
                    _currentUserService,
                    reservation.IdUtilisateur,
                    reservation.IdClient);

                return Ok(reservation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservation événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/cancel")]
        [Permission("Evenement.Reservation.Confirm")]
        public async Task<ActionResult<EvenementCancelReservationResponseDto>> Cancel(int id)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _reservationService.CancelAsync(id, idSociete);
                return Ok(result);
            }
            catch (EvenementHoldConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur cancel réservation événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

    }
}
