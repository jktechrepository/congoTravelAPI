using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementSessionPhotoService
    {
        Task<IReadOnlyList<EvenementSessionPhoto>> GetBySessionIdAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default,
            bool includePhotoBase64 = false);

        Task<PhotoContentPayload?> GetContentAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionPhoto> AddPhotoAsync(
            int idEvenementSession,
            int idSociete,
            AddEvenementSessionPhotoDto dto,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionPhoto> AddPhotoFromFileAsync(
            int idEvenementSession,
            int idSociete,
            IFormFile file,
            int? ordre = null,
            string? fileName = null,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création de session (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idEvenementSession,
            int idSociete,
            IReadOnlyList<AddEvenementSessionPhotoDto>? photos,
            CancellationToken cancellationToken = default);

        /// <summary>Remplacement complet via fichiers multipart (0–3). Liste vide = vider la galerie.</summary>
        Task<IReadOnlyList<EvenementSessionPhoto>> ReplaceAllFromFilesAsync(
            int idEvenementSession,
            int idSociete,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionPhoto?> UpdateOrdreAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            int ordre,
            CancellationToken cancellationToken = default);

        Task<bool> DeletePhotoAsync(
            int idEvenementSession,
            int idSociete,
            int idEvenementSessionPhoto,
            CancellationToken cancellationToken = default);
    }
}
