using CongoTravel.Attributes;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    [ApiController, Route("api/hotels/room-types"), Authorize]
    public class HotelRoomTypeController : ControllerBase
    {
        private readonly IHotelRoomTypeService _service;
        private readonly ICurrentUserService _currentUser;

        public HotelRoomTypeController(IHotelRoomTypeService service, ICurrentUserService currentUser)
        {
            _service = service; _currentUser = currentUser;
        }

        [HttpGet, AllowAnonymous]
        public async Task<ActionResult<IEnumerable<HotelRoomTypeResponseDto>>> GetList(
            [FromQuery] int? idSociete, [FromQuery] int? idHotel, CancellationToken cancellationToken)
        {
            try
            {
                var filter = new HotelRoomTypeListFilter { IdSociete = idSociete, IdHotel = idHotel };
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(_currentUser, idSociete, out var tenant);
                return Ok(staff && tenant.HasValue
                    ? await _service.ListAsync(tenant.Value, filter, cancellationToken)
                    : await _service.ListPublishedGlobalAsync(filter, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<ActionResult<HotelRoomTypeResponseDto>> GetById(int id, [FromQuery] int? idSociete, CancellationToken cancellationToken)
        {
            try
            {
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(_currentUser, idSociete, out var tenant);
                var value = staff && tenant.HasValue
                    ? await _service.GetByIdAsync(id, tenant.Value, cancellationToken)
                    : await _service.GetPublishedByIdAsync(id, cancellationToken);
                return value == null ? NotFound() : Ok(value);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost, Permission("Hotel.RoomType.Write")]
        public async Task<ActionResult<HotelRoomTypeResponseDto>> CreateDraft(HotelCreateRoomTypeRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.CreateDraftAsync(request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = value.IdHotelRoomType }, value);
            }
            catch (HotelRoomTypeConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id:int}"), Permission("Hotel.RoomType.Write")]
        public async Task<ActionResult<HotelRoomTypeResponseDto>> Update(int id, HotelUpdateRoomTypeRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.UpdateAsync(id, request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return value == null ? NotFound() : Ok(value);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id:int}/publish"), Permission("Hotel.RoomType.Write")]
        public async Task<ActionResult<HotelRoomTypeResponseDto>> Publish(int id, CancellationToken cancellationToken)
        {
            try { return Ok(await _service.PublishAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
