using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementTicketService
    {
        Task<EvenementTicketDetailResponseDto?> GetByIdAsync(
            int idEvenementTicket,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListAsync(
            int idSociete,
            EvenementTicketListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListByReservationAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>Retourne null si la réservation est introuvable ou n'appartient pas à la société.</summary>
        Task<IReadOnlyList<EvenementTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idEvenementReservation,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListBySessionAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListByStatusAsync(
            EvenementTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Résultat check ticket avec statut HTTP suggéré.</summary>
    public sealed class EvenementTicketCheckResult
    {
        public EvenementTicketCheckResponseDto Response { get; init; } = new();

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;
    }

    /// <summary>Résultat use ticket avec statut HTTP suggéré.</summary>
    public sealed class EvenementTicketUseResult
    {
        public EvenementTicketUseResponseDto? Response { get; init; }

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;

        public string? ErrorMessage { get; init; }
    }
}
