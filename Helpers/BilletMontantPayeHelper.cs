using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    /// <summary>Résout le montant payé (ou tarif catalogue) attribuable à un billet pour le calcul de pénalité.</summary>
    public static class BilletMontantPayeHelper
    {
        public static async Task<decimal> ResolveMontantPayeBilletAsync(
            CongoTravelDbContext context,
            IVoyageTarifService voyageTarifService,
            Billet billet,
            Voyage voyage,
            CancellationToken cancellationToken = default)
        {
            var tarifCatalogueBillet = await ResolveTarifCatalogueBilletAsync(
                context, voyageTarifService, billet, voyage, cancellationToken);

            if (!billet.IdReservation.HasValue)
                return tarifCatalogueBillet;

            var idReservation = billet.IdReservation.Value;

            var totalPaye = await context.Paiements
                .AsNoTracking()
                .Where(p => p.IdReservation == idReservation && !p.IsDeleted && p.Statut && p.MontantPaye.HasValue)
                .SumAsync(p => p.MontantPaye!.Value, cancellationToken);

            var passagers = await context.ReservationPassengers
                .AsNoTracking()
                .Where(p => p.IdReservation == idReservation && p.Statut)
                .Select(p => p.IdReservationPassenger)
                .ToListAsync(cancellationToken);

            if (passagers.Count == 0)
                return totalPaye > 0m ? totalPaye : tarifCatalogueBillet;

            if (passagers.Count == 1)
                return totalPaye > 0m ? totalPaye : tarifCatalogueBillet;

            var allocations = await context.VoyageSeatAllocations
                .AsNoTracking()
                .Where(a => a.IdVoyage == voyage.Id && passagers.Contains(a.IdReservationPassenger))
                .Select(a => a.IdSiege)
                .ToListAsync(cancellationToken);

            if (allocations.Count == 0)
                return totalPaye > 0m ? Math.Round(totalPaye / passagers.Count, 2, MidpointRounding.AwayFromZero) : tarifCatalogueBillet;

            var tarifTotal = await voyageTarifService.ComputeTotalForSiegesAsync(
                voyage.Id,
                allocations,
                voyage.Prix,
                cancellationToken);

            if (tarifTotal <= 0m)
                return totalPaye > 0m ? Math.Round(totalPaye / passagers.Count, 2, MidpointRounding.AwayFromZero) : tarifCatalogueBillet;

            if (totalPaye <= 0m)
                return tarifCatalogueBillet;

            return Math.Round(totalPaye * (tarifCatalogueBillet / tarifTotal), 2, MidpointRounding.AwayFromZero);
        }

        private static async Task<decimal> ResolveTarifCatalogueBilletAsync(
            CongoTravelDbContext context,
            IVoyageTarifService voyageTarifService,
            Billet billet,
            Voyage voyage,
            CancellationToken cancellationToken)
        {
            if (!billet.IdSiege.HasValue)
                return voyage.Prix;

            var idCategorieSiege = billet.Siege?.IdCategorieSiege ?? 0;
            if (idCategorieSiege <= 0)
            {
                idCategorieSiege = await context.Sieges
                    .AsNoTracking()
                    .Where(s => s.IdSiege == billet.IdSiege.Value)
                    .Select(s => s.IdCategorieSiege)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (idCategorieSiege <= 0)
                return voyage.Prix;

            return await voyageTarifService.ResolvePrixAsync(
                voyage.Id,
                idCategorieSiege,
                voyage.Prix,
                cancellationToken);
        }
    }
}
