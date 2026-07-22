using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class DeviseMontantConverter : IDeviseMontantConverter
    {
        private readonly CongoTravelDbContext _context;

        public DeviseMontantConverter(CongoTravelDbContext context)
        {
            _context = context;
        }

        public async Task<(decimal MontantCible, decimal Taux)> ConvertAsync(
            int idSociete,
            decimal montant,
            string codeSource,
            string codeCible,
            DateTime dateRef,
            CancellationToken cancellationToken = default)
        {
            codeSource = codeSource.Trim().ToUpperInvariant();
            codeCible = codeCible.Trim().ToUpperInvariant();

            if (codeSource == codeCible)
                return (montant, 1m);

            var tauxDirect = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == codeSource
                            && t.CodeDeviseCible == codeCible
                            && t.Statut
                            && t.DateEffet <= dateRef)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync(cancellationToken);

            if (tauxDirect.HasValue)
                return (Math.Round(montant * tauxDirect.Value, 2, MidpointRounding.AwayFromZero), tauxDirect.Value);

            var tauxInverse = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == codeCible
                            && t.CodeDeviseCible == codeSource
                            && t.Statut
                            && t.DateEffet <= dateRef)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync(cancellationToken);

            if (tauxInverse.HasValue && tauxInverse.Value != 0)
            {
                var inv = 1m / tauxInverse.Value;
                return (Math.Round(montant * inv, 2, MidpointRounding.AwayFromZero), inv);
            }

            throw new InvalidOperationException(
                $"Aucun taux actif pour {codeSource}->{codeCible} à la date {dateRef:yyyy-MM-dd}.");
        }
    }
}
