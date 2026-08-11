using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantCreneauMapper
    {
        public static RestaurantCreneauListItemDto ToListItemDto(RestaurantCreneau creneau)
        {
            var dto = new RestaurantCreneauListItemDto
            {
                IdRestaurantCreneau = creneau.IdRestaurantCreneau,
                IdSociete = creneau.IdSociete,
                NomSociete = creneau.Societe?.Nom,
                IdRestaurant = creneau.IdRestaurant,
                CodeRestaurant = creneau.Restaurant?.CodeRestaurant,
                NomRestaurant = creneau.Restaurant?.Nom,
                IdSite = creneau.Restaurant?.IdSite,
                NomSite = creneau.Restaurant?.Site?.NomSite,
                DateService = creneau.DateService,
                StartAtUtc = creneau.StartAtUtc,
                EndAtUtc = creneau.EndAtUtc,
                InventoryMode = creneau.InventoryMode.ToString(),
                Status = creneau.Status.ToString(),
                CodeDevise = creneau.CodeDevise,
                MontantAcompte = creneau.MontantAcompte,
                DateCreation = creneau.DateCreation,
                DateModification = creneau.DateModification
            };

            if (creneau.InventoryMode == RestaurantInventoryMode.GlobalQuota && creneau.GlobalQuota != null)
                dto.PrixUnitaire = creneau.GlobalQuota.PrixUnitaire;

            return dto;
        }

        public static RestaurantCreneauResponseDto ToResponseDto(RestaurantCreneau creneau)
        {
            var dto = new RestaurantCreneauResponseDto
            {
                IdRestaurantCreneau = creneau.IdRestaurantCreneau,
                IdSociete = creneau.IdSociete,
                NomSociete = creneau.Societe?.Nom,
                IdRestaurant = creneau.IdRestaurant,
                CodeRestaurant = creneau.Restaurant?.CodeRestaurant,
                NomRestaurant = creneau.Restaurant?.Nom,
                IdSite = creneau.Restaurant?.IdSite,
                NomSite = creneau.Restaurant?.Site?.NomSite,
                DateService = creneau.DateService,
                StartAtUtc = creneau.StartAtUtc,
                EndAtUtc = creneau.EndAtUtc,
                InventoryMode = creneau.InventoryMode.ToString(),
                Status = creneau.Status.ToString(),
                CodeDevise = creneau.CodeDevise,
                MontantAcompte = creneau.MontantAcompte,
                DateCreation = creneau.DateCreation,
                DateModification = creneau.DateModification
            };

            if (creneau.InventoryMode == RestaurantInventoryMode.GlobalQuota && creneau.GlobalQuota != null)
            {
                dto.GlobalQuota = ToGlobalQuotaDto(creneau.GlobalQuota, creneau.CodeDevise);
                dto.CouvertsTotaux = creneau.GlobalQuota.CapaciteTotale;
                dto.CouvertsRestants = Math.Max(
                    0,
                    creneau.GlobalQuota.CapaciteTotale
                    - creneau.GlobalQuota.QuantiteHold
                    - creneau.GlobalQuota.QuantiteVendue);
                dto.IsSoldOut = dto.CouvertsRestants == 0;
            }
            else if (creneau.InventoryMode == RestaurantInventoryMode.ClassQuota)
            {
                var zoneQuotas = (creneau.ZoneQuotas ?? Array.Empty<RestaurantCreneauZoneQuota>()).ToList();
                dto.ZoneQuotas = zoneQuotas
                    .OrderBy(q => q.IdRestaurantCreneauZoneQuota)
                    .Select(q => ToZoneQuotaDto(q, creneau.CodeDevise))
                    .ToList();

                dto.CouvertsTotaux = zoneQuotas.Sum(q => q.CapaciteTotale);
                dto.CouvertsRestants = zoneQuotas.Sum(q =>
                    Math.Max(0, q.CapaciteTotale - q.QuantiteHold - q.QuantiteVendue));
                dto.IsSoldOut = dto.CouvertsRestants == 0;
            }

            return dto;
        }

        public static RestaurantCreneauGlobalQuotaDto ToGlobalQuotaDto(
            RestaurantCreneauGlobalQuota quota,
            string codeDevise)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new RestaurantCreneauGlobalQuotaDto
            {
                CapaciteTotale = quota.CapaciteTotale,
                QuantiteHold = quota.QuantiteHold,
                QuantiteVendue = quota.QuantiteVendue,
                QuantiteDisponible = disponible,
                PrixUnitaire = quota.PrixUnitaire,
                CodeDevise = codeDevise
            };
        }

        public static RestaurantCreneauZoneQuotaDto ToZoneQuotaDto(
            RestaurantCreneauZoneQuota quota,
            string codeDevise)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new RestaurantCreneauZoneQuotaDto
            {
                IdRestaurantCreneauZoneQuota = quota.IdRestaurantCreneauZoneQuota,
                IdRestaurantZone = quota.IdRestaurantZone,
                CodeZone = quota.Zone?.Code,
                LibelleZone = quota.Zone?.Libelle ?? string.Empty,
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
