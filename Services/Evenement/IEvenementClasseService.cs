using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementClasseService
    {
        Task<EvenementClasseResponseDto> CreateAsync(
            EvenementCreateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementClasseResponseDto?> GetByIdAsync(
            int idEvenementClasse,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementClasseResponseDto>> ListAsync(
            int idSociete,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default);

        Task<EvenementClasseResponseDto?> UpdateAsync(
            int idEvenementClasse,
            EvenementUpdateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementClasseResponseDto?> ToggleStatutAsync(
            int idEvenementClasse,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementClasseResponseDto?> GetByLibelleAsync(
            string libelle,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
