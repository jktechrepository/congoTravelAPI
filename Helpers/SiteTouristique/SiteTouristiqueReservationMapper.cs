using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueReservationMapper
    {
        public static SiteTouristiqueHoldResponseDto ToHoldResponse(SiteTouristiqueReservation reservation) =>
            new()
            {
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc
                    ?? throw new InvalidOperationException("Une réservation HOLD doit avoir ExpiresAtUtc."),
                AmountPreview = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise
            };

        public static SiteTouristiqueConfirmPaymentResponseDto ToConfirmPaymentResponse(
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                Reservation = ToResponseDto(reservation),
                Payment = ToPaymentResponse(payment),
                AlreadyConfirmed = alreadyConfirmed
            };

        public static SiteTouristiqueInitiateFlexPayResponseDto ToInitiateFlexPayResponse(
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            string orderNumber,
            string? paymentUrl,
            bool flexPayAccepted,
            string message,
            bool alreadyInitiated) =>
            new()
            {
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                Payment = ToPaymentResponse(payment),
                OrderNumber = orderNumber,
                ReservationExpiresAtUtc = reservation.ExpiresAtUtc
                    ?? throw new InvalidOperationException("Une réservation HOLD doit avoir ExpiresAtUtc."),
                PaymentUrl = paymentUrl,
                MontantFlexPay = payment.Montant,
                CodeDevisePaiement = payment.CodeDevise,
                MontantTarif = payment.MontantTarif,
                CodeDeviseTarif = payment.CodeDeviseTarif,
                TauxApplique = payment.TauxVersDevisePaiement,
                FlexPayAccepted = flexPayAccepted,
                Message = message,
                AlreadyInitiated = alreadyInitiated
            };

        public static SiteTouristiqueReservationListItemDto ToListItemDto(SiteTouristiqueReservation reservation) =>
            new()
            {
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                IdSociete = reservation.IdSociete,
                IdSiteTouristiqueJournee = reservation.IdSiteTouristiqueJournee,
                IdSite = reservation.IdSite,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                IdUtilisateur = reservation.IdUtilisateur,
                IdClient = reservation.IdClient,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc,
                MontantSousTotal = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                DateCreation = reservation.DateCreation,
                DateModification = reservation.DateModification
            };

        public static SiteTouristiqueReservationResponseDto ToResponseDto(SiteTouristiqueReservation reservation)
        {
            var tickets = reservation.Lines
                .SelectMany(l => l.Tickets)
                .OrderBy(t => t.IdSiteTouristiqueTicket)
                .Select(ToTicketResponse)
                .ToList();

            return new SiteTouristiqueReservationResponseDto
            {
                IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                IdSociete = reservation.IdSociete,
                IdSiteTouristiqueJournee = reservation.IdSiteTouristiqueJournee,
                IdSite = reservation.IdSite,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                IdUtilisateur = reservation.IdUtilisateur,
                IdClient = reservation.IdClient,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc,
                MontantSousTotal = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                DateCreation = reservation.DateCreation,
                DateModification = reservation.DateModification,
                Lines = reservation.Lines
                    .OrderBy(l => l.IdSiteTouristiqueReservationLine)
                    .Select(ToLineResponse)
                    .ToList(),
                Tickets = tickets,
                Payments = reservation.Payments
                    .OrderBy(p => p.IdSiteTouristiquePayment)
                    .Select(ToPaymentResponse)
                    .ToList()
            };
        }

        public static SiteTouristiqueReservationLineResponseDto ToLineResponse(SiteTouristiqueReservationLine line) =>
            new()
            {
                IdSiteTouristiqueReservationLine = line.IdSiteTouristiqueReservationLine,
                LineType = line.LineType.ToString(),
                Quantite = line.Quantite,
                PrixUnitaire = line.PrixUnitaire,
                CodeDevise = line.CodeDevise,
                IdSiteTouristiqueClassQuota = line.IdSiteTouristiqueClassQuota
            };

        public static SiteTouristiquePaymentResponseDto ToPaymentResponse(SiteTouristiquePayment payment) =>
            new()
            {
                IdSiteTouristiquePayment = payment.IdSiteTouristiquePayment,
                IdSite = payment.IdSite,
                ReferencePaiement = payment.ReferencePaiement,
                Provider = payment.Provider,
                ProviderTxRef = payment.ProviderTxRef,
                Status = payment.Status.ToString(),
                Montant = payment.Montant,
                CodeDevise = payment.CodeDevise,
                MontantTarif = payment.MontantTarif,
                CodeDeviseTarif = payment.CodeDeviseTarif,
                TauxVersDevisePaiement = payment.TauxVersDevisePaiement,
                DateCreation = payment.DateCreation
            };

        public static SiteTouristiqueTicketResponseDto ToTicketResponse(SiteTouristiqueTicket ticket) =>
            new()
            {
                IdSiteTouristiqueTicket = ticket.IdSiteTouristiqueTicket,
                IdSiteTouristiqueReservationLine = ticket.IdSiteTouristiqueReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc
            };
    }
}
