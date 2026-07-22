using CongoTravel.Models;
using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    public static class OrigineOperationGroupeHelper
    {
        public static string ToGroupe(string? origine)
        {
            if (string.IsNullOrWhiteSpace(origine) || origine == OrigineOperation.INCONNU)
                return OrigineOperationGroupe.INCONNU;

            if (origine == OrigineOperation.CLIENT)
                return OrigineOperationGroupe.CLIENT;

            return OrigineOperationGroupe.AGENT;
        }

        public static IQueryable<Paiement> ApplyOrigineGroupeFilter(
            IQueryable<Paiement> query,
            string? origineGroupe)
        {
            if (string.IsNullOrWhiteSpace(origineGroupe))
                return query;

            var g = origineGroupe.Trim().ToUpperInvariant();

            if (g == OrigineOperationGroupe.CLIENT)
            {
                return query.Where(p => p.Origine == OrigineOperation.CLIENT);
            }

            if (g == OrigineOperationGroupe.AGENT)
            {
                return query.Where(p =>
                    p.Origine != OrigineOperation.CLIENT
                    && p.Origine != OrigineOperation.INCONNU
                    && p.Origine != null
                    && p.Origine != "");
            }

            if (g == OrigineOperationGroupe.INCONNU)
            {
                return query.Where(p =>
                    p.Origine == OrigineOperation.INCONNU
                    || p.Origine == null
                    || p.Origine == "");
            }

            return query;
        }
    }
}
