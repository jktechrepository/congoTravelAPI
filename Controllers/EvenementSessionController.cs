using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/events/sessions")]
    [Authorize]
    public class EvenementSessionController : ControllerBase
    {
        private readonly IEvenementSessionService _sessionService;
        private readonly IEvenementSessionPhotoService _photoService;
        private readonly IEvenementAvailabilityService _availabilityService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementSessionController> _logger;

        public EvenementSessionController(
            IEvenementSessionService sessionService,
            IEvenementSessionPhotoService photoService,
            IEvenementAvailabilityService availabilityService,
            ICurrentUserService currentUserService,
            ILogger<EvenementSessionController> logger)
        {
            _sessionService = sessionService;
            _photoService = photoService;
            _availabilityService = availabilityService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Liste les sessions événement.
        /// Anonyme / Client / non-staff : catalogue Published global (filtre optionnel idSociete libre).
        /// Personnel société (IsStaff) : sessions de sa société ; mismatch idSociete → 403.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetList(
            [FromQuery] int? idSociete,
            [FromQuery] string? status,
            [FromQuery] string? inventoryMode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!TryParseOptionalInventoryMode(inventoryMode, out var parsedMode, out var modeError))
                    return BadRequest(new { message = modeError });

                var isStaffTenant = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (!isStaffTenant || !effectiveSocieteId.HasValue)
                {
                    // Catalogue public / Client : Published + filtre idSociete optionnel
                    var publicFilter = new EvenementSessionListFilter
                    {
                        InventoryMode = parsedMode,
                        IdSociete = idSociete is > 0 ? idSociete : null
                    };
                    var published = await _sessionService.ListPublishedGlobalAsync(
                        publicFilter,
                        cancellationToken);
                    return Ok(published);
                }

                if (!TryParseOptionalStatus(status, out var parsedStatus, out var statusError))
                    return BadRequest(new { message = statusError });

                var filter = new EvenementSessionListFilter
                {
                    Status = parsedStatus,
                    InventoryMode = parsedMode
                };

                var sessions = await _sessionService.ListAsync(
                    effectiveSocieteId.Value,
                    filter,
                    cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET liste sessions événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les sessions événement d'une société (alias explicite).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetBySociete(
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var effectiveSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(
                    _currentUserService,
                    idSociete);
                var sessions = await _sessionService.ListAsync(effectiveSocieteId, cancellationToken: cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET sessions événement société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les sessions par statut (Draft, Published, Closed, Cancelled).</summary>
        [HttpGet("status/{status}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetByStatus(
            string status,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<EvenementSessionStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return BadRequest(new
                    {
                        message = $"Statut invalide '{status}'. Valeurs acceptées : Draft, Published, Closed, Cancelled."
                    });
                }

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var sessions = await _sessionService.ListByStatusAsync(parsedStatus, idSociete, cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET sessions événement statut {Status}", status);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les sessions par mode d'inventaire.</summary>
        [HttpGet("inventory-mode/{inventoryMode}")]
        [Permission("Evenement.Session.Read")]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetByInventoryMode(
            string inventoryMode,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Enum.TryParse<EvenementInventoryMode>(inventoryMode, ignoreCase: true, out var parsedMode))
                {
                    return BadRequest(new
                    {
                        message = $"InventoryMode invalide '{inventoryMode}'. Valeurs acceptées : GlobalQuota, ClassQuota, SeatNumbered."
                    });
                }

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var sessions = await _sessionService.ListByInventoryModeAsync(parsedMode, idSociete, cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET sessions événement mode {InventoryMode}", inventoryMode);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'une session par code (unique par société).</summary>
        [HttpGet("code/{codeSession}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementSessionResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementSessionResponseDto>> GetByCode(
            string codeSession,
            [FromQuery] int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codeSession))
                    return BadRequest(new { message = "Le paramètre codeSession est obligatoire." });

                if (_currentUserService.IsSuperAdmin)
                {
                    if (idSociete is > 0)
                    {
                        var bySociete = await _sessionService.GetByCodeAsync(
                            codeSession, idSociete.Value, cancellationToken);
                        if (bySociete == null)
                        {
                            return NotFound(new
                            {
                                message = $"Aucune session événement avec le code '{codeSession.Trim()}'."
                            });
                        }

                        return Ok(bySociete);
                    }

                    // Sans idSociete : même résolution que le catalogue public (Published, code unique)
                    var publishedSa = await _sessionService.GetPublishedByCodeAsync(
                        codeSession, null, cancellationToken);
                    if (publishedSa == null)
                    {
                        return NotFound(new
                        {
                            message = $"Aucune session Published avec le code '{codeSession.Trim()}'."
                        });
                    }

                    return Ok(publishedSa);
                }

                var isStaffTenant = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(
                    _currentUserService,
                    idSociete,
                    out var effectiveSocieteId);

                if (isStaffTenant && effectiveSocieteId.HasValue)
                {
                    var session = await _sessionService.GetByCodeAsync(
                        codeSession, effectiveSocieteId.Value, cancellationToken);
                    if (session == null)
                    {
                        return NotFound(new
                        {
                            message = $"Aucune session événement avec le code '{codeSession.Trim()}'."
                        });
                    }

                    return Ok(session);
                }

                var published = await _sessionService.GetPublishedByCodeAsync(
                    codeSession,
                    idSociete is > 0 ? idSociete : null,
                    cancellationToken);

                if (published == null)
                {
                    return NotFound(new
                    {
                        message = $"Aucune session Published avec le code '{codeSession.Trim()}'."
                    });
                }

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
                _logger.LogError(ex, "Erreur GET session événement code {CodeSession}", codeSession);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les sessions dont StartAtUtc tombe sur une date (jour UTC).</summary>
        [HttpGet("date/{date:datetime}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetByDate(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var sessions = await _sessionService.ListByDateAsync(date, idSociete, cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET sessions événement date {Date}", date);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les sessions entre deux dates sur StartAtUtc (inclusif, jour UTC).</summary>
        [HttpGet("daterange")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionListItemDto>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<IEnumerable<EvenementSessionListItemDto>>> GetByDateRange(
            [FromQuery] DateTime dateDebut,
            [FromQuery] DateTime dateFin,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dateFin < dateDebut)
                    return BadRequest(new { message = "dateFin doit être supérieure ou égale à dateDebut." });

                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var sessions = await _sessionService.ListByDateRangeAsync(
                    dateDebut,
                    dateFin,
                    idSociete,
                    cancellationToken);
                return Ok(sessions);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erreur GET sessions événement plage {DateDebut} - {DateFin}",
                    dateDebut,
                    dateFin);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Détail d'une session événement par identifiant.</summary>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementSessionResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementSessionResponseDto>> GetById(int id)
        {
            try
            {
                var session = await ResolveSessionForPublicReadAsync(id);
                if (session == null)
                    return NotFound(new { message = $"Session événement {id} introuvable." });

                return Ok(session);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur GET session événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPost]
        [Permission("Evenement.Session.Write")]
        public async Task<ActionResult<EvenementSessionResponseDto>> Create(
            [FromBody] EvenementCreateSessionRequestDto request)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var created = await _sessionService.CreateDraftAsync(request, idSociete);
                return CreatedAtAction(nameof(GetById), new { id = created.IdEvenementSession }, created);
            }
            catch (EvenementSessionConflictException ex)
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
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur POST session événement");
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpPut("{id:int}/publish")]
        [Permission("Evenement.Session.Write")]
        public async Task<ActionResult<EvenementSessionResponseDto>> Publish(int id)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var published = await _sessionService.PublishAsync(id, idSociete);
                return Ok(published);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur publish session événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        [HttpGet("{id:int}/availability")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(EvenementAvailabilityResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementAvailabilityResponseDto>> GetAvailability(int id)
        {
            try
            {
                var session = await ResolveSessionForPublicReadAsync(id);
                if (session == null)
                    return NotFound(new { message = $"Session événement {id} introuvable." });

                var availability = await _availabilityService.GetSessionAvailabilityAsync(
                    id, session.IdSociete);
                if (availability == null)
                    return NotFound(new { message = $"Session événement {id} introuvable." });

                return Ok(availability);
            }
            catch (NotSupportedException ex)
            {
                return StatusCode(StatusCodes.Status501NotImplemented, new { message = ex.Message });
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
                _logger.LogError(ex, "Erreur GET availability session événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Liste les photos d'une session (max 3).</summary>
        [HttpGet("{id:int}/photos")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<EvenementSessionPhotoDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<EvenementSessionPhotoDto>>> GetPhotos(int id)
        {
            try
            {
                var session = await ResolveSessionForPublicReadAsync(id);
                if (session == null)
                    return NotFound(new { message = $"Session événement {id} introuvable." });

                var photos = await _photoService.GetBySessionIdAsync(id, session.IdSociete);
                return Ok(photos.Select(EvenementSessionMapper.ToPhotoDto).ToList());
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
                _logger.LogError(ex, "Erreur GET photos session événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Ajoute une photo à une session (max 3).</summary>
        [HttpPost("{id:int}/photos")]
        [Permission("Evenement.Session.Write")]
        [ProducesResponseType(typeof(EvenementSessionPhotoDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementSessionPhotoDto>> AddPhoto(
            int id,
            [FromBody] AddEvenementSessionPhotoDto dto)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.AddPhotoAsync(id, idSociete, dto);
                return CreatedAtAction(
                    nameof(GetPhotos),
                    new { id },
                    EvenementSessionMapper.ToPhotoDto(photo));
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
                _logger.LogError(ex, "Erreur POST photo session événement {Id}", id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Met à jour l'ordre d'affichage d'une photo (1..3).</summary>
        [HttpPut("{id:int}/photos/{photoId:int}/ordre")]
        [Permission("Evenement.Session.Write")]
        [ProducesResponseType(typeof(EvenementSessionPhotoDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<EvenementSessionPhotoDto>> UpdatePhotoOrdre(
            int id,
            int photoId,
            [FromBody] UpdateEvenementSessionPhotoOrdreDto dto)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var photo = await _photoService.UpdateOrdreAsync(id, idSociete, photoId, dto.Ordre);
                if (photo == null)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour la session {id}." });

                return Ok(EvenementSessionMapper.ToPhotoDto(photo));
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
                _logger.LogError(ex, "Erreur PUT ordre photo {PhotoId} session {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>Supprime une photo de session.</summary>
        [HttpDelete("{id:int}/photos/{photoId:int}")]
        [Permission("Evenement.Session.Write")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeletePhoto(int id, int photoId)
        {
            try
            {
                var idSociete = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var deleted = await _photoService.DeletePhotoAsync(id, idSociete, photoId);
                if (!deleted)
                    return NotFound(new { message = $"Photo {photoId} introuvable pour la session {id}." });

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
                _logger.LogError(ex, "Erreur DELETE photo {PhotoId} session {Id}", photoId, id);
                return StatusCode(500, new { message = "Une erreur interne est survenue." });
            }
        }

        /// <summary>
        /// Résout une session pour lecture publique / Client (Published) ou staff (tenant).
        /// Super-Admin : tout statut, sans filtre société.
        /// </summary>
        private async Task<EvenementSessionResponseDto?> ResolveSessionForPublicReadAsync(int id)
        {
            if (_currentUserService.IsSuperAdmin)
                return await _sessionService.GetByIdAsync(id);

            var isStaffTenant = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(
                _currentUserService,
                null,
                out var effectiveSocieteId);

            if (isStaffTenant && effectiveSocieteId.HasValue)
                return await _sessionService.GetByIdAsync(id, effectiveSocieteId.Value);

            return await _sessionService.GetPublishedByIdAsync(id);
        }

        private static bool TryParseOptionalStatus(
            string? status,
            out EvenementSessionStatus? parsedStatus,
            out string? errorMessage)
        {
            parsedStatus = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(status))
                return true;

            if (Enum.TryParse<EvenementSessionStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
                return true;
            }

            errorMessage = $"Statut invalide '{status}'. Valeurs acceptées : Draft, Published, Closed, Cancelled.";
            return false;
        }

        private static bool TryParseOptionalInventoryMode(
            string? inventoryMode,
            out EvenementInventoryMode? parsedMode,
            out string? errorMessage)
        {
            parsedMode = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(inventoryMode))
                return true;

            if (Enum.TryParse<EvenementInventoryMode>(inventoryMode, ignoreCase: true, out var value))
            {
                parsedMode = value;
                return true;
            }

            errorMessage = $"InventoryMode invalide '{inventoryMode}'. Valeurs acceptées : GlobalQuota, ClassQuota, SeatNumbered.";
            return false;
        }
    }
}
