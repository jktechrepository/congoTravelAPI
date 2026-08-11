using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/flexpay")]
    public class RestaurantFlexPayController : ControllerBase
    {
        private readonly IRestaurantFlexPayCallbackService _callbackService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantFlexPayController> _logger;

        public RestaurantFlexPayController(
            IRestaurantFlexPayCallbackService callbackService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantFlexPayController> logger)
        {
            _callbackService = callbackService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Callback public FlexPay restaurant (sans JWT). Confirme la réservation HOLD si code = 0.
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantFlexPayCallbackProcessResultDto), 200)]
        public async Task<ActionResult<RestaurantFlexPayCallbackProcessResultDto>> Callback(
            [FromBody] FlexPayCallbackDto callback)
        {
            try
            {
                var raw = JsonSerializer.Serialize(callback);
                _logger.LogInformation(
                    "Callback FlexPay restaurant reçu — Order={OrderNumber}, Code={Code}, Payload={Payload}",
                    callback.OrderNumber,
                    callback.Code,
                    raw);

                var result = await _callbackService.ProcessCallbackAsync(callback);
                return Ok(new { message = result.Message, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur callback FlexPay restaurant");
                return StatusCode(500, new { message = "Erreur interne callback FlexPay restaurant" });
            }
        }

        /// <summary>
        /// Secours : vérifie le statut chez FlexPay et finalise la réservation HOLD si succès.
        /// </summary>
        [HttpGet("verifier/{orderNumber}")]
        [Authorize]
        [Permission("Restaurant.Reservation.Confirm")]
        [ProducesResponseType(typeof(RestaurantConfirmPaymentResponseDto), 200)]
        [ProducesResponseType(typeof(RestaurantFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Verifier(string orderNumber)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var result = await _callbackService.VerifyAndFinalizeAsync(orderNumber, idSociete);
                if (result.IsConfirmSuccess)
                    return Ok(result.ConfirmPayment);
                return Ok(result.StatusOnly);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
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
                _logger.LogError(ex, "Erreur vérification FlexPay restaurant {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Abandon explicite (JWT) : bouton Annuler de l’app — FAILED + libération HOLD + SignalR Failed.
        /// Indispensable pour Mobile Money (FlexPay n’appelle pas cancel_url).
        /// </summary>
        [HttpPost("abandon/{orderNumber}")]
        [Authorize]
        [Permission("Restaurant.Reservation.Confirm")]
        [ProducesResponseType(typeof(RestaurantFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Abandon(string orderNumber)
        {
            try
            {
                _ = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                return await AbandonAsync(orderNumber, RestaurantFlexPayCallbackService.CancelMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("approve")]
        [AllowAnonymous]
        public IActionResult Approve([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement restaurant en cours de confirmation.", orderNumber });

        /// <summary>
        /// Redirection FlexPay (annulation utilisateur) : FAILED + libération HOLD + SignalR Failed.
        /// </summary>
        [HttpGet("cancel")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Cancel([FromQuery] string? orderNumber)
        {
            return await AbandonAsync(orderNumber, RestaurantFlexPayCallbackService.CancelMessage);
        }

        /// <summary>
        /// Redirection FlexPay (refus) : FAILED + libération HOLD + SignalR Failed.
        /// </summary>
        [HttpGet("decline")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Decline([FromQuery] string? orderNumber)
        {
            return await AbandonAsync(orderNumber, RestaurantFlexPayCallbackService.DeclineMessage);
        }

        private async Task<IActionResult> AbandonAsync(string? orderNumber, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderNumber))
                    return BadRequest(new { message = "orderNumber requis.", paymentPending = false });

                var result = await _callbackService.AbandonPendingPaymentAsync(orderNumber, message);
                return Ok(new
                {
                    success = result.Success,
                    paymentPending = result.PaymentPending,
                    message = result.Message,
                    idRestaurantReservation = result.IdRestaurantReservation,
                    idRestaurantPayment = result.IdRestaurantPayment,
                    orderNumber
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, paymentPending = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur abandon FlexPay restaurant {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Une erreur interne est survenue.", paymentPending = false });
            }
        }
    }
}
