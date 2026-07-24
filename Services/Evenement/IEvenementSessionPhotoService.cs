using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementSessionPhotoService
    {
        Task<IReadOnlyList<EvenementSessionPhoto>> GetBySessionIdAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionPhoto> AddPhotoAsync(
            int idEvenementSession,
            int idSociete,
            AddEvenementSessionPhotoDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>Ajoute 1 à 3 photos à la création de session (liste vide ou null = rien).</summary>
        Task AddPhotosOnCreateAsync(
            int idEvenementSession,
            int idSociete,
            IReadOnlyList<AddEvenementSessionPhotoDto>? photos,
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
