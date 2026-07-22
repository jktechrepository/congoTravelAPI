using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementSessionMapper
    {
        public static EvenementSessionListItemDto ToListItemDto(EvenementSession session) =>
            new()
            {
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = session.IdSociete,
                CodeSession = session.CodeSession,
                Libelle = session.Libelle,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc,
                InventoryMode = session.InventoryMode.ToString(),
                Status = session.Status.ToString(),
                DateCreation = session.DateCreation,
                DateModification = session.DateModification
            };

        public static EvenementSessionResponseDto ToResponseDto(EvenementSession session)
        {
            var dto = new EvenementSessionResponseDto
            {
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = session.IdSociete,
                CodeSession = session.CodeSession,
                Libelle = session.Libelle,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc,
                InventoryMode = session.InventoryMode.ToString(),
                Status = session.Status.ToString(),
                DateCreation = session.DateCreation,
                DateModification = session.DateModification
            };

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

            return dto;
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
