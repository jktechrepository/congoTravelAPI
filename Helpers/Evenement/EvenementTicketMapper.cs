using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementTicketMapper
    {
        public static EvenementTicketListItemDto ToListItemDto(
            EvenementTicket ticket,
            EvenementReservation reservation) =>
            new()
            {
                IdEvenementTicket = ticket.IdEvenementTicket,
                IdEvenementReservationLine = ticket.IdEvenementReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdEvenementReservation = reservation.IdEvenementReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                IdEvenementSession = reservation.IdEvenementSession
            };

        public static EvenementTicketDetailResponseDto ToDetailDto(
            EvenementTicket ticket,
            EvenementReservation reservation,
            EvenementSession session) =>
            new()
            {
                IdEvenementTicket = ticket.IdEvenementTicket,
                IdEvenementReservationLine = ticket.IdEvenementReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdEvenementReservation = reservation.IdEvenementReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                ReservationStatus = reservation.Status.ToString(),
                IdEvenementSession = session.IdEvenementSession,
                CodeSession = session.CodeSession,
                LibelleSession = session.Libelle,
                StartAtUtc = session.StartAtUtc,
                EndAtUtc = session.EndAtUtc
            };

        public static EvenementTicketCheckResponseDto ToCheckResponse(
            EvenementTicket? ticket,
            EvenementReservation? reservation,
            EvenementSession? session,
            EvenementTicketEligibilityHelper.Result eligibility) =>
            new()
            {
                IdEvenementTicket = ticket?.IdEvenementTicket,
                TicketCode = ticket?.TicketCode,
                Status = ticket?.Status.ToString(),
                Statut = eligibility.Statut,
                Message = eligibility.Message,
                EntreeAutorisee = eligibility.EntreeAutorisee,
                IdEvenementReservation = reservation?.IdEvenementReservation,
                ReferenceReservation = reservation?.ReferenceReservation,
                IdEvenementSession = session?.IdEvenementSession,
                CodeSession = session?.CodeSession,
                LibelleSession = session?.Libelle,
                StartAtUtc = session?.StartAtUtc,
                CustomerRef = reservation?.CustomerRef
            };

        public static EvenementTicketUseResponseDto ToUseResponse(EvenementTicket ticket, bool alreadyUsed) =>
            new()
            {
                Ticket = EvenementReservationMapper.ToTicketResponse(ticket),
                AlreadyUsed = alreadyUsed
            };
    }
}
