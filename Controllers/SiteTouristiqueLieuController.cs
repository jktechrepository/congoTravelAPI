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
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueLieuController> _logger;

        public SiteTouristiqueLieuController(
            ISiteTouristiqueLieuService lieuService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueLieuController> logger)
        {
            _lieuService = lieuService;
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
