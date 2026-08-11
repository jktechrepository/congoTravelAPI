using CongoTravel.Models.DTOs.SiteTouristique;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueClasseService
    {
        Task<SiteTouristiqueClasseResponseDto> CreateAsync(
            SiteTouristiqueCreateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueClasseResponseDto?> GetByIdAsync(
            int idSiteTouristiqueClasse,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueClasseResponseDto>> ListAsync(
            int idSociete,
            bool actifsSeulement = false,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueClasseResponseDto?> UpdateAsync(
            int idSiteTouristiqueClasse,
            SiteTouristiqueUpdateClasseRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueClasseResponseDto?> ToggleStatutAsync(
            int idSiteTouristiqueClasse,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueClasseResponseDto?> GetByLibelleAsync(
            string libelle,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
