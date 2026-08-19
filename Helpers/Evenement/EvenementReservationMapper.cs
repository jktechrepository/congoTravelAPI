using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementReservationMapper
    {
        public static EvenementHoldResponseDto ToHoldResponse(EvenementReservation reservation) =>
            new()
            {
                IdEvenementReservation = reservation.IdEvenementReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc
                    ?? throw new InvalidOperationException("Une réservation HOLD doit avoir ExpiresAtUtc."),
                AmountPreview = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise
            };

        public static EvenementConfirmPaymentResponseDto ToConfirmPaymentResponse(
            EvenementReservation reservation,
            EvenementPayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                Reservation = ToResponseDto(reservation),
                Payment = ToPaymentResponse(payment),
                AlreadyConfirmed = alreadyConfirmed
            };

        public static EvenementInitiateFlexPayResponseDto ToInitiateFlexPayResponse(
            EvenementReservation reservation,
            EvenementPayment payment,
            string orderNumber,
            string? paymentUrl,
            bool flexPayAccepted,
            string message,
            bool alreadyInitiated) =>
            new()
            {
                IdEvenementReservation = reservation.IdEvenementReservation,
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

        public static EvenementReservationListItemDto ToListItemDto(EvenementReservation reservation) =>
            new()
            {
                IdEvenementReservation = reservation.IdEvenementReservation,
                IdSociete = reservation.IdSociete,
                IdEvenementSession = reservation.IdEvenementSession,
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

        public static EvenementReservationResponseDto ToResponseDto(EvenementReservation reservation)
        {
            var tickets = reservation.Lines
                .SelectMany(l => l.Tickets)
                .OrderBy(t => t.IdEvenementTicket)
                .Select(t => ToTicketResponse(t, reservation.Session))
                .ToList();

            return new EvenementReservationResponseDto
            {
                IdEvenementReservation = reservation.IdEvenementReservation,
                IdSociete = reservation.IdSociete,
                IdEvenementSession = reservation.IdEvenementSession,
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
                    .OrderBy(l => l.IdEvenementReservationLine)
                    .Select(ToLineResponse)
                    .ToList(),
                Tickets = tickets,
                Payments = reservation.Payments
                    .OrderBy(p => p.IdEvenementPayment)
                    .Select(ToPaymentResponse)
                    .ToList()
            };
        }

        public static EvenementReservationLineResponseDto ToLineResponse(EvenementReservationLine line) =>
            new()
            {
                IdEvenementReservationLine = line.IdEvenementReservationLine,
                LineType = line.LineType.ToString(),
                Quantite = line.Quantite,
                PrixUnitaire = line.PrixUnitaire,
                CodeDevise = line.CodeDevise,
                IdEvenementSessionSeat = line.IdEvenementSessionSeat,
                IdEvenementSessionClassQuota = line.IdEvenementSessionClassQuota
            };

        public static EvenementPaymentResponseDto ToPaymentResponse(EvenementPayment payment) =>
            new()
            {
                IdEvenementPayment = payment.IdEvenementPayment,
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

        public static EvenementTicketResponseDto ToTicketResponse(
            EvenementTicket ticket,
            EvenementSession? session = null) =>
            new()
            {
                IdEvenementTicket = ticket.IdEvenementTicket,
                IdEvenementReservationLine = ticket.IdEvenementReservationLine,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status.ToString(),
                LogoOrganisateur = session?.LogoOrganisateur ?? ticket.ReservationLine?.Reservation?.Session?.LogoOrganisateur,
                IssuedAtUtc = ticket.IssuedAtUtc,
                UsedAtUtc = ticket.UsedAtUtc
            };
    }
}
