using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/flexpay")]
    public class EvenementFlexPayController : ControllerBase
    {
        private readonly IEvenementFlexPayCallbackService _callbackService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementFlexPayController> _logger;

        public EvenementFlexPayController(
            IEvenementFlexPayCallbackService callbackService,
            ICurrentUserService currentUserService,
            ILogger<EvenementFlexPayController> logger)
        {
            _callbackService = callbackService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Callback public FlexPay événement (sans JWT). Confirme la réservation HOLD si code = 0.
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementFlexPayCallbackProcessResultDto), 200)]
        public async Task<ActionResult<EvenementFlexPayCallbackProcessResultDto>> Callback(
            [FromBody] FlexPayCallbackDto callback)
        {
            try
            {
                var raw = JsonSerializer.Serialize(callback);
                _logger.LogInformation(
                    "Callback FlexPay événement reçu — Order={OrderNumber}, Code={Code}, Payload={Payload}",
                    callback.OrderNumber,
                    callback.Code,
                    raw);

                var result = await _callbackService.ProcessCallbackAsync(callback);
                return Ok(new { message = result.Message, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur callback FlexPay événement");
                return StatusCode(500, new { message = "Erreur interne callback FlexPay événement" });
            }
        }

        /// <summary>
        /// Secours : vérifie le statut chez FlexPay et finalise la réservation HOLD si succès.
        /// </summary>
        [HttpGet("verifier/{orderNumber}")]
        [Authorize]
        [Permission("Evenement.Reservation.Confirm")]
        [ProducesResponseType(typeof(EvenementConfirmPaymentResponseDto), 200)]
        [ProducesResponseType(typeof(EvenementFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Verifier(string orderNumber)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
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
                _logger.LogError(ex, "Erreur vérification FlexPay événement {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Abandon explicite (JWT) : bouton Annuler de l’app — FAILED + libération HOLD + SignalR Failed.
        /// Indispensable pour Mobile Money (FlexPay n’appelle pas cancel_url).
        /// </summary>
        [HttpPost("abandon/{orderNumber}")]
        [Authorize]
        [Permission("Evenement.Reservation.Confirm")]
        [ProducesResponseType(typeof(EvenementFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Abandon(string orderNumber)
        {
            try
            {
                _ = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                return await AbandonAsync(orderNumber, EvenementFlexPayCallbackService.CancelMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpGet("approve")]
        [AllowAnonymous]
        public IActionResult Approve([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement événement en cours de confirmation.", orderNumber });

        /// <summary>
        /// Redirection FlexPay (annulation utilisateur) : FAILED + libération HOLD + SignalR Failed.
        /// </summary>
        [HttpGet("cancel")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Cancel([FromQuery] string? orderNumber)
        {
            return await AbandonAsync(orderNumber, EvenementFlexPayCallbackService.CancelMessage);
        }

        /// <summary>
        /// Redirection FlexPay (refus) : FAILED + libération HOLD + SignalR Failed.
        /// </summary>
        [HttpGet("decline")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementFlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Decline([FromQuery] string? orderNumber)
        {
            return await AbandonAsync(orderNumber, EvenementFlexPayCallbackService.DeclineMessage);
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
                    idEvenementReservation = result.IdEvenementReservation,
                    idEvenementPayment = result.IdEvenementPayment,
                    orderNumber
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message, paymentPending = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur abandon FlexPay événement {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Une erreur interne est survenue.", paymentPending = false });
            }
        }
    }
}
