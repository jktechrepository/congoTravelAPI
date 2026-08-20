using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementReservationService
    {
        Task<EvenementReservationResponseDto?> GetByIdAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementReservationListItemDto>> ListAsync(
            int idSociete,
            EvenementReservationListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<EvenementReservationResponseDto?> GetByReferenceAsync(
            string reference,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementReservationListItemDto>> ListBySessionAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>Retourne null si la session est introuvable ou n'appartient pas à la société.</summary>
        Task<IReadOnlyList<EvenementReservationListItemDto>?> ListBySocieteAndSessionAsync(
            int idSociete,
            int idEvenementSession,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementReservationListItemDto>> ListByStatusAsync(
            EvenementReservationStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementReservationListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementReservationListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketResponseDto>?> GetTicketsByReservationAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementCancelReservationResponseDto> CancelAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Supprime définitivement une réservation jamais confirmée (HOLD/CANCELLED/EXPIRED)
        /// sans paiement SUCCEEDED — tickets + payments + réservation.
        /// No-op idempotent si absente ou non éligible.
        /// </summary>
        Task<bool> PurgeNeverConfirmedAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
