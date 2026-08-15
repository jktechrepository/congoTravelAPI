using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/reservations")]
    [Authorize]
    public class SiteTouristiqueReservationController : ControllerBase
    {
        private readonly ISiteTouristiqueReservationService _reservationService;
        private readonly ISiteTouristiqueReservationWithPaiementService _reservationWithPaiementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueReservationController> _logger;

        public SiteTouristiqueReservationController(
            ISiteTouristiqueReservationService reservationService,
            ISiteTouristiqueReservationWithPaiementService reservationWithPaiementService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueReservationController> logger)
        {
            _reservationService = reservationService;
            _reservationWithPaiementService = reservationWithPaiementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les réservations site touristique de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idSiteTouristiqueJournee,
            [FromQuery] string? customerRef,
            [FromQuery] int? idUtilisateur,
            [FromQuery] int? idClient,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new SiteTouristiqueReservationListFilter
                {
                    Status = parsedStatus,
                    IdSiteTouristiqueJournee = idSiteTouristiqueJournee,
                    CustomerRef = customerRef,
                    IdUtilisateur = idUtilisateur,
                    IdClient = idClient
                };

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
                _logger.LogError(ex, "Erreur GET liste réservations site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Parcours CASH (miroir Transport) : hold + confirm en un appel.
        /// </summary>
        [HttpPost("with-paiement")]
        [Permission("SiteTouristique.Hold.Create")]
        [Permission("SiteTouristique.Reservation.Confirm")]
        [ProducesResponseType(typeof(SiteTouristiqueReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<SiteTouristiqueReservationWithPaiementResponseDto>> CreateWithPaiement(
            [FromBody] SiteTouristiqueReservationWithPaiementRequestDto request,
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
            catch (SiteTouristiqueHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur with-paiement réservation site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Parcours FlexPay (miroir Transport électronique) : hold + initiate en un appel.
        /// Finalisation via callback / verify.
        /// </summary>
        [HttpPost("with-paiement-electronique")]
        [Permission("SiteTouristique.Hold.Create")]
        [Permission("SiteTouristique.Reservation.Confirm")]
        [ProducesResponseType(typeof(SiteTouristiqueReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<SiteTouristiqueReservationWithPaiementResponseDto>> CreateWithPaiementElectronique(
            [FromBody] SiteTouristiqueReservationWithPaiementRequestDto request,
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
            catch (SiteTouristiqueHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur with-paiement-electronique réservation site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations site touristique d'une société (alias explicite).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var reservations = await _reservationService.ListAsync(effectiveSocieteId, cancellationToken: cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations site touristique société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations d'une session pour une société.</summary>
        [HttpGet("societe/{idSociete:int}/session/{idSiteTouristiqueJournee:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetBySocieteAndSession(
            int idSociete,
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var reservations = await _reservationService.ListBySocieteAndSessionAsync(
                    effectiveSocieteId,
                    idSiteTouristiqueJournee,
                    cancellationToken);

                if (reservations == null)
                {
                    return NotFound(new
                    {
                        message = $"Session {idSiteTouristiqueJournee} introuvable ou n'appartient pas à la société {effectiveSocieteId}."
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
                    "Erreur GET réservations site touristique société {IdSociete} session {IdSession}",
                    idSociete,
                    idSiteTouristiqueJournee);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations d'une session (société JWT).</summary>
        [HttpGet("session/{idSiteTouristiqueJournee:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetBySession(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListBySessionAsync(
                    idSiteTouristiqueJournee,
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
                _logger.LogError(ex, "Erreur GET réservations site touristique session {IdSession}", idSiteTouristiqueJournee);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations par statut (HOLD, CONFIRMED, CANCELLED, EXPIRED).</summary>
        [HttpGet("status/{status}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<SiteTouristiqueReservationStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : HOLD, CONFIRMED, CANCELLED, EXPIRED."
                    });
                }

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                _logger.LogError(ex, "Erreur GET réservations site touristique statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Recherche une réservation par référence (unique par société).</summary>
        [HttpGet("reference/{reference}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueReservationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueReservationResponseDto>> GetByReference(
            string reference,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return BadRequest(new { message = "Le paramètre reference est obligatoire." });

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservation = await _reservationService.GetByReferenceAsync(
                    reference,
                    idSociete,
                    cancellationToken);

                if (reservation == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucune réservation site touristique avec la référence '{reference.Trim()}'."
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
                _logger.LogError(ex, "Erreur GET réservation site touristique référence {Reference}", reference);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations créées à une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservations = await _reservationService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(reservations);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservations site touristique date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations créées entre deux dates (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueReservationListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueReservationListItemDto>>> GetByDateRange(
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

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                    "Erreur GET réservations site touristique plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Tickets associés à une réservation site touristique.</summary>
        [HttpGet("{id:int}/tickets")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketResponseDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketResponseDto>>> GetTickets(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _reservationService.GetTicketsByReservationAsync(id, idSociete, cancellationToken);

                if (tickets == null)
                    return NotFound(new { message = $"Réservation site touristique {id} introuvable." });

                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets réservation site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'une réservation site touristique (lignes, tickets, paiements).</summary>
        [HttpGet("{id:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueReservationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueReservationResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservation = await _reservationService.GetByIdAsync(id, idSociete, cancellationToken);

                if (reservation == null)
                    return NotFound(new { message = $"Réservation site touristique {id} introuvable." });

                return Ok(reservation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservation site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/cancel")]
        [Permission("SiteTouristique.Reservation.Confirm")]
        public async Task<ActionResult<SiteTouristiqueCancelReservationResponseDto>> Cancel(int id)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _reservationService.CancelAsync(id, idSociete);
                return Ok(result);
            }
            catch (SiteTouristiqueHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur cancel réservation site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out SiteTouristiqueReservationStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<SiteTouristiqueReservationStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : HOLD, CONFIRMED, CANCELLED, EXPIRED.";
            return false;
        }
    }
}
