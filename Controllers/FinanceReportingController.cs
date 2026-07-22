using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Models.Enums;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FinanceReportingController : ControllerBase
    {
        private readonly CongoTravelDbContext _context;

        public FinanceReportingController(CongoTravelDbContext context)
        {
            _context = context;
        }

        [HttpGet("paiements/summary")]
        [Permission("FinanceReporting.ReadAll")]
        public async Task<IActionResult> GetPaiementsSummary(
            [FromQuery] int idSociete,
            [FromQuery] DateTime? dateDebut,
            [FromQuery] DateTime? dateFin)
        {
            var from = dateDebut ?? DateTime.UtcNow.Date;
            var to = (dateFin ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);

            var baseQuery = _context.Paiements.AsNoTracking()
                .Where(p => !p.IsDeleted
                            && p.Statut
                            && p.IdSociete == idSociete
                            && p.DatePaiement >= from
                            && p.DatePaiement <= to);

            var totalPayePrincipal = await baseQuery.SumAsync(p => p.MontantPayeDevisePrincipale ?? 0m);
            var totalRestePrincipal = await baseQuery.SumAsync(p => p.ResteAPayeDevisePrincipale ?? 0m);
            var totalTransactions = await baseQuery.CountAsync();

            var byDevise = await baseQuery
                .GroupBy(p => p.CodeDevisePaiement)
                .Select(g => new
                {
                    codeDevisePaiement = g.Key,
                    totalMontantPaye = g.Sum(x => x.MontantPaye ?? 0m),
                    totalMontantPayeDevisePrincipale = g.Sum(x => x.MontantPayeDevisePrincipale ?? 0m),
                    count = g.Count()
                })
                .OrderBy(x => x.codeDevisePaiement)
                .ToListAsync();

            var byOrigineGroupe = await baseQuery
                .GroupBy(p =>
                    p.Origine == OrigineOperation.CLIENT ? OrigineOperationGroupe.CLIENT
                    : p.Origine == OrigineOperation.INCONNU || p.Origine == null || p.Origine == ""
                        ? OrigineOperationGroupe.INCONNU
                        : OrigineOperationGroupe.AGENT)
                .Select(g => new
                {
                    origineGroupe = g.Key,
                    totalMontantPayeDevisePrincipale = g.Sum(x => x.MontantPayeDevisePrincipale ?? 0m),
                    count = g.Count()
                })
                .OrderBy(x => x.origineGroupe)
                .ToListAsync();

            return Ok(new
            {
                idSociete,
                dateDebut = from,
                dateFin = to,
                totalTransactions,
                totalPayeDevisePrincipale = totalPayePrincipal,
                totalResteDevisePrincipale = totalRestePrincipal,
                byDevise,
                byOrigineGroupe
            });
        }

        [HttpGet("rapport-caisse")]
        [Permission("FinanceReporting.ReadAll")]
        public async Task<IActionResult> GetRapportCaisse(
            [FromQuery] int idSociete,
            [FromQuery] int? idUtilisateur,
            [FromQuery] DateTime? datePrecise,
            [FromQuery] DateTime? dateDebut,
            [FromQuery] DateTime? dateFin)
        {
            var (fromUtc, toUtc, modePeriode, isValid, errorMessage) =
                RapportCaisseMetricsHelper.ResolvePeriode(datePrecise, dateDebut, dateFin);

            if (!isValid)
            {
                return BadRequest(new { message = errorMessage });
            }

            var paiements = await _context.Paiements.AsNoTracking()
                .Where(p => !p.IsDeleted
                            && p.Statut
                            && p.IdSociete == idSociete
                            && p.DatePaiement >= fromUtc
                            && p.DatePaiement <= toUtc
                            && (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))
                .ToListAsync();

            var codeDevisePrincipale = paiements
                .Select(p => p.CodeDevisePrincipale)
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "CDF";

            var rapport = RapportCaisseMetricsHelper.BuildRapportCaisse(
                paiements,
                idSociete,
                idUtilisateur,
                fromUtc,
                toUtc,
                modePeriode,
                codeDevisePrincipale);

            return Ok(rapport);
        }
    }
}

