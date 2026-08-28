using System.Text.Json;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/flexpay")]
    public class HotelFlexPayController : ControllerBase
    {
        private readonly IHotelFlexPayCallbackService _callback;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelFlexPayController> _logger;
        public HotelFlexPayController(IHotelFlexPayCallbackService callback,
            ICurrentUserService currentUser, ILogger<HotelFlexPayController> logger)
        {
            _callback = callback; _currentUser = currentUser; _logger = logger;
        }

        [HttpPost("callback"), AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] FlexPayCallbackDto callback)
        {
            _logger.LogInformation("Callback FlexPay hôtel reçu — Order={Order}, Code={Code}, Payload={Payload}",
                callback.OrderNumber, callback.Code, JsonSerializer.Serialize(callback));
            var result = await _callback.ProcessCallbackAsync(callback);
            return Ok(new { message = result.Message, result });
        }

        [HttpGet("verifier/{orderNumber}"), Authorize]
        [Permission("Hotel.Reservation.Confirm")]
        public async Task<IActionResult> Verifier(
            string orderNumber, [FromQuery] int? idSociete, CancellationToken cancellationToken)
        {
            try
            {
                var company = HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(
                    _currentUser, idSociete);
                var result = await _callback.VerifyAndFinalizeAsync(orderNumber, company, cancellationToken);
                return Ok(result.IsConfirmSuccess ? result.ConfirmPayment : result.StatusOnly);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("abandon/{orderNumber}"), Authorize]
        [Permission("Hotel.Reservation.Confirm")]
        public async Task<IActionResult> Abandon(string orderNumber, CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _callback.AbandonPendingPaymentAsync(orderNumber, HotelFlexPayCallbackService.CancelMessage, cancellationToken);
                return Ok(payment);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("approve"), AllowAnonymous]
        public IActionResult Approve([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement hôtel en cours de confirmation.", orderNumber });

        [HttpGet("cancel"), AllowAnonymous]
        public Task<IActionResult> Cancel([FromQuery] string? orderNumber, CancellationToken ct) =>
            AbandonRedirect(orderNumber, HotelFlexPayCallbackService.CancelMessage, ct);

        [HttpGet("decline"), AllowAnonymous]
        public Task<IActionResult> Decline([FromQuery] string? orderNumber, CancellationToken ct) =>
            AbandonRedirect(orderNumber, HotelFlexPayCallbackService.DeclineMessage, ct);

        private async Task<IActionResult> AbandonRedirect(string? orderNumber, string message, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return BadRequest(new { message = "orderNumber requis.", paymentPending = false });
            var result = await _callback.AbandonPendingPaymentAsync(orderNumber, message, ct);
            return Ok(result);
        }

    }
}
