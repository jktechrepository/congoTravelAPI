using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/restaurants/etablissements")]
    [Authorize]
    public class RestaurantEtablissementController : ControllerBase
    {
        private readonly IRestaurantEtablissementService _etablissementService;
        private readonly IRestaurantPhotoService _photoService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantEtablissementController> _logger;

        public RestaurantEtablissementController(
            IRestaurantEtablissementService etablissementService,
            IRestaurantPhotoService photoService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantEtablissementController> logger)
        {
            _etablissementService = etablissementService;
            _photoService = photoService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RestaurantEtablissementListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<RestaurantEtablissementListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    var published = await _etablissementService.ListPublishedGlobalAsync(
                        new RestaurantEtablissementListFilter
                        {
                            IdSociete = idSociete is > 0 ? idSociete : null
                        },
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var etablissements = await _etablissementService.ListAsync(
                    effectiveSocieteId.Value,
                    new RestaurantEtablissementListFilter { Status = parsedStatus },
                    cancellationToken);
                return Ok(etablissements);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste établissements restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> GetById(
            int id,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var etablissement = await _etablissementService.GetByIdAsync(
                        id, effectiveSocieteId.Value, cancellationToken);
                    if (etablissement == null)
                        return NotFound(new { message = $"Établissement {id} introuvable." });
                    return Ok(etablissement);
                }

                var published = await _etablissementService.GetPublishedByIdAsync(id, cancellationToken);
                if (published == null)
                    return NotFound(new { message = $"Établissement Published {id} introuvable." });
                return Ok(published);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> CreateDraft(
            [FromBody] RestaurantCreateEtablissementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _etablissementService.CreateDraftAsync(request, idSociete, cancellationToken);
                return CreatedAtAction(nameof(GetById), new { id = created.IdRestaurant }, created);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (RestaurantConflictException ex)
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
                _logger.LogError(ex, "Erreur POST établissement restaurant");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> Update(
            int id,
            [FromBody] RestaurantUpdateEtablissementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var updated = await _etablissementService.UpdateAsync(id, request, idSociete, cancellationToken);
                if (updated == null)
                    return NotFound(new { message = $"Établissement {id} introuvable." });
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
                _logger.LogError(ex, "Erreur PUT établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantEtablissementResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantEtablissementResponseDto>> Publish(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _etablissementService.PublishAsync(id, idSociete, cancellationToken);
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
                _logger.LogError(ex, "Erreur publish établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les photos d'un établissement (max 3).</summary>
        [HttpGet("{id:int}/photos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<RestaurantPhotoDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<RestaurantPhotoDto>>> GetPhotos(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var etablissement = await ResolveEtablissementForPublicReadAsync(id, cancellationToken);
                if (etablissement == null)
                    return NotFound(new { message = $"Établissement {id} introuvable." });

                var photos = await _photoService.GetByRestaurantIdAsync(id, etablissement.IdSociete, cancellationToken);
                return Ok(photos.Select(RestaurantEtablissementMapper.ToPhotoDto).ToList());
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
                _logger.LogError(ex, "Erreur GET photos établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Ajoute une photo à un établissement (max 3).</summary>
        [HttpPost("{id:int}/photos")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantPhotoDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantPhotoDto>> AddPhoto(
            int id,
            [FromBody] AddRestaurantPhotoDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.AddPhotoAsync(id, idSociete, dto, cancellationToken);
                return CreatedAtAction(
                    nameof(GetPhotos),
                    new { id },
                    RestaurantEtablissementMapper.ToPhotoDto(photo));
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
                _logger.LogError(ex, "Erreur POST photo établissement restaurant {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Met à jour l'ordre d'affichage d'une photo (1..3).</summary>
        [HttpPut("{id:int}/photos/{photoId:int}/ordre")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(typeof(RestaurantPhotoDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<RestaurantPhotoDto>> UpdatePhotoOrdre(
            int id,
            int photoId,
            [FromBody] UpdateRestaurantPhotoOrdreDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.UpdateOrdreAsync(id, idSociete, photoId, dto.Ordre, cancellationToken);
                if (photo == null)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour l'établissement {id}." });

                return Ok(RestaurantEtablissementMapper.ToPhotoDto(photo));
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
                _logger.LogError(ex, "Erreur PUT ordre photo {PhotoId} établissement {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Supprime une photo d'établissement.</summary>
        [HttpDelete("{id:int}/photos/{photoId:int}")]
        [Permission("Restaurant.Etablissement.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePhoto(
            int id,
            int photoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var deleted = await _photoService.DeletePhotoAsync(id, idSociete, photoId, cancellationToken);
                if (!deleted)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour l'établissement {id}." });

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
                _logger.LogError(ex, "Erreur DELETE photo {PhotoId} établissement {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        private async Task<RestaurantEtablissementResponseDto?> ResolveEtablissementForPublicReadAsync(
            int id,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.IsSuperAdmin)
                return await _etablissementService.GetByIdAsync(id, cancellationToken: cancellationToken);

            var isStaffTenant = RestaurantTenancyGuard.TryResolveStaffTenantForCatalogList(
                _currentUserService,
                null,
                out var effectiveSocieteId);

            if (isStaffTenant && effectiveSocieteId.HasValue)
                return await _etablissementService.GetByIdAsync(id, effectiveSocieteId.Value, cancellationToken);

            return await _etablissementService.GetPublishedByIdAsync(id, cancellationToken);
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out RestaurantStatus? parsed,
            out string? error)
        {
            parsed = null;
            error = null;
            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (!Enum.TryParse<RestaurantStatus>(status.Trim(), ignoreCase: true, out var value))
            {
                error = $"Statut invalide '{status}'. Valeurs : Draft, Published, Closed, Cancelled.";
                return false;
            }

            parsed = value;
            return true;
        }
    }
}
