using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Remboursement;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemboursementController : ControllerBase
    {
        private readonly CongoTravelDbContext _context;

        public RemboursementController(CongoTravelDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [Permission("Remboursement.Create")]
        public async Task<IActionResult> Create([FromBody] CreateRemboursementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var paiement = await _context.Paiements.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdPaiement == dto.IdPaiement && !p.IsDeleted);
            if (paiement == null)
                return NotFound(new { message = $"Paiement {dto.IdPaiement} introuvable." });
            if (paiement.IdSociete != dto.IdSociete)
                return BadRequest(new { message = "Le paiement n'appartient pas à la société fournie." });

            var dejaRembourse = await _context.Remboursements.AsNoTracking()
                .Where(r => r.IdPaiement == dto.IdPaiement && r.Statut)
                .SumAsync(r => (decimal?)r.MontantRembourseDevisePrincipale) ?? 0m;

            var montantMaxRemboursable = paiement.MontantPayeDevisePrincipale ?? 0m;
            if (montantMaxRemboursable <= 0)
                return BadRequest(new { message = "Ce paiement ne possède pas de montant payé remboursable." });

            var societe = await _context.Societes.AsNoTracking().FirstOrDefaultAsync(s => s.IdSociete == dto.IdSociete);
            if (societe == null)
                return NotFound(new { message = $"Société {dto.IdSociete} introuvable." });
            var devisePrincipale = string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale) ? "CDF" : societe.CodeDevisePrincipale.Trim().ToUpperInvariant();

            var deviseRemboursement = dto.ForcerDevisePrincipale
                ? devisePrincipale
                : (dto.CodeDeviseRemboursement?.Trim().ToUpperInvariant() ?? paiement.CodeDevisePaiement);

            var taux = await ResolveRateAsync(dto.IdSociete, deviseRemboursement, devisePrincipale, dto.DateRemboursement ?? DateTime.UtcNow);
            if (!taux.Success)
                return BadRequest(new { message = taux.ErrorMessage });

            var montantDevisePrincipale = Math.Round(dto.MontantRembourse * taux.Value, 2, MidpointRounding.AwayFromZero);
            if (dejaRembourse + montantDevisePrincipale > montantMaxRemboursable)
            {
                return BadRequest(new
                {
                    message = "Le montant dépasse le total remboursable.",
                    montantMaxRemboursable,
                    dejaRembourse
                });
            }

            var entity = new Remboursement
            {
                IdPaiement = dto.IdPaiement,
                IdSociete = dto.IdSociete,
                IdUtilisateur = dto.IdUtilisateur,
                CodeDeviseRemboursement = deviseRemboursement,
                CodeDevisePrincipale = devisePrincipale,
                MontantRembourse = dto.MontantRembourse,
                TauxVersDevisePrincipale = taux.Value,
                MontantRembourseDevisePrincipale = montantDevisePrincipale,
                DateRemboursement = dto.DateRemboursement ?? DateTime.UtcNow,
                Motif = dto.Motif,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };

            _context.Remboursements.Add(entity);
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        private async Task<(bool Success, string? ErrorMessage, decimal Value)> ResolveRateAsync(
            int idSociete,
            string codeSource,
            string codeCible,
            DateTime dateReference)
        {
            if (codeSource == codeCible)
                return (true, null, 1m);

            var rate = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == codeSource
                            && t.CodeDeviseCible == codeCible
                            && t.Statut
                            && t.DateEffet <= dateReference)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync();
            if (!rate.HasValue)
                return (false, $"Aucun taux actif {codeSource}->{codeCible} trouvé à la date {dateReference:yyyy-MM-dd}.", 0m);

            return (true, null, rate.Value);
        }
    }
}

