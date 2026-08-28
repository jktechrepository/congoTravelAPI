using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Devise;
using CongoTravel.Models.DTOs.TauxChange;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/Devise")]
    [Authorize]
    public class DeviseController : ControllerBase
    {
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public DeviseController(
            CongoTravelDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet("devises")]
        public async Task<IActionResult> GetDevisesActives()
        {
            var idSociete = _currentUserService.SocieteId;
            var devises = await _context.DevisesMonetaires.AsNoTracking()
                .Where(d => d.Statut)
                .Where(d => _currentUserService.IsSuperAdmin || d.IdSociete == idSociete)
                .OrderBy(d => d.CodeDevise)
                .Select(d => new
                {
                    d.IdDeviseMonetaire,
                    d.IdSociete,
                    d.CodeDevise,
                    d.Libelle,
                    d.Symbole,
                    estDevisePrincipale = d.Societe != null && d.Societe.CodeDevisePrincipale == d.CodeDevise
                })
                .ToListAsync();

            return Ok(devises);
        }

        [HttpGet("devises/societe/{idSociete:int}")]
        public async Task<IActionResult> GetDevisesBySociete(
            int idSociete,
            [FromQuery] bool includeInactive = false)
        {
            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != idSociete)
                return Forbid();

            var societeExists = await _context.Societes
                .AsNoTracking()
                .AnyAsync(s => s.IdSociete == idSociete);
            if (!societeExists)
                return NotFound(new { message = $"Société {idSociete} introuvable." });

            var devises = await _context.DevisesMonetaires
                .AsNoTracking()
                .Where(d => d.IdSociete == idSociete)
                .Where(d => includeInactive || d.Statut)
                .OrderBy(d => d.CodeDevise)
                .Select(d => new
                {
                    d.IdDeviseMonetaire,
                    d.IdSociete,
                    d.CodeDevise,
                    d.Libelle,
                    d.Symbole,
                    estDevisePrincipale = d.Societe != null && d.Societe.CodeDevisePrincipale == d.CodeDevise
                })
                .ToListAsync();

            return Ok(devises);
        }

        [HttpPost("devises")]
        public async Task<IActionResult> CreateDevise([FromBody] CreateDeviseMonetaireDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != dto.IdSociete)
                return Forbid();

            var societeExists = await _context.Societes
                .AsNoTracking()
                .AnyAsync(s => s.IdSociete == dto.IdSociete);
            if (!societeExists)
                return NotFound(new { message = $"Société {dto.IdSociete} introuvable." });

            if (dto.EstDevisePrincipale && !dto.Statut)
                return BadRequest(new { message = "Une devise principale doit être active." });

            var code = dto.CodeDevise.Trim().ToUpperInvariant();
            if (code.Length != 3)
                return BadRequest(new { message = "Le code devise doit contenir exactement 3 caractères." });

            var libelle = dto.Libelle.Trim();
            if (string.IsNullOrWhiteSpace(libelle))
                return BadRequest(new { message = "Le libellé de la devise est obligatoire." });

            var symbole = string.IsNullOrWhiteSpace(dto.Symbole) ? null : dto.Symbole.Trim();

            var existe = await _context.DevisesMonetaires
                .AsNoTracking()
                .AnyAsync(d => d.IdSociete == dto.IdSociete && d.CodeDevise == code);
            if (existe)
                return Conflict(new { message = $"La devise '{code}' existe déjà pour la société {dto.IdSociete}." });

            var devise = new DeviseMonetaire
            {
                IdSociete = dto.IdSociete,
                CodeDevise = code,
                Libelle = libelle,
                Symbole = symbole,
                Statut = dto.Statut,
                DateCreation = DateTime.UtcNow
            };

            _context.DevisesMonetaires.Add(devise);

            if (dto.EstDevisePrincipale)
            {
                var societe = await _context.Societes
                    .FirstOrDefaultAsync(s => s.IdSociete == dto.IdSociete);

                if (societe == null)
                    return NotFound(new { message = $"Société {dto.IdSociete} introuvable." });

                societe.CodeDevisePrincipale = code;
            }

            await _context.SaveChangesAsync();

            return Created($"/api/Devise/devises/{devise.IdDeviseMonetaire}", new
            {
                devise.IdDeviseMonetaire,
                devise.IdSociete,
                devise.CodeDevise,
                devise.Libelle,
                devise.Symbole,
                devise.Statut,
                estDevisePrincipale = dto.EstDevisePrincipale,
                devise.DateCreation
            });
        }

        [HttpGet("devises/{idDeviseMonetaire:int}")]
        public async Task<IActionResult> GetDeviseById(int idDeviseMonetaire)
        {
            var devise = await _context.DevisesMonetaires
                .AsNoTracking()
                .Include(d => d.Societe)
                .FirstOrDefaultAsync(d => d.IdDeviseMonetaire == idDeviseMonetaire);

            if (devise == null)
                return NotFound(new { message = $"Devise {idDeviseMonetaire} introuvable." });

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != devise.IdSociete)
                return Forbid();

            return Ok(new
            {
                devise.IdDeviseMonetaire,
                devise.IdSociete,
                devise.CodeDevise,
                devise.Libelle,
                devise.Symbole,
                devise.Statut,
                estDevisePrincipale = devise.Societe != null && devise.Societe.CodeDevisePrincipale == devise.CodeDevise,
                devise.DateCreation,
                devise.DateModification
            });
        }

        [HttpPut("devises/{idDeviseMonetaire:int}")]
        public async Task<IActionResult> UpdateDevise(
            int idDeviseMonetaire,
            [FromBody] UpdateDeviseMonetaireDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var devise = await _context.DevisesMonetaires
                .FirstOrDefaultAsync(d => d.IdDeviseMonetaire == idDeviseMonetaire);

            if (devise == null)
                return NotFound(new { message = $"Devise {idDeviseMonetaire} introuvable." });

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != devise.IdSociete)
                return Forbid();

            var societe = await _context.Societes
                .FirstOrDefaultAsync(s => s.IdSociete == devise.IdSociete);
            if (societe == null)
                return NotFound(new { message = $"Société {devise.IdSociete} introuvable." });

            var estPrincipaleActuelle = societe.CodeDevisePrincipale == devise.CodeDevise;

            var libelle = dto.Libelle.Trim();
            if (string.IsNullOrWhiteSpace(libelle))
                return BadRequest(new { message = "Le libellé de la devise est obligatoire." });

            if (dto.EstDevisePrincipale && !dto.Statut)
                return BadRequest(new { message = "Une devise principale doit être active." });

            if (!dto.Statut && estPrincipaleActuelle)
                return BadRequest(new
                {
                    message = "Impossible de désactiver la devise principale actuelle. Définissez d'abord une autre devise principale."
                });

            devise.Libelle = libelle;
            devise.Symbole = string.IsNullOrWhiteSpace(dto.Symbole) ? null : dto.Symbole.Trim();
            devise.Statut = dto.Statut;
            devise.DateModification = DateTime.UtcNow;

            if (dto.EstDevisePrincipale)
            {
                societe.CodeDevisePrincipale = devise.CodeDevise;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Devise mise à jour.",
                devise.IdDeviseMonetaire,
                devise.IdSociete,
                devise.CodeDevise,
                devise.Libelle,
                devise.Symbole,
                devise.Statut,
                estDevisePrincipale = societe.CodeDevisePrincipale == devise.CodeDevise,
                devise.DateCreation,
                devise.DateModification
            });
        }

        [HttpPut("societe/{idSociete:int}/devise-principale/{codeDevise}")]
        public async Task<IActionResult> SetDevisePrincipale(int idSociete, string codeDevise)
        {
            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != idSociete)
                return Forbid();

            var code = codeDevise.Trim().ToUpperInvariant();
            var societe = await _context.Societes.FirstOrDefaultAsync(s => s.IdSociete == idSociete);
            if (societe == null)
                return NotFound(new { message = $"Société {idSociete} introuvable." });

            var deviseExiste = await _context.DevisesMonetaires
                .AsNoTracking()
                .AnyAsync(d => d.CodeDevise == code && d.Statut);
            if (!deviseExiste)
                return BadRequest(new { message = $"La devise '{code}' n'est pas autorisée." });

            societe.CodeDevisePrincipale = code;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Devise principale mise à jour.", idSociete, codeDevisePrincipale = code });
        }

        [HttpPost("taux-change")]
        public async Task<IActionResult> UpsertTaux([FromBody] UpsertTauxChangeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

          //  if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != dto.IdSociete)
           //     return Forbid();

            var source = dto.CodeDeviseSource.Trim().ToUpperInvariant();
            var cible = dto.CodeDeviseCible.Trim().ToUpperInvariant();
            if (source == cible)
                return BadRequest(new { message = "La devise source et cible doivent être différentes." });

            var dateEffet = dto.DateEffet ?? DateTime.UtcNow;
            var societeExists = await _context.Societes.AsNoTracking().AnyAsync(s => s.IdSociete == dto.IdSociete);
            if (!societeExists)
                return NotFound(new { message = $"Société {dto.IdSociete} introuvable." });

            var devises = await _context.DevisesMonetaires.AsNoTracking()
                .Where(d => (d.CodeDevise == source || d.CodeDevise == cible) && d.Statut)
                .Select(d => d.CodeDevise)
                .ToListAsync();
            if (!devises.Contains(source) || !devises.Contains(cible))
                return BadRequest(new { message = "La devise source ou cible est invalide/inactive." });

            var taux = new TauxChange
            {
                IdSociete = dto.IdSociete,
                CodeDeviseSource = source,
                CodeDeviseCible = cible,
                Taux = dto.Taux,
                DateEffet = dateEffet,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };

            _context.TauxChanges.Add(taux);
            await _context.SaveChangesAsync();
            return Ok(taux);
        }

        [HttpGet("taux-change")]
        public async Task<IActionResult> GetTaux([FromQuery] int idSociete, [FromQuery] string source, [FromQuery] string cible)
        {
            if (!DeviseTenancyGuard.CanReadDeviseDataForSociete(_currentUserService, idSociete))
                return Forbid();

            var src = source.Trim().ToUpperInvariant();
            var dst = cible.Trim().ToUpperInvariant();
            var taux = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete && t.CodeDeviseSource == src && t.CodeDeviseCible == dst && t.Statut)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .FirstOrDefaultAsync();

            if (taux == null)
                return NotFound(new { message = $"Aucun taux {src}->{dst} trouvé pour la société {idSociete}." });

            return Ok(taux);
        }

        [HttpGet("preview-conversion")]
        public async Task<IActionResult> PreviewConversion(
            [FromQuery] int idSociete,
            [FromQuery] string codeDeviseSource,
            [FromQuery] decimal montant,
            [FromQuery] DateTime? datePaiement)
        {
            if (!DeviseTenancyGuard.CanReadDeviseDataForSociete(_currentUserService, idSociete))
                return Forbid();

            if (montant < 0)
                return BadRequest(new { message = "Le montant doit être supérieur ou égal à 0." });

            var source = codeDeviseSource.Trim().ToUpperInvariant();
            var dateRef = datePaiement ?? DateTime.UtcNow;

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete);
            if (societe == null)
                return NotFound(new { message = $"Société {idSociete} introuvable." });

            var codeDevisePrincipale = string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale)
                ? "CDF"
                : societe.CodeDevisePrincipale.Trim().ToUpperInvariant();

            var deviseSourceExiste = await _context.DevisesMonetaires.AsNoTracking()
                .AnyAsync(d => d.CodeDevise == source && d.Statut);
            if (!deviseSourceExiste)
                return BadRequest(new { message = $"La devise source '{source}' n'est pas active." });

            if (source == codeDevisePrincipale)
            {
                return Ok(new
                {
                    idSociete,
                    codeDeviseSource = source,
                    codeDevisePrincipale,
                    datePaiement = dateRef,
                    taux = 1m,
                    montantSource = montant,
                    montantConverti = Math.Round(montant, 2, MidpointRounding.AwayFromZero)
                });
            }

            var taux = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == source
                            && t.CodeDeviseCible == codeDevisePrincipale
                            && t.Statut
                            && t.DateEffet <= dateRef)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync();

            if (!taux.HasValue)
            {
                return NotFound(new
                {
                    message = $"Aucun taux actif trouvé pour {source}->{codeDevisePrincipale} à la date {dateRef:yyyy-MM-dd}."
                });
            }

            var montantConverti = Math.Round(montant * taux.Value, 2, MidpointRounding.AwayFromZero);
            return Ok(new
            {
                idSociete,
                codeDeviseSource = source,
                codeDevisePrincipale,
                datePaiement = dateRef,
                taux = taux.Value,
                montantSource = montant,
                montantConverti
            });
        }
    }
}
