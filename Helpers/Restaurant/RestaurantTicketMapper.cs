using CongoTravel.Models;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantTicketMapper
    {
        public static RestaurantTicketListItemDto ToListItemDto(
            RestaurantTicket ticket,
            RestaurantReservation reservation,
            Societe? societe = null) =>
            new()
            {
                IdRestaurantTicket = ticket.IdRestaurantTicket,
                IdRestaurantReservationLine = ticket.IdRestaurantReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdRestaurantReservation = reservation.IdRestaurantReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                IdRestaurantCreneau = reservation.IdRestaurantCreneau,
                LogoSociete = societe?.Logo ?? reservation.Societe?.Logo
            };

        public static RestaurantTicketDetailResponseDto ToDetailDto(
            RestaurantTicket ticket,
            RestaurantReservation reservation,
            RestaurantCreneau creneau) =>
            new()
            {
                IdRestaurantTicket = ticket.IdRestaurantTicket,
                IdRestaurantReservationLine = ticket.IdRestaurantReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdRestaurantReservation = reservation.IdRestaurantReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                ReservationStatus = reservation.Status.ToString(),
                IdRestaurantCreneau = creneau.IdRestaurantCreneau,
                LogoSociete = reservation.Societe?.Logo,
                DateService = creneau.DateService,
                StartAtUtc = creneau.StartAtUtc,
                EndAtUtc = creneau.EndAtUtc
            };

        public static RestaurantTicketCheckResponseDto ToCheckResponse(
            RestaurantTicket? ticket,
            RestaurantReservation? reservation,
            RestaurantCreneau? creneau,
            RestaurantTicketEligibilityHelper.Result eligibility) =>
            new()
            {
                IdRestaurantTicket = ticket?.IdRestaurantTicket,
                TicketCode = ticket?.TicketCode,
                Status = ticket?.Status.ToString(),
                Statut = eligibility.Statut,
                Message = eligibility.Message,
                EntreeAutorisee = eligibility.EntreeAutorisee,
                IdRestaurantReservation = reservation?.IdRestaurantReservation,
                ReferenceReservation = reservation?.ReferenceReservation,
                IdRestaurantCreneau = creneau?.IdRestaurantCreneau,
                LogoSociete = reservation?.Societe?.Logo,
                DateService = creneau?.DateService,
                StartAtUtc = creneau?.StartAtUtc,
                CustomerRef = reservation?.CustomerRef
            };

        public static RestaurantTicketUseResponseDto ToUseResponse(RestaurantTicket ticket, bool alreadyUsed) =>
            new()
            {
                Ticket = RestaurantReservationMapper.ToTicketResponse(ticket),
                AlreadyUsed = alreadyUsed
            };
    }
}
