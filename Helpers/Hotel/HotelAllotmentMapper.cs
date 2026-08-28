using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelAllotmentMapper
    {
        public static int QuantiteDisponible(HotelNightAllotment a) =>
            Math.Max(0, a.CapaciteTotale - a.QuantiteHold - a.QuantiteVendue);

        public static HotelAllotmentResponseDto ToResponseDto(HotelNightAllotment a) =>
            new()
            {
                IdHotelNightAllotment = a.IdHotelNightAllotment,
                IdSociete = a.IdSociete,
                IdHotel = a.IdHotel,
                IdHotelRoomType = a.IdHotelRoomType,
                CodeRoomType = a.RoomType?.Code,
                LibelleRoomType = a.RoomType?.Libelle,
                NightDate = a.NightDate.Date,
                CapaciteTotale = a.CapaciteTotale,
                QuantiteHold = a.QuantiteHold,
                QuantiteVendue = a.QuantiteVendue,
                QuantiteDisponible = QuantiteDisponible(a),
                PrixNuit = a.PrixNuit,
                CodeDevise = a.CodeDevise,
                Status = a.Status.ToString()
            };

        public static HotelAvailabilityNightDto ToAvailabilityNight(HotelNightAllotment a) =>
            new()
            {
                NightDate = a.NightDate.Date,
                IdHotelRoomType = a.IdHotelRoomType,
                CodeRoomType = a.RoomType?.Code,
                LibelleRoomType = a.RoomType?.Libelle,
                IdHotelNightAllotment = a.IdHotelNightAllotment,
                CapaciteTotale = a.CapaciteTotale,
                QuantiteHold = a.QuantiteHold,
                QuantiteVendue = a.QuantiteVendue,
                QuantiteDisponible = QuantiteDisponible(a),
                PrixNuit = a.PrixNuit,
                CodeDevise = a.CodeDevise
            };
    }
}
