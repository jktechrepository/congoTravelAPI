using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueReservationService
    {
        Task<SiteTouristiqueReservationResponseDto?> GetByIdAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueReservationListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueReservationResponseDto?> GetByReferenceAsync(
            string reference,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListBySessionAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>Retourne null si la session est introuvable ou n'appartient pas à la société.</summary>
        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>?> ListBySocieteAndSessionAsync(
            int idSociete,
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByStatusAsync(
            SiteTouristiqueReservationStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketResponseDto>?> GetTicketsByReservationAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueCancelReservationResponseDto> CancelAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
