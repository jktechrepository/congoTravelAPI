using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace CongoTravel.Services.Repositories
{
    public interface IVehiculePhotoService
    {
        Task<IReadOnlyList<PhotoVehicule>> GetByVehiculeIdAsync(
            int idVehicule,
            bool includePhotoBase64 = false);

        Task<PhotoContentPayload?> GetContentAsync(
            int idVehicule,
            int idPhotoVehicule,
            CancellationToken cancellationToken = default);

        Task<PhotoVehicule> AddPhotoAsync(int idVehicule, AddPhotoVehiculeDto dto);

        Task<PhotoVehicule> AddPhotoFromFileAsync(
            int idVehicule,
            IFormFile file,
            int? ordre = null,
            string? fileName = null,
            CancellationToken cancellationToken = default);
        /// <summary>Ajoute 1 à 3 photos à la création du véhicule (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos);
        /// <summary>
        /// null = ne pas toucher aux photos ; [] = tout supprimer ; sinon remplacement complet (max 3).
        /// </summary>
        Task ReplaceAllPhotosOnUpdateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos);

        /// <summary>Remplacement complet via fichiers multipart (0–3). Liste vide = vider la galerie.</summary>
        Task<IReadOnlyList<PhotoVehicule>> ReplaceAllFromFilesAsync(
            int idVehicule,
            IReadOnlyList<IFormFile> files,
            IReadOnlyList<int>? ordres = null,
            CancellationToken cancellationToken = default);

        Task<PhotoVehicule?> UpdateOrdreAsync(int idVehicule, int idPhotoVehicule, int ordre);
        Task<bool> DeletePhotoAsync(int idVehicule, int idPhotoVehicule);
    }
}
