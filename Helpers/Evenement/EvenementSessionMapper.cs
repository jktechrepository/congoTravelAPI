using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementSessionMapper
    {
        public static EvenementSessionListItemDto ToListItemDto(EvenementSession session)
        {
            var dto = new EvenementSessionListItemDto
            {
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = session.IdSociete,
                NomSociete = session.Societe?.Nom,
                IdSite = session.IdSite,
                NomSite = session.Site?.NomSite,
                CodeSession = session.CodeSession,
                Libelle = session.Libelle,
                Description = session.Description,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc,
                InventoryMode = session.InventoryMode.ToString(),
                TypeEvenement = session.TypeEvenement.ToString(),
                NomOrganisateur = session.NomOrganisateur,
                TelephoneOrganisateur = session.TelephoneOrganisateur,
                MailOrganisateur = session.MailOrganisateur,
                Ville = session.Ville,
                Commune = session.Commune,
                Quartier = session.Quartier,
                Avenue = session.Avenue,
                Numero = session.Numero,
                Status = session.Status.ToString(),
                DateCreation = session.DateCreation,
                DateModification = session.DateModification,
                PhotoCouverture = ResolveCoverPhoto(session)
            };

            ApplyPriceSummary(session, out var prixMin, out var prixMax, out var codeDevise);
            dto.PrixMin = prixMin;
            dto.PrixMax = prixMax;
            dto.CodeDevise = codeDevise;
            return dto;
        }

        public static EvenementSessionResponseDto ToResponseDto(EvenementSession session)
        {
            var dto = new EvenementSessionResponseDto
            {
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = session.IdSociete,
                NomSociete = session.Societe?.Nom,
                IdSite = session.IdSite,
                NomSite = session.Site?.NomSite,
                CodeSession = session.CodeSession,
                Libelle = session.Libelle,
                Description = session.Description,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc,
                InventoryMode = session.InventoryMode.ToString(),
                TypeEvenement = session.TypeEvenement.ToString(),
                NomOrganisateur = session.NomOrganisateur,
                TelephoneOrganisateur = session.TelephoneOrganisateur,
                MailOrganisateur = session.MailOrganisateur,
                Ville = session.Ville,
                Commune = session.Commune,
                Quartier = session.Quartier,
                Avenue = session.Avenue,
                Numero = session.Numero,
                Status = session.Status.ToString(),
                DateCreation = session.DateCreation,
                DateModification = session.DateModification,
                PhotoCouverture = ResolveCoverPhoto(session)
            };

            ApplyPriceSummary(session, out var prixMin, out var prixMax, out var codeDevise);
            dto.PrixMin = prixMin;
            dto.PrixMax = prixMax;
            dto.CodeDevise = codeDevise;

            ApplyAvailabilitySummary(session, dto);

            if (session.GlobalQuota != null)
                dto.GlobalQuota = ToGlobalQuotaAvailability(session.GlobalQuota);

            if (session.ClassQuotas.Count > 0)
            {
                dto.ClassQuotas = session.ClassQuotas
                    .OrderBy(q => q.IdEvenementSessionClassQuota)
                    .Select(ToClassQuotaAvailability)
                    .ToList();
            }

            if (session.Seats.Count > 0)
            {
                dto.Seats = session.Seats
                    .OrderBy(s => s.SeatCode)
                    .Select(ToSeatAvailability)
                    .ToList();
            }

            if (session.Photos.Count > 0)
            {
                dto.Photos = session.Photos
                    .Where(p => p.Statut)
                    .OrderBy(p => p.Ordre)
                    .Select(ToPhotoDto)
                    .ToList();
            }

            return dto;
        }

        private static EvenementSessionPhotoDto? ResolveCoverPhoto(EvenementSession session)
        {
            var cover = session.Photos?
                .Where(p => p.Statut)
                .OrderBy(p => p.Ordre)
                .FirstOrDefault();

            return cover == null ? null : ToPhotoDto(cover);
        }

        private static void ApplyPriceSummary(
            EvenementSession session,
            out decimal? prixMin,
            out decimal? prixMax,
            out string? codeDevise)
        {
            prixMin = null;
            prixMax = null;
            codeDevise = null;

            switch (session.InventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    if (session.GlobalQuota == null)
                        return;
                    prixMin = session.GlobalQuota.PrixUnitaire;
                    prixMax = session.GlobalQuota.PrixUnitaire;
                    codeDevise = session.GlobalQuota.CodeDevise;
                    break;

                case EvenementInventoryMode.ClassQuota:
                    ApplyMinMaxFromPrices(
                        session.ClassQuotas?.Select(q => (q.PrixUnitaire, q.CodeDevise)),
                        out prixMin,
                        out prixMax,
                        out codeDevise);
                    break;

                case EvenementInventoryMode.SeatNumbered:
                    ApplyMinMaxFromPrices(
                        session.Seats?.Select(s => (s.PrixUnitaire, s.CodeDevise)),
                        out prixMin,
                        out prixMax,
                        out codeDevise);
                    break;
            }
        }

        private static void ApplyMinMaxFromPrices(
            IEnumerable<(decimal Prix, string CodeDevise)>? prices,
            out decimal? prixMin,
            out decimal? prixMax,
            out string? codeDevise)
        {
            prixMin = null;
            prixMax = null;
            codeDevise = null;

            if (prices == null)
                return;

            var list = prices.ToList();
            if (list.Count == 0)
                return;

            var min = list.Min(p => p.Prix);
            var max = list.Max(p => p.Prix);
            prixMin = min;
            prixMax = max;
            codeDevise = list
                .Where(p => p.Prix == min && !string.IsNullOrWhiteSpace(p.CodeDevise))
                .Select(p => p.CodeDevise)
                .FirstOrDefault()
                ?? list.Select(p => p.CodeDevise).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
        }

        private static void ApplyAvailabilitySummary(
            EvenementSession session,
            EvenementSessionResponseDto dto)
        {
            switch (session.InventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    if (session.GlobalQuota == null)
                        return;
                    dto.PlacesTotales = session.GlobalQuota.CapaciteTotale;
                    dto.PlacesRestantes = Math.Max(
                        0,
                        session.GlobalQuota.CapaciteTotale
                        - session.GlobalQuota.QuantiteHold
                        - session.GlobalQuota.QuantiteVendue);
                    break;

                case EvenementInventoryMode.ClassQuota:
                    if (session.ClassQuotas == null || session.ClassQuotas.Count == 0)
                        return;
                    dto.PlacesTotales = session.ClassQuotas.Sum(q => q.CapaciteTotale);
                    dto.PlacesRestantes = session.ClassQuotas.Sum(q =>
                        Math.Max(0, q.CapaciteTotale - q.QuantiteHold - q.QuantiteVendue));
                    break;

                case EvenementInventoryMode.SeatNumbered:
                    if (session.Seats == null || session.Seats.Count == 0)
                        return;
                    dto.PlacesTotales = session.Seats.Count;
                    dto.PlacesRestantes = session.Seats.Count(s =>
                        s.SeatStatus == EvenementSessionSeatStatus.Available);
                    break;

                default:
                    return;
            }

            dto.IsSoldOut = dto.PlacesRestantes == 0;
        }

        public static EvenementSessionPhotoDto ToPhotoDto(EvenementSessionPhoto photo)
        {
            var contentType = string.IsNullOrWhiteSpace(photo.TypeMIME)
                ? "image/jpeg"
                : photo.TypeMIME!;

            return new EvenementSessionPhotoDto
            {
                IdEvenementSessionPhoto = photo.IdEvenementSessionPhoto,
                IdEvenementSession = photo.IdEvenementSession,
                PhotoBase64 = photo.PhotoData.Length > 0
                    ? VehiculePhotoBase64Helper.ToDataUrl(photo.PhotoData, contentType)
                    : string.Empty,
                Ordre = photo.Ordre,
                OriginalFileName = photo.OriginalFileName,
                TypeMIME = photo.TypeMIME,
                FileSize = photo.FileSize,
                Statut = photo.Statut,
                DateCreation = photo.DateCreation,
                DateModification = photo.DateModification
            };
        }

        public static EvenementSeatAvailabilityDto ToSeatAvailability(EvenementSessionSeat seat) =>
            new()
            {
                IdEvenementSessionSeat = seat.IdEvenementSessionSeat,
                SeatCode = seat.SeatCode,
                SeatStatus = seat.SeatStatus.ToString(),
                CodeSection = seat.Section?.CodeSection,
                LibelleSection = seat.Section?.Libelle,
                IdEvenementClasse = seat.IdEvenementClasse,
                CodeClasse = seat.Classe?.CodeClasse,
                LibelleClasse = seat.Classe?.Libelle,
                PrixUnitaire = seat.PrixUnitaire,
                CodeDevise = seat.CodeDevise
            };

        public static EvenementClassQuotaAvailabilityDto ToClassQuotaAvailability(
            EvenementSessionClassQuota quota)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new EvenementClassQuotaAvailabilityDto
            {
                IdEvenementSessionClassQuota = quota.IdEvenementSessionClassQuota,
                IdEvenementClasse = quota.IdEvenementClasse,
                CodeClasse = quota.Classe?.CodeClasse ?? string.Empty,
                LibelleClasse = quota.Classe?.Libelle ?? string.Empty,
                CapaciteTotale = quota.CapaciteTotale,
                QuantiteHold = quota.QuantiteHold,
                QuantiteVendue = quota.QuantiteVendue,
                QuantiteDisponible = disponible,
                PrixUnitaire = quota.PrixUnitaire,
                CodeDevise = quota.CodeDevise
            };
        }

        public static EvenementGlobalQuotaAvailabilityDto ToGlobalQuotaAvailability(
            EvenementSessionGlobalQuota quota)
        {
            var disponible = Math.Max(0, quota.CapaciteTotale - quota.QuantiteHold - quota.QuantiteVendue);
            return new EvenementGlobalQuotaAvailabilityDto
            {
                CapaciteTotale = quota.CapaciteTotale,
                QuantiteHold = quota.QuantiteHold,
                QuantiteVendue = quota.QuantiteVendue,
                QuantiteDisponible = disponible,
                PrixUnitaire = quota.PrixUnitaire,
                CodeDevise = quota.CodeDevise
            };
        }
    }
}
