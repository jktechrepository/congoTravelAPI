using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/sites-touristiques/lieux")]
    [Authorize]
    public class SiteTouristiqueLieuController : ControllerBase
    {
        private readonly ISiteTouristiqueLieuService _lieuService;
        private readonly ISiteTouristiqueLieuPhotoService _photoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueLieuController> _logger;

        public SiteTouristiqueLieuController(
            ISiteTouristiqueLieuService lieuService,
            ISiteTouristiqueLieuPhotoService photoService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueLieuController> logger)
        {
            _lieuService = lieuService;
            _photoService = photoService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueLieuListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueLieuListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    var published = await _lieuService.ListPublishedGlobalAsync(
                        new SiteTouristiqueLieuListFilter
                        {
                            IdSociete = idSociete is > 0 ? idSociete : null
                        },
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var lieux = await _lieuService.ListAsync(
                    effectiveSocieteId.Value,
                    new SiteTouristiqueLieuListFilter { Status = parsedStatus },
                    cancellationToken);
                return Ok(lieux);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste lieux site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("societe/{idSociete:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueLieuListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueLieuListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var lieux = await _lieuService.ListAsync(effectiveSocieteId, cancellationToken: cancellationToken);
                return Ok(lieux);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET lieux site touristique société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("code/{codeLieu}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SiteTouristiqueLieuResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuResponseDto>> GetByCode(
            string codeLieu,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codeLieu))
                    return BadRequest(new { message = "Le paramètre codeLieu est obligatoire." });

                var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var lieu = await _lieuService.GetByCodeAsync(codeLieu, effectiveSocieteId.Value, cancellationToken);
                    if (lieu == null)
                        return NotFound(new { message = $"Aucun lieu avec le code '{codeLieu.Trim()}'." });
                    return Ok(lieu);
                }

                var published = await _lieuService.GetPublishedByCodeAsync(
                    codeLieu,
                    idSociete is > 0 ? idSociete : null,
                    cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Aucun lieu Published avec le code '{codeLieu.Trim()}'." });
                return Ok(published);
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
                _logger.LogError(ex, "Erreur GET lieu code {CodeLieu}", codeLieu);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(SiteTouristiqueLieuResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var lieu = await _lieuService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);
                    if (lieu == null)
                        return NotFound(new { message = $"Lieu {id} introuvable." });
                    return Ok(lieu);
                }

                var published = await _lieuService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Lieu Published {id} introuvable." });
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET lieu {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueLieuResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<SiteTouristiqueLieuResponseDto>> CreateDraft(
            [FromBody] SiteTouristiqueCreateLieuRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _lieuService.CreateDraftAsync(request, idSociete, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdSiteTouristique }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (SiteTouristiqueLieuConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST lieu site touristique");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueLieuResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuResponseDto>> Update(
            int id,
            [FromBody] SiteTouristiqueUpdateLieuRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _lieuService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Lieu {id} introuvable." });
                return Ok(updated);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur PUT lieu {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueLieuResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _lieuService.PublishAsync(id, idSociete, cancellationToken);
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur publish lieu {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les photos d'un lieu (max 3).</summary>
        [HttpGet("{id:int}/photos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<SiteTouristiqueLieuPhotoDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<SiteTouristiqueLieuPhotoDto>>> GetPhotos(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var lieu = await ResolveLieuForPublicReadAsync(id, cancellationToken);
                if (lieu == null)
                    return NotFound(new { message = $"Lieu {id} introuvable." });

                var photos = await _photoService.GetByLieuIdAsync(id, lieu.IdSociete, cancellationToken);
                return Ok(photos.Select(SiteTouristiqueLieuMapper.ToPhotoDto).ToList());
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET photos lieu site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Ajoute une photo à un lieu (max 3).</summary>
        [HttpPost("{id:int}/photos")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueLieuPhotoDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuPhotoDto>> AddPhoto(
            int id,
            [FromBody] AddSiteTouristiqueLieuPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.AddPhotoAsync(id, idSociete, dto, cancellationToken);
                return CreatedAtAction(
                    nameof(GetPhotos),
                    new { id },
                    SiteTouristiqueLieuMapper.ToPhotoDto(photo));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST photo lieu site touristique {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Met à jour l'ordre d'affichage d'une photo (1..3).</summary>
        [HttpPut("{id:int}/photos/{photoId:int}/ordre")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(typeof(SiteTouristiqueLieuPhotoDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteTouristiqueLieuPhotoDto>> UpdatePhotoOrdre(
            int id,
            int photoId,
            [FromBody] UpdateSiteTouristiqueLieuPhotoOrdreDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.UpdateOrdreAsync(id, idSociete, photoId, dto.Ordre, cancellationToken);
                if (photo == null)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour le lieu {id}." });

                return Ok(SiteTouristiqueLieuMapper.ToPhotoDto(photo));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur PUT ordre photo {PhotoId} lieu {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Supprime une photo de lieu.</summary>
        [HttpDelete("{id:int}/photos/{photoId:int}")]
        [Permission("SiteTouristique.Lieu.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePhoto(
            int id,
            int photoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var deleted = await _photoService.DeletePhotoAsync(id, idSociete, photoId, cancellationToken);
                if (!deleted)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour le lieu {id}." });

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur DELETE photo {PhotoId} lieu {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private async Task<SiteTouristiqueLieuResponseDto?> ResolveLieuForPublicReadAsync(
            int id,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.IsSuperAdmin)
                return await _lieuService.GetByIdAsync(id, cancellationToken: cancellationToken);

            var isStaffTenant = SiteTouristiqueTenancyGuard.TryResolveStaffTenantForCatalogList(
                _currentUserService,
                null,
                out var effectiveSocieteId);

            if (isStaffTenant && effectiveSocieteId.HasValue)
                return await _lieuService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);

            return await _lieuService.GetPublishedByIdAsync(id, cancellationToken);
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out SiteTouristiqueStatus? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (!Enum.TryParse<SiteTouristiqueStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                error = $"Statut invalide '{status}'. Valeurs : Draft, Published, Closed, Cancelled.";
                return false;
            }

            parsed = value;
            return true;
        }
    }
}
