using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueJourneeService
    {
        Task<SiteTouristiqueJourneeResponseDto> CreateDraftAsync(
            SiteTouristiqueCreateJourneeRequestDto request,
            int idSociete,
            int? idSiteTouristiquePlanification = null,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueJourneeResponseDto?> GetByIdAsync(
            int idSiteTouristiqueJournee,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueJourneeResponseDto?> GetPublishedByIdAsync(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListPublishedGlobalAsync(
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByStatusAsync(
            SiteTouristiqueStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByInventoryModeAsync(
            SiteTouristiqueInventoryMode inventoryMode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateAsync(
            DateOnly dateVisite,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateRangeAsync(
            DateOnly dateDebut,
            DateOnly dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueJourneeResponseDto> PublishAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
