using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelPhotoService
    {
        Task<IReadOnlyList<HotelPhoto>> GetByHotelIdAsync(int idHotel, int idSociete, CancellationToken cancellationToken = default, bool includePhotoBase64 = false);
        Task<PhotoContentPayload?> GetContentAsync(int idHotel, int idSociete, int idHotelPhoto, CancellationToken cancellationToken = default);
        Task AddPhotosOnCreateAsync(int idHotel, int idSociete, IReadOnlyList<AddHotelPhotoDto>? photos, CancellationToken cancellationToken = default);
        Task<HotelPhoto> AddPhotoAsync(int idHotel, int idSociete, AddHotelPhotoDto dto, CancellationToken cancellationToken = default);
        Task<HotelPhoto> AddPhotoFromFileAsync(int idHotel, int idSociete, IFormFile file, int? ordre = null, string? fileName = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HotelPhoto>> ReplaceAllFromFilesAsync(int idHotel, int idSociete, IReadOnlyList<IFormFile> files, IReadOnlyList<int>? ordres = null, CancellationToken cancellationToken = default);
        Task<HotelPhoto?> UpdateOrdreAsync(int idHotel, int idSociete, int idHotelPhoto, int ordre, CancellationToken cancellationToken = default);
        Task<bool> DeletePhotoAsync(int idHotel, int idSociete, int idHotelPhoto, CancellationToken cancellationToken = default);
    }
}
