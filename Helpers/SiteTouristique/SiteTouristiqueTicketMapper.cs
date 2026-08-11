using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueTicketMapper
    {
        public static SiteTouristiqueTicketListItemDto ToListItemDto(
            SiteTouristiqueTicket ticket,
            SiteTouristiqueReservation reservation) =>
            new()
            {
                IdSiteTouristiqueTicket = ticket.IdSiteTouristiqueTicket,
                IdSiteTouristiqueReservationLine = ticket.IdSiteTouristiqueReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                IdSiteTouristiqueJournee = reservation.IdSiteTouristiqueJournee
            };

        public static SiteTouristiqueTicketDetailResponseDto ToDetailDto(
            SiteTouristiqueTicket ticket,
            SiteTouristiqueReservation reservation,
            SiteTouristiqueJournee journee) =>
            new()
            {
                IdSiteTouristiqueTicket = ticket.IdSiteTouristiqueTicket,
                IdSiteTouristiqueReservationLine = ticket.IdSiteTouristiqueReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc,
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                ReservationStatus = reservation.Status.ToString(),
                IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                IdSiteTouristique = journee.IdSiteTouristique,
                CodeLieu = journee.Lieu?.CodeLieu,
                NomLieu = journee.Lieu?.Nom,
                DateVisite = journee.DateVisite
            };

        public static SiteTouristiqueTicketCheckResponseDto ToCheckResponse(
            SiteTouristiqueTicket? ticket,
            SiteTouristiqueReservation? reservation,
            SiteTouristiqueJournee? journee,
            SiteTouristiqueTicketEligibilityHelper.Result eligibility) =>
            new()
            {
                IdSiteTouristiqueTicket = ticket?.IdSiteTouristiqueTicket,
                TicketCode = ticket?.TicketCode,
                Status = ticket?.Status.ToString(),
                Statut = eligibility.Statut,
                Message = eligibility.Message,
                EntreeAutorisee = eligibility.EntreeAutorisee,
                IdSiteTouristiqueReservation = reservation?.IdSiteTouristiqueReservation,
                ReferenceReservation = reservation?.ReferenceReservation,
                IdSiteTouristiqueJournee = journee?.IdSiteTouristiqueJournee,
                CodeLieu = journee?.Lieu?.CodeLieu,
                NomLieu = journee?.Lieu?.Nom,
                DateVisite = journee?.DateVisite,
                CustomerRef = reservation?.CustomerRef
            };

        public static SiteTouristiqueTicketUseResponseDto ToUseResponse(SiteTouristiqueTicket ticket, bool alreadyUsed) =>
            new()
            {
                Ticket = SiteTouristiqueReservationMapper.ToTicketResponse(ticket),
                AlreadyUsed = alreadyUsed
            };
    }
}
