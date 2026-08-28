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
    [ApiController, Route("api/hotels/etablissements"), Authorize]
    public class HotelEtablissementController : ControllerBase
    {
        private readonly IHotelEtablissementService _service;
        private readonly IHotelPhotoService _photos;
        private readonly ICurrentUserService _currentUser;

        public HotelEtablissementController(IHotelEtablissementService service, IHotelPhotoService photos, ICurrentUserService currentUser)
        {
            _service = service; _photos = photos; _currentUser = currentUser;
        }

        [HttpGet, AllowAnonymous]
        public async Task<ActionResult<IEnumerable<HotelEtablissementListItemDto>>> GetList(
            [FromQuery] int? idSociete, [FromQuery] string? status, CancellationToken cancellationToken)
        {
            try
            {
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(_currentUser, idSociete, out var tenant);
                if (!staff || tenant == null)
                    return Ok(await _service.ListPublishedGlobalAsync(new() { IdSociete = idSociete is > 0 ? idSociete : null }, cancellationToken));
                if (!TryParseStatus(status, out var parsed)) return BadRequest(new { message = "Statut invalide. Valeurs : Draft, Published, Closed, Cancelled." });
                return Ok(await _service.ListAsync(tenant.Value, new() { Status = parsed }, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("{id:int}"), AllowAnonymous]
        public async Task<ActionResult<HotelEtablissementResponseDto>> GetById(int id, [FromQuery] int? idSociete, CancellationToken cancellationToken)
        {
            try
            {
                var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(_currentUser, idSociete, out var tenant);
                var value = staff && tenant.HasValue
                    ? await _service.GetByIdAsync(id, tenant.Value, cancellationToken)
                    : await _service.GetPublishedByIdAsync(id, cancellationToken);
                return value == null ? NotFound(new { message = $"Hôtel {id} introuvable." }) : Ok(value);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost, Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelEtablissementResponseDto>> CreateDraft(
            HotelCreateEtablissementRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var created = await _service.CreateDraftAsync(request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdHotel }, created);
            }
            catch (HotelConflictException ex) { return Conflict(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelEtablissementResponseDto>> Update(int id, HotelUpdateEtablissementRequestDto request, CancellationToken cancellationToken)
        {
            try
            {
                var value = await _service.UpdateAsync(id, request, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken);
                return value == null ? NotFound() : Ok(value);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("{id:int}/publish"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelEtablissementResponseDto>> Publish(int id, CancellationToken cancellationToken)
        {
            try { return Ok(await _service.PublishAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), cancellationToken)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("{id:int}/photos"), AllowAnonymous]
        public async Task<ActionResult<IEnumerable<HotelPhotoDto>>> GetPhotos(int id, [FromQuery] bool includePhotoBase64, CancellationToken cancellationToken)
        {
            var hotel = await ResolveForReadAsync(id, cancellationToken);
            if (hotel == null) return NotFound();
            var values = await _photos.GetByHotelIdAsync(id, hotel.IdSociete, cancellationToken, includePhotoBase64);
            return Ok(values.Select(p => HotelEtablissementMapper.ToPhotoDto(p, includePhotoBase64)));
        }

        [HttpGet("{id:int}/photos/{photoId:int}/content"), AllowAnonymous]
        public async Task<IActionResult> GetPhotoContent(int id, int photoId, CancellationToken cancellationToken)
        {
            var hotel = await ResolveForReadAsync(id, cancellationToken);
            if (hotel == null) return NotFound();
            var payload = await _photos.GetContentAsync(id, hotel.IdSociete, photoId, cancellationToken);
            return payload == null ? NotFound() : File(payload.Content, payload.ContentType);
        }

        [HttpPost("{id:int}/photos"), Consumes("application/json"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelPhotoDto>> AddPhoto(int id, AddHotelPhotoDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var photo = await _photos.AddPhotoAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), dto, cancellationToken);
                return CreatedAtAction(nameof(GetPhotos), new { id }, HotelEtablissementMapper.ToPhotoDto(photo));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id:int}/photos"), Consumes("multipart/form-data"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelPhotoDto>> AddPhotoMultipart(int id, [FromForm] IFormFile file,
            [FromForm] int? ordre, [FromForm] string? fileName, CancellationToken cancellationToken)
        {
            try
            {
                var photo = await _photos.AddPhotoFromFileAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), file, ordre, fileName, cancellationToken);
                return CreatedAtAction(nameof(GetPhotos), new { id }, HotelEtablissementMapper.ToPhotoDto(photo));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("{id:int}/photos"), Consumes("multipart/form-data"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<IEnumerable<HotelPhotoDto>>> ReplacePhotos(int id, [FromForm] List<IFormFile>? files,
            [FromForm] List<int>? ordres, CancellationToken cancellationToken)
        {
            try
            {
                var values = await _photos.ReplaceAllFromFilesAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser),
                    files ?? new(), ordres, cancellationToken);
                return Ok(values.Select(p => HotelEtablissementMapper.ToPhotoDto(p)));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("{id:int}/photos/{photoId:int}/ordre"), Permission("Hotel.Etablissement.Write")]
        public async Task<ActionResult<HotelPhotoDto>> UpdatePhotoOrdre(int id, int photoId, UpdateHotelPhotoOrdreDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var photo = await _photos.UpdateOrdreAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), photoId, dto.Ordre, cancellationToken);
                return photo == null ? NotFound() : Ok(HotelEtablissementMapper.ToPhotoDto(photo));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("{id:int}/photos/{photoId:int}"), Permission("Hotel.Etablissement.Write")]
        public async Task<IActionResult> DeletePhoto(int id, int photoId, CancellationToken cancellationToken)
        {
            var deleted = await _photos.DeletePhotoAsync(id, HotelTenancyGuard.ResolveEffectiveSocieteId(_currentUser), photoId, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }

        private async Task<HotelEtablissementResponseDto?> ResolveForReadAsync(int id, CancellationToken token)
        {
            if (_currentUser.IsSuperAdmin) return await _service.GetByIdAsync(id, cancellationToken: token);
            var staff = HotelTenancyGuard.TryResolveStaffTenantForCatalogList(_currentUser, null, out var tenant);
            return staff && tenant.HasValue ? await _service.GetByIdAsync(id, tenant, token) : await _service.GetPublishedByIdAsync(id, token);
        }

        private static bool TryParseStatus(string? value, out HotelStatus? status)
        {
            status = null;
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (!Enum.TryParse<HotelStatus>(value, true, out var parsed)) return false;
            status = parsed; return true;
        }
    }
}
