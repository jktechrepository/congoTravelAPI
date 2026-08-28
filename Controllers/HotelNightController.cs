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
    [ApiController, Route("api/hotels/nights"), Authorize]
    public class HotelNightController : ControllerBase
    {
        private readonly IHotelNightService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelNightController> _logger;

        public HotelNightController(
            IHotelNightService service,
            ICurrentUserService currentUser,
            ILogger<HotelNightController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet, AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<HotelNightResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<HotelNightResponseDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] int? idHotel,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = new HotelNightListFilter
                {
                    IdSociete = idSociete,
                    IdHotel = idHotel,
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
                _logger.LogError(ex, "Erreur GET liste nuits hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        [ProducesResponseType(typeof(HotelNightResponseDto), 200)]
        public async Task<ActionResult<HotelNightResponseDto>> GetById(
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
                    ? NotFound(new { message = $"Nuit {id} introuvable." })
                    : Ok(value);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost, Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelNightResponseDto), 201)]
        public async Task<ActionResult<HotelNightResponseDto>> CreateDraft(
            HotelCreateNightRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.CreateDraftAsync(
                    request,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = value.IdHotelNight }, value);
            }
            catch (HotelNightConflictException ex)
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
        [ProducesResponseType(typeof(HotelNightBatchResultDto), 200)]
        public async Task<ActionResult<HotelNightBatchResultDto>> CreateDraftBatch(
            HotelCreateNightBatchRequestDto request,
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
            catch (HotelNightConflictException ex)
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
        public async Task<ActionResult<HotelNightResponseDto>> Update(
            int id,
            HotelUpdateNightRequestDto request,
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
                    ? NotFound(new { message = $"Nuit {id} introuvable." })
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
        public async Task<ActionResult<HotelNightResponseDto>> Publish(
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
