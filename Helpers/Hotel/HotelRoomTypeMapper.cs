using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelRoomTypeMapper
    {
        public static HotelRoomTypeResponseDto ToResponseDto(HotelRoomType roomType) =>
            new()
            {
                IdHotelRoomType = roomType.IdHotelRoomType,
                IdSociete = roomType.IdSociete,
                IdHotel = roomType.IdHotel,
                Code = roomType.Code,
                Libelle = roomType.Libelle,
                Description = roomType.Description,
                CapacitePersonnesMax = roomType.CapacitePersonnesMax,
                PrixNuitReference = roomType.PrixNuitReference,
                CodeDevise = roomType.CodeDevise,
                Status = roomType.Status.ToString(),
                DateCreation = roomType.DateCreation,
                DateModification = roomType.DateModification
            };
    }
}
