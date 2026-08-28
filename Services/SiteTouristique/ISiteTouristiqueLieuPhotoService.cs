using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueLieuPhotoService
    {
        Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> GetByLieuIdAsync(
            int idSiteTouristique,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<PhotoContentPayload?> GetContentAsync(
            int idSiteTouristique,
            int idSociete,
            int idSiteTouristiqueLieuPhoto,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuPhoto> AddPhotoAsync(
            int idSiteTouristique,
            int idSociete,
            AddSiteTouristiqueLieuPhotoDto dto,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueLieuPhoto> AddPhotoFromFileAsync(
            int idSiteTouristique,
            int idSociete,
            IFormFile file,
            int? ordre = null,
            string? fileName = null,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création du lieu (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idSiteTouristique,
            int idSociete,
            IReadOnlyList<AddSiteTouristiqueLieuPhotoDto>? photos,
            CancellationToken cancellationToken = default);

        /// <summary>Remplacement complet via fichiers multipart (0–3). Liste vide = vider la galerie.</summary>
        Task<IReadOnlyList<SiteTouristiqueLieuPhoto>> ReplaceAllFromFilesAsync(
            int idSiteTouristique,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
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
