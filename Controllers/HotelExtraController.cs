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
    [ApiController, Route("api/hotels/extras"), Authorize]
    public class HotelExtraController : ControllerBase
    {
        private readonly IHotelExtraService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<HotelExtraController> _logger;

        public HotelExtraController(
            IHotelExtraService service,
            ICurrentUserService currentUser,
            ILogger<HotelExtraController> logger)
        {
            _service = service;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet, Permission("Hotel.Etablissement.Read")]
        [ProducesResponseType(typeof(IEnumerable<HotelExtraResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<HotelExtraResponseDto>>> GetList(
            [FromQuery] int? idHotel,
            [FromQuery] bool? isActif,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var societe = HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete);
                return Ok(await _service.ListAsync(societe, new HotelExtraListFilter
                {
                    IdHotel = idHotel,
                    IsActif = isActif
                }, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste extras hôtel");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}"), Permission("Hotel.Etablissement.Read")]
        public async Task<ActionResult<HotelExtraResponseDto>> GetById(
            int id, [FromQuery] int? idSociete = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.GetByIdAsync(
                    id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser, idSociete), cancellationToken);
                return value == null
                    ? NotFound(new { message = $"Extra {id} introuvable." })
                    : Ok(value);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost, Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelExtraResponseDto>> Create(
            HotelCreateExtraRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.CreateAsync(
                    request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = value.IdHotelExtra }, value);
            }
            catch (HotelExtraConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelExtraResponseDto>> Update(
            int id, HotelUpdateExtraRequestDto request, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _service.UpdateAsync(
                    id, request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return value == null
                    ? NotFound(new { message = $"Extra {id} introuvable." })
                    : Ok(value);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("{id:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                await _service.DeleteAsync(
                    id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
