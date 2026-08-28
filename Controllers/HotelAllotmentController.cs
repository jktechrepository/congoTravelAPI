using CongoTravel.Attributes;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/allotments"), Authorize]
    public class HotelAllotmentController : ControllerBase
    {
        private readonly IHotelAllotmentService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelAllotmentController> _logger;

        public HotelAllotmentController(
            IHotelAllotmentService service,
            ICurrentUserService currentUser,
            ILogger<HotelAllotmentController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet, AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<HotelAllotmentResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<HotelAllotmentResponseDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idHotel,
            [FromQuery] int? idHotelRoomType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new HotelAllotmentListFilter
                {
                    IdSociete = idSociete,
                    IdHotel = idHotel,
                    IdHotelRoomType = idHotelRoomType,
                    From = from,
                    To = to
                };

                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUser, idSociete, out var tenant);

                if (staff && tenant.HasValue)
                {
                    if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                        return BadRequest(new { message = statusError });
                    filter.Status = parsedStatus;
                    return Ok(await _service.ListAsync(tenant.Value, filter, cancellationToken));
                }

                return Ok(await _service.ListPublishedGlobalAsync(filter, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste allotments hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        [ProducesResponseType(typeof(HotelAllotmentResponseDto), 200)]
        public async Task<ActionResult<HotelAllotmentResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUser, idSociete, out var tenant);
                var value = staff && tenant.HasValue
                    ? await _service.GetByIdAsync(id, tenant.Value, cancellationToken)
                    : await _service.GetPublishedByIdAsync(id, cancellationToken);
                return value == null
                    ? NotFound(new { message = $"Allotment {id} introuvable." })
                    : Ok(value);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost, Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelAllotmentResponseDto), 201)]
        public async Task<ActionResult<HotelAllotmentResponseDto>> CreateDraft(
            HotelCreateAllotmentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.CreateDraftAsync(
                    request,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = value.IdHotelNightAllotment }, value);
            }
            catch (HotelNightAllotmentConflictException ex)
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
        }

        [HttpPost("batch"), Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelAllotmentBatchResultDto), 200)]
        public async Task<ActionResult<HotelAllotmentBatchResultDto>> CreateDraftBatch(
            HotelCreateAllotmentBatchRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.CreateDraftBatchAsync(
                    request,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return Ok(value);
            }
            catch (HotelNightAllotmentConflictException ex)
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
        }

        [HttpPut("{id:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelAllotmentResponseDto>> Update(
            int id,
            HotelUpdateAllotmentRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.UpdateAsync(
                    id,
                    request,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return value == null
                    ? NotFound(new { message = $"Allotment {id} introuvable." })
                    : Ok(value);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPut("{id:int}/publish"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelAllotmentResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return Ok(await _service.PublishAsync(
                    id,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken));
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
        }

        private static bool TryParseOptionalStatus(string? status, out HotelStatus? parsed, out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;
            if (Enum.TryParse<HotelStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                parsed = value;
                return true;
            }

            error = $"Status invalide : '{status}'. Valeurs : Draft, Published, Closed, Cancelled.";
            return false;
        }
    }
}
