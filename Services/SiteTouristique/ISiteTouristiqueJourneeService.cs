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

        /// <summary>
        /// Met à jour une journée : Draft = date/devise/fenêtres/quotas ;
        /// Published = fenêtres + capacité/prix si aucune vente active.
        /// </summary>
        Task<SiteTouristiqueJourneeResponseDto> UpdateAsync(
            int idSiteTouristiqueJournee,
            SiteTouristiqueUpdateJourneeRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Supprime une journée (hard delete) si aucune vente active (HOLD/CONFIRMED)
        /// et aucune commande FlexPay en attente.
        /// </summary>
        Task DeleteAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft-delete : passe la journée en <c>Cancelled</c> (Draft/Published).
        /// Idempotent si déjà Cancelled ; Closed → erreur.
        /// </summary>
        Task<SiteTouristiqueJourneeResponseDto> CancelAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Clôture opérationnelle : passe la journée en <c>Closed</c> (Draft/Published).
        /// Idempotent si déjà Closed ; Cancelled → erreur.
        /// </summary>
        Task<SiteTouristiqueJourneeResponseDto> CloseAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
