using CongoTravel.Models;

namespace CongoTravel.Models.DTOs.Mapping
{
    /// <summary>Prix affiché sur <see cref="BilletResponseDto"/>.</summary>
    public static class BilletResponseDtoPricing
    {
        /// <summary>
        /// Utilise le tarif <see cref="VoyageTarifCategorieSiege"/> pour la catégorie du siège attribué au billet
        /// si une ligne existe ; sinon <see cref="Voyage.Prix"/>.
        /// </summary>
        public static int? ResolvePrixVoyage(Billet src)
        {
            var voyage = src.Reservation?.Voyage;
            if (voyage == null)
                return null;

            var siege = src.Siege;
            if (siege != null && siege.IdCategorieSiege > 0)
            {
                var tarifs = voyage.VoyageTarifsCategorieSiege;
                if (tarifs is { Count: > 0 })
                {
                    foreach (var t in tarifs)
                    {
                        if (t.IdCategorieSiege == siege.IdCategorieSiege)
                            return t.Prix;
                    }
                }
            }

            return voyage.Prix;
        }
    }
}
