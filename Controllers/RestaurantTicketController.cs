using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/tickets")]
    [Authorize]
    public class RestaurantTicketController : ControllerBase
    {
        private readonly IRestaurantTicketService _ticketService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantTicketController> _logger;

        public RestaurantTicketController(
            IRestaurantTicketService ticketService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantTicketController> logger)
        {
            _ticketService = ticketService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Liste les tickets restaurant de la société (JWT ou idSociete Super-Admin).</summary>
        [HttpGet]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idRestaurantReservation,
            [FromQuery] int? idRestaurantCreneau,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new RestaurantTicketListFilter
                {
                    Status = parsedStatus,
                    IdRestaurantReservation = idRestaurantReservation,
                    IdRestaurantCreneau = idRestaurantCreneau
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
                _logger.LogError(ex, "Erreur GET liste tickets restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets restaurant d'une société (alias explicite).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
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
                _logger.LogError(ex, "Erreur GET tickets restaurant société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation pour une société.</summary>
        [HttpGet("societe/{idSociete:int}/reservation/{idRestaurantReservation:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetBySocieteAndReservation(
            int idSociete,
            int idRestaurantReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var tickets = await _ticketService.ListBySocieteAndReservationAsync(
                    effectiveSocieteId,
                    idRestaurantReservation,
                    cancellationToken);

                if (tickets == null)
                {
                    return NotFound(new
                    {
                        message = $"Réservation {idRestaurantReservation} introuvable ou n'appartient pas à la société {effectiveSocieteId}."
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
                    "Erreur GET tickets restaurant société {IdSociete} réservation {IdReservation}",
                    idSociete,
                    idRestaurantReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'une réservation (société JWT).</summary>
        [HttpGet("reservation/{idRestaurantReservation:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetByReservation(
            int idRestaurantReservation,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByReservationAsync(
                    idRestaurantReservation,
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
                _logger.LogError(ex, "Erreur GET tickets restaurant réservation {IdReservation}", idRestaurantReservation);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets d'un créneau restaurant.</summary>
        [HttpGet("creneau/{idRestaurantCreneau:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetByCreneau(
            int idRestaurantCreneau,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByCreneauAsync(
                    idRestaurantCreneau,
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
                _logger.LogError(ex, "Erreur GET tickets restaurant créneau {IdCreneau}", idRestaurantCreneau);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets par statut (ISSUED, USED, VOID).</summary>
        [HttpGet("status/{status}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<RestaurantTicketStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID."
                    });
                }

                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByStatusAsync(parsedStatus, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets restaurant statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par code (équivalent qrcode transport).</summary>
        [HttpGet("code/{ticketCode}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(RestaurantTicketDetailResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantTicketDetailResponseDto>> GetByCode(
            string ticketCode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticketCode))
                    return BadRequest(new { message = "Le paramètre ticketCode est obligatoire." });

                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByTicketCodeAsync(ticketCode, idSociete, cancellationToken);

                if (ticket == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucun ticket restaurant avec le code '{ticketCode.Trim()}'."
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
                _logger.LogError(ex, "Erreur GET ticket restaurant code {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis à une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var tickets = await _ticketService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(tickets);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET tickets restaurant date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les tickets émis entre deux dates (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantTicketListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<RestaurantTicketListItemDto>>> GetByDateRange(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dateFin < dateDebut)
                    return BadRequest(new { message = "dateFin doit être supérieure ou égale à dateDebut." });

                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                    "Erreur GET tickets restaurant plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'un ticket par identifiant numérique.</summary>
        [HttpGet("{id:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(RestaurantTicketDetailResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantTicketDetailResponseDto>> GetById(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var ticket = await _ticketService.GetByIdAsync(id, idSociete, cancellationToken);

                if (ticket == null)
                    return NotFound(new { message = $"Ticket restaurant {id} introuvable." });

                return Ok(ticket);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET ticket restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{ticketCode}/check")]
        [Permission("Restaurant.Ticket.Check")]
        public async Task<ActionResult<RestaurantTicketCheckResponseDto>> Check(string ticketCode)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _ticketService.CheckTicketAsync(ticketCode, idSociete);
                return StatusCode(result.HttpStatusCode, result.Response);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur check ticket restaurant {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{ticketCode}/use")]
        [Permission("Restaurant.Ticket.Use")]
        public async Task<ActionResult<RestaurantTicketUseResponseDto>> Use(string ticketCode)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                _logger.LogError(ex, "Erreur use ticket restaurant {TicketCode}", ticketCode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out RestaurantTicketStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<RestaurantTicketStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : ISSUED, USED, VOID.";
            return false;
        }
    }
}
