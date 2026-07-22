using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Services.Repositories
{
    public interface IVehiculePhotoService
    {
        Task<IReadOnlyList<PhotoVehicule>> GetByVehiculeIdAsync(int idVehicule);
        Task<PhotoVehicule> AddPhotoAsync(int idVehicule, AddPhotoVehiculeDto dto);
        /// <summary>Ajoute 1 à 3 photos à la création du véhicule (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos);
        /// <summary>
        /// null = ne pas toucher aux photos ; [] = tout supprimer ; sinon remplacement complet (max 3).
        /// </summary>
        Task ReplaceAllPhotosOnUpdateAsync(int idVehicule, IReadOnlyList<AddPhotoVehiculeDto>? photos);
        Task<PhotoVehicule?> UpdateOrdreAsync(int idVehicule, int idPhotoVehicule, int ordre);
        Task<bool> DeletePhotoAsync(int idVehicule, int idPhotoVehicule);
    }
}
