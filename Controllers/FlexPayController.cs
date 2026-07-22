using System.Text.Json;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.DTOs.ReversementSite;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlexPayController : ControllerBase
    {
        private readonly IFlexPayCallbackService _callbackService;
        private readonly IFlexPayPayOutCallbackService _payOutCallbackService;
        private readonly ILogger<FlexPayController> _logger;

        public FlexPayController(
            IFlexPayCallbackService callbackService,
            IFlexPayPayOutCallbackService payOutCallbackService,
            ILogger<FlexPayController> logger)
        {
            _callbackService = callbackService;
            _payOutCallbackService = payOutCallbackService;
            _logger = logger;
        }

        /// <summary>
        /// Callback public FlexPay (sans JWT). Crée la réservation si code = 0.
        /// </summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FlexPayCallbackProcessResultDto), 200)]
        public async Task<ActionResult<FlexPayCallbackProcessResultDto>> Callback([FromBody] FlexPayCallbackDto callback)
        {
            try
            {
                var raw = JsonSerializer.Serialize(callback);
                var headers = string.Join("; ", Request.Headers.Select(h => $"{h.Key}={h.Value}"));
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _callbackService.ProcessCallbackAsync(callback, raw, headers, ip);
                return Ok(new { message = result.Message, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur callback FlexPay");
                return StatusCode(500, new { message = "Erreur interne callback FlexPay" });
            }
        }

        /// <summary>
        /// Callback public FlexPay PayOut (sans JWT). Met à jour le reversement site.
        /// </summary>
        [HttpPost("payout/callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FlexPayPayOutCallbackProcessResultDto), 200)]
        public async Task<ActionResult<FlexPayPayOutCallbackProcessResultDto>> PayOutCallback([FromBody] FlexPayCallbackDto callback)
        {
            try
            {
                var raw = JsonSerializer.Serialize(callback);
                var headers = string.Join("; ", Request.Headers.Select(h => $"{h.Key}={h.Value}"));
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var result = await _payOutCallbackService.ProcessCallbackAsync(callback, raw, headers, ip);
                return Ok(new { message = result.Message, result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur callback PayOut FlexPay");
                return StatusCode(500, new { message = "Erreur interne callback PayOut FlexPay" });
            }
        }

        /// <summary>
        /// Secours : vérifie le statut chez FlexPay et finalise si succès.
        /// </summary>
        [HttpGet("verifier/{orderNumber}")]
        [Authorize]
        [ProducesResponseType(typeof(ReservationWithPaiementResponseDto), 200)]
        [ProducesResponseType(typeof(FlexPayCallbackProcessResultDto), 200)]
        public async Task<IActionResult> Verifier(string orderNumber)
        {
            try
            {
                var result = await _callbackService.VerifyAndFinalizeAsync(orderNumber);
                if (result.IsUnifiedSuccess)
                    return Ok(result.ReservationWithPaiement);
                return Ok(result.StatusOnly);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur vérification FlexPay {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Erreur interne" });
            }
        }

        [HttpGet("approve")]
        [AllowAnonymous]
        public IActionResult Approve([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement en cours de confirmation.", orderNumber });

        [HttpGet("cancel")]
        [AllowAnonymous]
        public IActionResult Cancel([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement annulé.", orderNumber });

        [HttpGet("decline")]
        [AllowAnonymous]
        public IActionResult Decline([FromQuery] string? orderNumber) =>
            Ok(new { message = "Paiement refusé.", orderNumber });
    }
}
