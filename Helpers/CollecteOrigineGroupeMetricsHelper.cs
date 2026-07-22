using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    public static class CollecteOrigineGroupeMetricsHelper
    {
        public static Task<(List<CollecteOrigineGroupeItemDto> Items, CollecteOrigineGroupeSyntheseDto Synthese)>
            GetCollecteParOrigineGroupeAsync(
                CongoTravelDbContext context,
                int societeId,
                DateTime monthStart,
                DateTime nextMonthStart,
                DateTime previousMonthStart,
                int? idSite = null,
                CancellationToken cancellationToken = default) =>
            GetCollecteParOrigineGroupeAsync(
                context,
                new[] { societeId },
                monthStart,
                nextMonthStart,
                previousMonthStart,
                idSite,
                cancellationToken);

        public static async Task<(List<CollecteOrigineGroupeItemDto> Items, CollecteOrigineGroupeSyntheseDto Synthese)>
            GetCollecteParOrigineGroupeAsync(
                CongoTravelDbContext context,
                IReadOnlyList<int>? societeIds,
                DateTime monthStart,
                DateTime nextMonthStart,
                DateTime previousMonthStart,
                int? idSite = null,
                CancellationToken cancellationToken = default)
        {
            var baseQuery = context.Paiements.AsNoTracking()
                .Where(p => !p.IsDeleted && p.Statut);

            if (societeIds is { Count: > 0 })
                baseQuery = baseQuery.Where(p => societeIds.Contains(p.IdSociete));

            if (idSite.HasValue)
                baseQuery = baseQuery.Where(p => p.IdSite == idSite.Value);

            var currentGroups = await GroupByOrigineGroupeAsync(
                baseQuery.Where(p => p.DatePaiement >= monthStart && p.DatePaiement < nextMonthStart),
                cancellationToken);

            var previousGroups = await GroupByOrigineGroupeAsync(
                baseQuery.Where(p => p.DatePaiement >= previousMonthStart && p.DatePaiement < monthStart),
                cancellationToken);

            return BuildResult(currentGroups, previousGroups);
        }

        private static async Task<Dictionary<string, (decimal Montant, int Count)>> GroupByOrigineGroupeAsync(
            IQueryable<Models.Paiement> query,
            CancellationToken cancellationToken)
        {
            var rows = await query
                .GroupBy(p =>
                    p.Origine == OrigineOperation.CLIENT ? OrigineOperationGroupe.CLIENT
                    : p.Origine == OrigineOperation.INCONNU || p.Origine == null || p.Origine == ""
                        ? OrigineOperationGroupe.INCONNU
                        : OrigineOperationGroupe.AGENT)
                .Select(g => new
                {
                    Groupe = g.Key,
                    Montant = g.Sum(x => (decimal?)(x.MontantPayeDevisePrincipale ?? x.MontantPaye) ?? 0m),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            return rows.ToDictionary(x => x.Groupe, x => (x.Montant, x.Count));
        }

        public static (List<CollecteOrigineGroupeItemDto> Items, CollecteOrigineGroupeSyntheseDto Synthese) BuildResult(
            Dictionary<string, (decimal Montant, int Count)> currentGroups,
            Dictionary<string, (decimal Montant, int Count)> previousGroups)
        {
            var totalMois = currentGroups.Values.Sum(x => x.Montant);
            var items = new List<CollecteOrigineGroupeItemDto>();

            foreach (var groupe in OrigineOperationGroupe.All)
            {
                currentGroups.TryGetValue(groupe, out var current);
                previousGroups.TryGetValue(groupe, out var previous);

                var montant = current.Montant;
                var montantPrecedent = previous.Montant;

                items.Add(new CollecteOrigineGroupeItemDto
                {
                    OrigineGroupe = groupe,
                    Montant = montant,
                    NombrePaiements = current.Count,
                    MontantMoisPrecedent = montantPrecedent,
                    VariationPourcentage = SocieteTransportMetricsHelper.ComputeVariationPercent(montant, montantPrecedent),
                    PartPourcentage = totalMois > 0m
                        ? Math.Round(montant / totalMois * 100m, 2)
                        : 0m
                });
            }

            currentGroups.TryGetValue(OrigineOperationGroupe.CLIENT, out var client);
            currentGroups.TryGetValue(OrigineOperationGroupe.AGENT, out var agent);
            currentGroups.TryGetValue(OrigineOperationGroupe.INCONNU, out var inconnu);

            var montantClassifie = client.Montant + agent.Montant;
            var synthese = new CollecteOrigineGroupeSyntheseDto
            {
                MontantClassifie = montantClassifie,
                MontantNonClassifie = inconnu.Montant,
                PartDigitalPourcentage = montantClassifie > 0m
                    ? Math.Round(client.Montant / montantClassifie * 100m, 2)
                    : 0m,
                PartGuichetPourcentage = montantClassifie > 0m
                    ? Math.Round(agent.Montant / montantClassifie * 100m, 2)
                    : 0m
            };

            return (items, synthese);
        }
    }
}
