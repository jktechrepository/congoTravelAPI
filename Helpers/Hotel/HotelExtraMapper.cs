using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelExtraMapper
    {
        public static HotelExtraResponseDto ToResponseDto(HotelExtra extra) =>
            new()
            {
                IdHotelExtra = extra.IdHotelExtra,
                IdSociete = extra.IdSociete,
                IdHotel = extra.IdHotel,
                Code = extra.Code,
                Libelle = extra.Libelle,
                PrixUnitaire = extra.PrixUnitaire,
                CodeDevise = extra.CodeDevise,
                PricingUnit = extra.PricingUnit.ToString(),
                IsActif = extra.IsActif,
                DateCreation = extra.DateCreation,
                DateModification = extra.DateModification
            };

        public static HotelReservationExtraResponseDto ToReservationExtraDto(HotelReservationExtra line) =>
            new()
            {
                IdHotelReservationExtra = line.IdHotelReservationExtra,
                IdHotelExtra = line.IdHotelExtra,
                Code = line.Extra?.Code,
                Libelle = line.Extra?.Libelle,
                PricingUnit = line.Extra?.PricingUnit.ToString() ?? string.Empty,
                Quantity = line.Quantity,
                PrixUnitaireSnapshot = line.PrixUnitaireSnapshot,
                MontantLigne = line.MontantLigne,
                CodeDevise = line.CodeDevise
            };
    }
}
