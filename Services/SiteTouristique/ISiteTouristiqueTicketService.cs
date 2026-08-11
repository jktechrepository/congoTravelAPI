using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public interface ISiteTouristiqueTicketService
    {
        Task<SiteTouristiqueTicketDetailResponseDto?> GetByIdAsync(
            int idSiteTouristiqueTicket,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueTicketListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByReservationAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>Retourne null si la réservation est introuvable ou n'appartient pas à la société.</summary>
        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idSiteTouristiqueReservation,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListBySessionAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByStatusAsync(
            SiteTouristiqueTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<SiteTouristiqueTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Résultat check ticket avec statut HTTP suggéré.</summary>
    public sealed class SiteTouristiqueTicketCheckResult
    {
        public SiteTouristiqueTicketCheckResponseDto Response { get; init; } = new();

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;
    }

    /// <summary>Résultat use ticket avec statut HTTP suggéré.</summary>
    public sealed class SiteTouristiqueTicketUseResult
    {
        public SiteTouristiqueTicketUseResponseDto? Response { get; init; }

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;

        public string? ErrorMessage { get; init; }
    }
}
