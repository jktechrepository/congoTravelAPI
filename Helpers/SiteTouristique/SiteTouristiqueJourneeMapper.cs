using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueJourneeMapper
    {
        public static SiteTouristiqueJourneeListItemDto ToListItemDto(SiteTouristiqueJournee journee)
        {
            var dto = new SiteTouristiqueJourneeListItemDto
            {
                IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                IdSociete = journee.IdSociete,
                NomSociete = journee.Societe?.Nom,
                IdSiteTouristique = journee.IdSiteTouristique,
                CodeLieu = journee.Lieu?.CodeLieu,
                NomLieu = journee.Lieu?.Nom,
                IdSite = journee.Lieu?.IdSite,
                NomSite = journee.Lieu?.Site?.NomSite,
                DateVisite = journee.DateVisite,
                InventoryMode = journee.InventoryMode.ToString(),
                Status = journee.Status.ToString(),
                CodeDevise = journee.CodeDevise,
                SalesOpenAtUtc = journee.SalesOpenAtUtc,
                SalesCloseAtUtc = journee.SalesCloseAtUtc,
                DateCreation = journee.DateCreation,
                DateModification = journee.DateModification
            };

            ApplyPriceSummary(journee, out var prixMin, out var prixMax);
            dto.PrixMin = prixMin;
            dto.PrixMax = prixMax;
            return dto;
        }

        public static SiteTouristiqueJourneeResponseDto ToResponseDto(SiteTouristiqueJournee journee)
        {
            var dto = new SiteTouristiqueJourneeResponseDto
            {
                IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                IdSociete = journee.IdSociete,
                NomSociete = journee.Societe?.Nom,
                IdSiteTouristique = journee.IdSiteTouristique,
                CodeLieu = journee.Lieu?.CodeLieu,
                NomLieu = journee.Lieu?.Nom,
                IdSite = journee.Lieu?.IdSite,
                NomSite = journee.Lieu?.Site?.NomSite,
                DateVisite = journee.DateVisite,
                InventoryMode = journee.InventoryMode.ToString(),
                Status = journee.Status.ToString(),
                CodeDevise = journee.CodeDevise,
                SalesOpenAtUtc = journee.SalesOpenAtUtc,
                SalesCloseAtUtc = journee.SalesCloseAtUtc,
                DateCreation = journee.DateCreation,
                DateModification = journee.DateModification
            };

            ApplyPriceSummary(journee, out var prixMin, out var prixMax);
            dto.PrixMin = prixMin;
            dto.PrixMax = prixMax;
            ApplyAvailabilitySummary(journee, dto);

            if (journee.GlobalQuota != null)
                dto.GlobalQuota = ToGlobalQuotaAvailability(journee.GlobalQuota, journee.CodeDevise);

            if (journee.ClassQuotas.Count > 0)
            {
                dto.ClassQuotas = journee.ClassQuotas
                    .OrderBy(q => q.IdSiteTouristiqueClassQuota)
                    .Select(q => ToClassQuotaAvailability(q, journee.CodeDevise))
                    .ToList();
            }

            return dto;
        }

        private static void ApplyPriceSummary(
            SiteTouristiqueJournee journee,
            out decimal? prixMin,
            out decimal? prixMax)
        {
            prixMin = null;
            prixMax = null;

            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    if (journee.GlobalQuota == null)
                        return;
                    prixMin = journee.GlobalQuota.PrixUnitaire;
                    prixMax = journee.GlobalQuota.PrixUnitaire;
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    if (journee.ClassQuotas == null || journee.ClassQuotas.Count == 0)
                        return;
                    prixMin = journee.ClassQuotas.Min(q => q.PrixUnitaire);
                    prixMax = journee.ClassQuotas.Max(q => q.PrixUnitaire);
                    break;
            }
        }

        private static void ApplyAvailabilitySummary(
            SiteTouristiqueJournee journee,
            SiteTouristiqueJourneeResponseDto dto)
        {
            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    if (journee.GlobalQuota == null)
                        return;
                    dto.PlacesTotales = journee.GlobalQuota.CapaciteTotale;
                    dto.PlacesRestantes = Math.Max(
                        0,
                        journee.GlobalQuota.CapaciteTotale
                        - journee.GlobalQuota.QuantiteHold
                        - journee.GlobalQuota.QuantiteVendue);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    if (journee.ClassQuotas == null || journee.ClassQuotas.Count == 0)
                        return;
                    dto.PlacesTotales = journee.ClassQuotas.Sum(q => q.CapaciteTotale);
                    dto.PlacesRestantes = journee.ClassQuotas.Sum(q =>
                        Math.Max(0, q.CapaciteTotale - q.QuantiteHold - q.QuantiteVendue));
                    break;

                default:
                    return;
            }

            dto.IsSoldOut = dto.PlacesRestantes == 0;
        }

        public static SiteTouristiqueClassQuotaAvailabilityDto ToClassQuotaAvailability(
            SiteTouristiqueClassQuota quota,
            string codeDevise)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new SiteTouristiqueClassQuotaAvailabilityDto
            {
                IdSiteTouristiqueClassQuota = quota.IdSiteTouristiqueClassQuota,
                IdSiteTouristiqueClasse = quota.IdSiteTouristiqueClasse,
                CodeClasse = quota.Classe?.Code,
                LibelleClasse = quota.Classe?.Libelle ?? string.Empty,
                CapaciteTotale = quota.CapaciteTotale,
                QuantiteHold = quota.QuantiteHold,
                QuantiteVendue = quota.QuantiteVendue,
                QuantiteDisponible = disponible,
                PrixUnitaire = quota.PrixUnitaire,
                CodeDevise = codeDevise
            };
        }

        public static SiteTouristiqueGlobalQuotaAvailabilityDto ToGlobalQuotaAvailability(
            SiteTouristiqueGlobalQuota quota,
            string codeDevise)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new SiteTouristiqueGlobalQuotaAvailabilityDto
            {
                CapaciteTotale = quota.CapaciteTotale,
                QuantiteHold = quota.QuantiteHold,
                QuantiteVendue = quota.QuantiteVendue,
                QuantiteDisponible = disponible,
                PrixUnitaire = quota.PrixUnitaire,
                CodeDevise = codeDevise
            };
        }
    }
}
