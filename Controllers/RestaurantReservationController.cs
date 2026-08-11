using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/reservations")]
    [Authorize]
    public class RestaurantReservationController : ControllerBase
    {
        private readonly IRestaurantReservationService _reservationService;
        private readonly IRestaurantReservationWithPaiementService _reservationWithPaiementService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantReservationController> _logger;

        public RestaurantReservationController(
            IRestaurantReservationService reservationService,
            IRestaurantReservationWithPaiementService reservationWithPaiementService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantReservationController> logger)
        {
            _reservationService = reservationService;
            _reservationWithPaiementService = reservationWithPaiementService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<RestaurantReservationListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantReservationListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] int? idRestaurant,
            [FromQuery] int? idRestaurantCreneau,
            [FromQuery] string? customerRef,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new RestaurantReservationListFilter
                {
                    Status = parsedStatus,
                    IdRestaurant = idRestaurant,
                    IdRestaurantCreneau = idRestaurantCreneau,
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
                _logger.LogError(ex, "Erreur GET liste réservations restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [Permission("Restaurant.Etablissement.Read")]
        [ProducesResponseType(typeof(RestaurantReservationResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantReservationResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                var reservation = await _reservationService.GetByIdAsync(id, effectiveSocieteId, cancellationToken);
                if (reservation == null)
                    return NotFound(new { message = $"Réservation restaurant {id} introuvable." });

                return Ok(reservation);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET réservation restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Parcours CASH : hold + confirm acompte en un appel.</summary>
        [HttpPost("with-paiement")]
        [Permission("Restaurant.Hold.Create")]
        [Permission("Restaurant.Reservation.Confirm")]
        [ProducesResponseType(typeof(RestaurantReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantReservationWithPaiementResponseDto>> CreateWithPaiement(
            [FromBody] RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _reservationWithPaiementService.CreateCashAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (RestaurantHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur with-paiement réservation restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Hold + initiation FlexPay acompte.</summary>
        [HttpPost("with-paiement-electronique")]
        [Permission("Restaurant.Hold.Create")]
        [Permission("Restaurant.Reservation.Confirm")]
        [ProducesResponseType(typeof(RestaurantReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<RestaurantReservationWithPaiementResponseDto>> CreateWithPaiementElectronique(
            [FromBody] RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _reservationWithPaiementService.InitiateElectronicAsync(
                    request,
                    cancellationToken);
                return Ok(result);
            }
            catch (RestaurantHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur with-paiement-electronique réservation restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost("{id:int}/cancel")]
        [Permission("Restaurant.Reservation.Confirm")]
        [ProducesResponseType(typeof(RestaurantCancelReservationResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantCancelReservationResponseDto>> Cancel(
            int id,
            [FromQuery] int? idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);

                var result = await _reservationService.CancelAsync(id, effectiveSocieteId, cancellationToken);
                return Ok(result);
            }
            catch (RestaurantHoldConflictException ex)
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
                _logger.LogError(ex, "Erreur cancel réservation restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out RestaurantReservationStatus? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<RestaurantReservationStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                parsed = value;
                return true;
            }

            error = $"Statut invalide : {status}. Valeurs : HOLD, CONFIRMED, CANCELLED, EXPIRED.";
            return false;
        }
    }
}
