using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/tickets")]
    [Authorize]
    public class EvenementTicketController : ControllerBase
    {
        private readonly IEvenementTicketService _ticketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementTicketController> _logger;

        public EvenementTicketController(
            IEvenementTicketService ticketService,
            ICurrentUserService currentUserService,
            ILogger<EvenementTicketController> logger)
        {
            _ticketService = ticketService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les tickets événement de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idEvenementReservation,
            [FromQuery] int? idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new EvenementTicketListFilter
                {
                    Status = parsedStatus,
                    IdEvenementReservation = idEvenementReservation,
                    IdEvenementSession = idEvenementSession
                };

                var tickets = await _ticketService.ListAsync(effectiveSocieteId, filter, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste tickets événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets événement d'une société (alias explicite).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var tickets = await _ticketService.ListAsync(effectiveSocieteId, cancellationToken: cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets événement société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation pour une société.</summary>
        [HttpGet("societe/{idSociete:int}/reservation/{idEvenementReservation:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetBySocieteAndReservation(
            int idSociete,
            int idEvenementReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var tickets = await _ticketService.ListBySocieteAndReservationAsync(
                    effectiveSocieteId,
                    idEvenementReservation,
                    cancellationToken);

                if (tickets == null)
                {
                    return NotFound(new
                    {
                        message = $"Réservation {idEvenementReservation} introuvable ou n'appartient pas à la société {effectiveSocieteId}."
                    });
                }

                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur GET tickets événement société {IdSociete} réservation {IdReservation}",
                    idSociete,
                    idEvenementReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation (société JWT).</summary>
        [HttpGet("reservation/{idEvenementReservation:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetByReservation(
            int idEvenementReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByReservationAsync(
                    idEvenementReservation,
                    idSociete,
                    cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets événement réservation {IdReservation}", idEvenementReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une session événement.</summary>
        [HttpGet("session/{idEvenementSession:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetBySession(
            int idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListBySessionAsync(
                    idEvenementSession,
                    idSociete,
                    cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets événement session {IdSession}", idEvenementSession);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets par statut (ISSUED, USED, VOID).</summary>
        [HttpGet("status/{status}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<EvenementTicketStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID."
                    });
                }

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByStatusAsync(parsedStatus, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets événement statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par code (équivalent qrcode transport).</summary>
        [HttpGet("code/{ticketCode}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(EvenementTicketDetailResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementTicketDetailResponseDto>> GetByCode(
            string ticketCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketCode))
                    return BadRequest(new { message = "Le paramètre ticketCode est obligatoire." });

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByTicketCodeAsync(ticketCode, idSociete, cancellationToken);

                if (ticket == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucun ticket événement avec le code '{ticketCode.Trim()}'."
                    });
                }

                return Ok(ticket);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET ticket événement code {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis à une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets événement date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis entre deux dates (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementTicketListItemDto>>> GetByDateRange(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dateFin < dateDebut)
                    return BadRequest(new { message = "dateFin doit être supérieure ou égale à dateDebut." });

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByDateRangeAsync(
                    dateDebut,
                    dateFin,
                    idSociete,
                    cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur GET tickets événement plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par identifiant numérique.</summary>
        [HttpGet("{id:int}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(EvenementTicketDetailResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementTicketDetailResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByIdAsync(id, idSociete, cancellationToken);

                if (ticket == null)
                    return NotFound(new { message = $"Ticket événement {id} introuvable." });

                return Ok(ticket);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET ticket événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{ticketCode}/check")]
        [Permission("Evenement.Ticket.Check")]
        public async Task<ActionResult<EvenementTicketCheckResponseDto>> Check(string ticketCode)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _ticketService.CheckTicketAsync(ticketCode, idSociete);
                return StatusCode(result.HttpStatusCode, result.Response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur check ticket événement {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{ticketCode}/use")]
        [Permission("Evenement.Ticket.Use")]
        public async Task<ActionResult<EvenementTicketUseResponseDto>> Use(string ticketCode)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _ticketService.UseTicketAsync(ticketCode, idSociete);

                if (result.Response != null)
                    return StatusCode(result.HttpStatusCode, result.Response);

                return StatusCode(result.HttpStatusCode, new { message = result.ErrorMessage });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur use ticket événement {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out EvenementTicketStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<EvenementTicketStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID.";
            return false;
        }
    }
}
