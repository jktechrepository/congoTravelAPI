using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InfoPaiementSocieteController : ControllerBase
    {
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<InfoPaiementSocieteController> _logger;

        public InfoPaiementSocieteController(
            CongoTravelDbContext context,
            ICurrentUserService currentUser,
            ILogger<InfoPaiementSocieteController> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet("site/{idSite:int}")]
        [ProducesResponseType(typeof(InfoPaiementSocieteResponseDto), 200)]
        public async Task<ActionResult<InfoPaiementSocieteResponseDto>> GetBySite(int idSite)
        {
            var denied = RequireSuperAdmin();
            if (denied != null)
                return denied;
            var info = await _context.InfoPaiementsSociete.AsNoTracking()
                .FirstOrDefaultAsync(i => i.IdSite == idSite);
            if (info == null)
                return NotFound(new { message = $"Configuration FlexPay introuvable pour le site {idSite}." });
            return Ok(Map(info));
        }

        [HttpPost]
        [ProducesResponseType(typeof(InfoPaiementSocieteResponseDto), 201)]
        public async Task<ActionResult<InfoPaiementSocieteResponseDto>> Create([FromBody] InfoPaiementSocieteCreateDto dto)
        {
            var denied = RequireSuperAdmin();
            if (denied != null)
                return denied;
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var siteOk = await _context.Sites.AnyAsync(s => s.IdSite == dto.IdSite && s.IdSociete == dto.IdSociete);
            if (!siteOk)
                return BadRequest(new { message = "Le site n'appartient pas à la société indiquée." });

            var exists = await _context.InfoPaiementsSociete.AnyAsync(i => i.IdSite == dto.IdSite);
            if (exists)
                return Conflict(new { message = "Une configuration FlexPay existe déjà pour ce site." });

            var entity = new InfoPaiementSociete
            {
                IdSociete = dto.IdSociete,
                IdSite = dto.IdSite,
                CodeMarchand = dto.CodeMarchand.Trim(),
                ApiToken = dto.ApiToken.Trim(),
                ActifMobileMoney = dto.ActifMobileMoney,
                ActifCarteBancaire = dto.ActifCarteBancaire,
                Statut = dto.Statut,
                DateCreation = DateTime.UtcNow
            };

            _context.InfoPaiementsSociete.Add(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("InfoPaiementSociete créé pour site {IdSite}", dto.IdSite);
            return CreatedAtAction(nameof(GetBySite), new { idSite = entity.IdSite }, Map(entity));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(InfoPaiementSocieteResponseDto), 200)]
        public async Task<ActionResult<InfoPaiementSocieteResponseDto>> Update(int id, [FromBody] InfoPaiementSocieteUpdateDto dto)
        {
            var denied = RequireSuperAdmin();
            if (denied != null)
                return denied;
            var entity = await _context.InfoPaiementsSociete.FindAsync(id);
            if (entity == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.CodeMarchand))
                entity.CodeMarchand = dto.CodeMarchand.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ApiToken))
                entity.ApiToken = dto.ApiToken.Trim();
            if (dto.ActifMobileMoney.HasValue)
                entity.ActifMobileMoney = dto.ActifMobileMoney.Value;
            if (dto.ActifCarteBancaire.HasValue)
                entity.ActifCarteBancaire = dto.ActifCarteBancaire.Value;
            if (dto.Statut.HasValue)
                entity.Statut = dto.Statut.Value;

            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(Map(entity));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var denied = RequireSuperAdmin();
            if (denied != null)
                return denied;
            var entity = await _context.InfoPaiementsSociete.FindAsync(id);
            if (entity == null)
                return NotFound();
            _context.InfoPaiementsSociete.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private ActionResult? RequireSuperAdmin()
        {
            if (!_currentUser.IsSuperAdmin)
                return Forbid();
            return null;
        }

        private static InfoPaiementSocieteResponseDto Map(InfoPaiementSociete e) => new()
        {
            IdInfoPaiementSociete = e.IdInfoPaiementSociete,
            IdSociete = e.IdSociete,
            IdSite = e.IdSite,
            CodeMarchand = e.CodeMarchand,
            ApiTokenMasked = FlexPayTokenMaskHelper.Mask(e.ApiToken),
            ActifMobileMoney = e.ActifMobileMoney,
            ActifCarteBancaire = e.ActifCarteBancaire,
            Statut = e.Statut,
            DateCreation = e.DateCreation,
            DateModification = e.DateModification
        };
    }
}
