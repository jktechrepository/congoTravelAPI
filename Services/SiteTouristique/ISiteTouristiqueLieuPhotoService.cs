using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueLieuPhotoService
    {
        Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> GetByLieuIdAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuPhoto> AddPhotoAsync(
            int idSiteTouristique,
            int idSociete,
            AddSiteTouristiqueLieuPhotoDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création du lieu (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idSiteTouristique,
            int idSociete,
            IReadOnlyList<AddSiteTouristiqueLieuPhotoDto>? photos,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuPhoto?> UpdateOrdreAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            int ordre,
            CancellationToken cancellationToken = default);

        Task<bool> DeletePhotoAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            CancellationToken cancellationToken = default);
    }
}
