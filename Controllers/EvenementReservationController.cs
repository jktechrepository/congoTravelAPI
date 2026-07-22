using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
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
        private readonly IEvenementPaymentService _paymentService;
        private readonly IEvenementFlexPayInitiationService _flexPayInitiationService;
        private readonly IEvenementReservationService _reservationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementReservationController> _logger;

        public EvenementReservationController(
            IEvenementPaymentService paymentService,
            IEvenementFlexPayInitiationService flexPayInitiationService,
            IEvenementReservationService reservationService,
            ICurrentUserService currentUserService,
            ILogger<EvenementReservationController> logger)
        {
            _paymentService = paymentService;
            _flexPayInitiationService = flexPayInitiationService;
            _reservationService = reservationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les réservations événement de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementReservationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idEvenementSession,
            [FromQuery] string? customerRef,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new EvenementReservationListFilter
                {
                    Status = parsedStatus,
                    IdEvenementSession = idEvenementSession,
                    CustomerRef = customerRef
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
                _logger.LogError(ex, "Erreur GET liste réservations événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les réservations événement d'une société (alias explicite).</summary>
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
                var reservations = await _reservationService.ListAsync(effectiveSocieteId, cancellationToken: cancellationToken);
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
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _reservationService.GetTicketsByReservationAsync(id, idSociete, cancellationToken);

                if (tickets == null)
                    return NotFound(new { message = $"Réservation événement {id} introuvable." });

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
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var reservation = await _reservationService.GetByIdAsync(id, idSociete, cancellationToken);

                if (reservation == null)
                    return NotFound(new { message = $"Réservation événement {id} introuvable." });

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

        [HttpPost("{id:int}/confirm-payment")]
        [Permission("Evenement.Reservation.Confirm")]
        public async Task<ActionResult<EvenementConfirmPaymentResponseDto>> ConfirmPayment(
            int id,
            [FromBody] EvenementConfirmPaymentRequestDto request)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _paymentService.ConfirmPaymentAsync(id, idSociete, request);
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
                _logger.LogError(ex, "Erreur confirm-payment réservation événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/initiate-flexpay")]
        [Permission("Evenement.Reservation.Confirm")]
        public async Task<ActionResult<EvenementInitiateFlexPayResponseDto>> InitiateFlexPay(
            int id,
            [FromBody] EvenementInitiateFlexPayRequestDto request)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _flexPayInitiationService.InitiateAsync(id, idSociete, request);
                return Ok(result);
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
                _logger.LogError(ex, "Erreur initiate-flexpay réservation événement {Id}", id);
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

        private static bool TryParseOptionalStatus(
            string? status,
            out EvenementReservationStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<EvenementReservationStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : HOLD, CONFIRMED, CANCELLED, EXPIRED.";
            return false;
        }
    }
}
