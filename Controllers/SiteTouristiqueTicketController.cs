using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/tickets")]
    [Authorize]
    public class SiteTouristiqueTicketController : ControllerBase
    {
        private readonly ISiteTouristiqueTicketService _ticketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueTicketController> _logger;

        public SiteTouristiqueTicketController(
            ISiteTouristiqueTicketService ticketService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueTicketController> logger)
        {
            _ticketService = ticketService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les tickets site touristique de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idSiteTouristiqueReservation,
            [FromQuery] int? idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new SiteTouristiqueTicketListFilter
                {
                    Status = parsedStatus,
                    IdSiteTouristiqueReservation = idSiteTouristiqueReservation,
                    IdSiteTouristiqueJournee = idSiteTouristiqueJournee
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
                _logger.LogError(ex, "Erreur GET liste tickets site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets site touristique d'une société (alias explicite).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur GET tickets site touristique société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation pour une société.</summary>
        [HttpGet("societe/{idSociete:int}/reservation/{idSiteTouristiqueReservation:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetBySocieteAndReservation(
            int idSociete,
            int idSiteTouristiqueReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var tickets = await _ticketService.ListBySocieteAndReservationAsync(
                    effectiveSocieteId,
                    idSiteTouristiqueReservation,
                    cancellationToken);

                if (tickets == null)
                {
                    return NotFound(new
                    {
                        message = $"Réservation {idSiteTouristiqueReservation} introuvable ou n'appartient pas à la société {effectiveSocieteId}."
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
                    "Erreur GET tickets site touristique société {IdSociete} réservation {IdReservation}",
                    idSociete,
                    idSiteTouristiqueReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation (société JWT).</summary>
        [HttpGet("reservation/{idSiteTouristiqueReservation:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetByReservation(
            int idSiteTouristiqueReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByReservationAsync(
                    idSiteTouristiqueReservation,
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
                _logger.LogError(ex, "Erreur GET tickets site touristique réservation {IdReservation}", idSiteTouristiqueReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une session site touristique.</summary>
        [HttpGet("session/{idSiteTouristiqueJournee:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetBySession(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListBySessionAsync(
                    idSiteTouristiqueJournee,
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
                _logger.LogError(ex, "Erreur GET tickets site touristique session {IdSession}", idSiteTouristiqueJournee);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets par statut (ISSUED, USED, VOID).</summary>
        [HttpGet("status/{status}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<SiteTouristiqueTicketStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID."
                    });
                }

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByStatusAsync(parsedStatus, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets site touristique statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par code (équivalent qrcode transport).</summary>
        [HttpGet("code/{ticketCode}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueTicketDetailResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueTicketDetailResponseDto>> GetByCode(
            string ticketCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketCode))
                    return BadRequest(new { message = "Le paramètre ticketCode est obligatoire." });

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByTicketCodeAsync(ticketCode, idSociete, cancellationToken);

                if (ticket == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucun ticket site touristique avec le code '{ticketCode.Trim()}'."
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
                _logger.LogError(ex, "Erreur GET ticket site touristique code {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis à une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets site touristique date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis entre deux dates (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueTicketListItemDto>>> GetByDateRange(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dateFin < dateDebut)
                    return BadRequest(new { message = "dateFin doit être supérieure ou égale à dateDebut." });

                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                    "Erreur GET tickets site touristique plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par identifiant numérique.</summary>
        [HttpGet("{id:int}")]
        [Permission("SiteTouristique.Lieu.Read")]
        [ProducesResponseType(typeof(SiteTouristiqueTicketDetailResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueTicketDetailResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByIdAsync(id, idSociete, cancellationToken);

                if (ticket == null)
                    return NotFound(new { message = $"Ticket site touristique {id} introuvable." });

                return Ok(ticket);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET ticket site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{ticketCode}/check")]
        [Permission("SiteTouristique.Ticket.Check")]
        public async Task<ActionResult<SiteTouristiqueTicketCheckResponseDto>> Check(string ticketCode)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _ticketService.CheckTicketAsync(ticketCode, idSociete);
                return StatusCode(result.HttpStatusCode, result.Response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur check ticket site touristique {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{ticketCode}/use")]
        [Permission("SiteTouristique.Ticket.Use")]
        public async Task<ActionResult<SiteTouristiqueTicketUseResponseDto>> Use(string ticketCode)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                _logger.LogError(ex, "Erreur use ticket site touristique {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out SiteTouristiqueTicketStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<SiteTouristiqueTicketStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID.";
            return false;
        }
    }
}
