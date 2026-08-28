using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelAvailabilityService
    {
        Task<HotelAvailabilityResponseDto> GetAvailabilityAsync(
            int idHotel,
            DateTime from,
            DateTime to,
            int? idHotelRoomType = null,
            int? idSociete = null,
            bool publishedOnly = true,
            HotelInventoryMode? inventoryMode = null,
            CancellationToken cancellationToken = default);
    }
}
