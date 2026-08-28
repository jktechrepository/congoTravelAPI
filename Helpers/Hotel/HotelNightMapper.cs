using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelNightMapper
    {
        public static int QuantiteDisponible(HotelNight n) =>
            Math.Max(0, n.CapaciteTotale - n.QuantiteHold - n.QuantiteVendue);

        public static HotelNightResponseDto ToResponseDto(HotelNight n) =>
            new()
            {
                IdHotelNight = n.IdHotelNight,
                IdSociete = n.IdSociete,
                IdHotel = n.IdHotel,
                NightDate = n.NightDate.Date,
                CapaciteTotale = n.CapaciteTotale,
                QuantiteHold = n.QuantiteHold,
                QuantiteVendue = n.QuantiteVendue,
                QuantiteDisponible = QuantiteDisponible(n),
                PrixNuit = n.PrixNuit,
                CodeDevise = n.CodeDevise,
                Status = n.Status.ToString()
            };
    }
}
