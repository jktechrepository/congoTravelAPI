using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueLieuService
    {
        Task<SiteTouristiqueLieuResponseDto> CreateDraftAsync(
            SiteTouristiqueCreateLieuRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuResponseDto?> GetByIdAsync(
            int idSiteTouristique,
            int? idSociete = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<SiteTouristiqueLieuResponseDto?> GetPublishedByIdAsync(
            int idSiteTouristique,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<SiteTouristiqueLieuResponseDto?> GetByCodeAsync(
            string codeLieu,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<SiteTouristiqueLieuResponseDto?> GetPublishedByCodeAsync(
            string codeLieu,
            int? idSociete = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueLieuListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListPublishedGlobalAsync(
            SiteTouristiqueLieuListFilter? filter = null,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<IReadOnlyList<SiteTouristiqueLieuListItemDto>> ListByStatusAsync(
            SiteTouristiqueStatus status,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<SiteTouristiqueLieuResponseDto?> UpdateAsync(
            int idSiteTouristique,
            SiteTouristiqueUpdateLieuRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuResponseDto> PublishAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
