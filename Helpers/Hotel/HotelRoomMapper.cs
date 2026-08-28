using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelRoomMapper
    {
        public static HotelRoomResponseDto ToResponseDto(HotelRoom room) =>
            new()
            {
                IdHotelRoom = room.IdHotelRoom,
                IdSociete = room.IdSociete,
                IdHotel = room.IdHotel,
                IdHotelRoomType = room.IdHotelRoomType,
                Numero = room.Numero,
                Etage = room.Etage,
                Libelle = room.Libelle,
                IsActif = room.IsActif,
                DateCreation = room.DateCreation,
                DateModification = room.DateModification
            };

        public static HotelRoomAssignmentResponseDto ToAssignmentDto(HotelRoomAssignment a) =>
            new()
            {
                IdHotelRoomAssignment = a.IdHotelRoomAssignment,
                IdHotelReservation = a.IdHotelReservation,
                IdHotelReservationLine = a.IdHotelReservationLine,
                IdHotelRoom = a.IdHotelRoom,
                Numero = a.Room?.Numero,
                DateAttributionUtc = a.DateAttributionUtc
            };
    }
}
