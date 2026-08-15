using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant
{
    public interface IRestaurantTicketService
    {
        Task<RestaurantTicketDetailResponseDto?> GetByIdAsync(
            int idRestaurantTicket,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListAsync(
            int idSociete,
            RestaurantTicketListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByReservationAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>Retourne null si la réservation est introuvable ou n'appartient pas à la société.</summary>
        Task<IReadOnlyList<RestaurantTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idRestaurantReservation,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByCreneauAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByStatusAsync(
            RestaurantTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<RestaurantTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default);
    }

    public sealed class RestaurantTicketCheckResult
    {
        public RestaurantTicketCheckResponseDto Response { get; init; } = new();

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;
    }

    public sealed class RestaurantTicketUseResult
    {
        public RestaurantTicketUseResponseDto? Response { get; init; }

        public int HttpStatusCode { get; init; } = StatusCodes.Status200OK;

        public string? ErrorMessage { get; set; }
    }
}
