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
    [ApiController, Route("api/hotels/rooms"), Authorize]
    public class HotelRoomController : ControllerBase
    {
        private readonly IHotelRoomService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelRoomController> _logger;

        public HotelRoomController(
            IHotelRoomService service,
            ICurrentUserService currentUser,
            ILogger<HotelRoomController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet, Permission("Hotel.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<HotelRoomResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<HotelRoomResponseDto>>> GetList(
            [FromQuery] int? idHotel,
            [FromQuery] int? idHotelRoomType,
            [FromQuery] bool? isActif,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete);
                return Ok(await _service.ListAsync(societe, new HotelRoomListFilter
                {
                    IdHotel = idHotel,
                    IdHotelRoomType = idHotelRoomType,
                    IsActif = isActif
                }, cancellationToken));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste chambres hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}"), Permission("Hotel.Etablissement.Read")]
        [ProducesResponseType(typeof(HotelRoomResponseDto), 200)]
        public async Task<ActionResult<HotelRoomResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.GetByIdAsync(
                    id,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete),
                    cancellationToken);
                return value == null
                    ? NotFound(new { message = $"Chambre {id} introuvable." })
                    : Ok(value);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost, Permission("Hotel.Etablissement.Write")]
        [ProducesResponseType(typeof(HotelRoomResponseDto), 201)]
        public async Task<ActionResult<HotelRoomResponseDto>> Create(
            HotelCreateRoomRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.CreateAsync(
                    request,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = value.IdHotelRoom }, value);
            }
            catch (HotelRoomConflictException ex)
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
        public async Task<ActionResult<HotelRoomResponseDto>> Update(
            int id,
            HotelUpdateRoomRequestDto request,
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
                    ? NotFound(new { message = $"Chambre {id} introuvable." })
                    : Ok(value);
            }
            catch (HotelRoomConflictException ex)
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

        [HttpDelete("{id:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _service.DeleteAsync(
                    id,
                    HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    cancellationToken);
                return NoContent();
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
    }
}
