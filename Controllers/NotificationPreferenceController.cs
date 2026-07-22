using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationPreferenceController : ControllerBase
    {
        private readonly INotificationPreferenceRepository _preferenceRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public NotificationPreferenceController(
            INotificationPreferenceRepository preferenceRepository,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _preferenceRepository = preferenceRepository;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        // GET: api/NotificationPreference/mes-preferences
        [HttpGet("mes-preferences")]
        [Permission("NotificationPreference.Read")]
        public async Task<ActionResult<NotificationPreferenceDto>> GetMesPreferences()
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var preference = await _preferenceRepository.GetByUtilisateurAsync(userId);
            
            if (preference == null)
            {
                // Retourner les valeurs par défaut
                return Ok(new NotificationPreferenceDto());
            }

            return Ok(new NotificationPreferenceDto
            {
                AllowPush = preference.AllowPush,
                AllowInApp = preference.AllowInApp,
                AllowSms = preference.AllowSms,
                AllowEmail = preference.AllowEmail,
                OptOutGlobal = preference.OptOutGlobal,
                OptOutFactures = preference.OptOutFactures
            });
        }

        // PUT: api/NotificationPreference/mes-preferences
        [HttpPut("mes-preferences")]
        [Permission("NotificationPreference.Update")]
        public async Task<ActionResult<NotificationPreferenceDto>> UpdateMesPreferences([FromBody] UpdateNotificationPreferenceDto dto)
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var existing = await _preferenceRepository.GetByUtilisateurAsync(userId);
            
            var preference = existing ?? new NotificationPreference
            {
                IdUtilisateur = userId
            };

            // Mettre à jour uniquement les champs fournis
            if (dto.AllowPush.HasValue) preference.AllowPush = dto.AllowPush.Value;
            if (dto.AllowInApp.HasValue) preference.AllowInApp = dto.AllowInApp.Value;
            if (dto.AllowSms.HasValue) preference.AllowSms = dto.AllowSms.Value;
            if (dto.AllowEmail.HasValue) preference.AllowEmail = dto.AllowEmail.Value;
            if (dto.OptOutGlobal.HasValue) preference.OptOutGlobal = dto.OptOutGlobal.Value;
            if (dto.OptOutFactures.HasValue) preference.OptOutFactures = dto.OptOutFactures.Value;

            var updated = await _preferenceRepository.CreateOrUpdateAsync(preference);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                existing, 
                updated, 
                ctx.UserId, 
                ctx.UserName, 
                ctx.UserRole, 
                ctx.IdSociete, 
                ctx.IpAddress, 
                ctx.UserAgent, 
                "Mise à jour des préférences de notification");

            return Ok(new NotificationPreferenceDto
            {
                AllowPush = updated.AllowPush,
                AllowInApp = updated.AllowInApp,
                AllowSms = updated.AllowSms,
                AllowEmail = updated.AllowEmail,
                OptOutGlobal = updated.OptOutGlobal,
                OptOutFactures = updated.OptOutFactures
            });
        }

        // DELETE: api/NotificationPreference/mes-preferences
        [HttpDelete("mes-preferences")]
        [Permission("NotificationPreference.Delete")]
        public async Task<IActionResult> DeleteMesPreferences()
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
            {
                return Unauthorized();
            }

            var deleted = await _preferenceRepository.DeleteAsync(userId);
            if (!deleted)
            {
                return NotFound();
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(
                new NotificationPreference { IdUtilisateur = userId }, 
                ctx.UserId, 
                ctx.UserName, 
                ctx.UserRole, 
                ctx.IdSociete, 
                ctx.IpAddress, 
                ctx.UserAgent, 
                "Suppression des préférences de notification");

            return NoContent();
        }
    }
}

