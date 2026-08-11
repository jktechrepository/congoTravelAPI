using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;

namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantReservationMapper
    {
        public static RestaurantHoldResponseDto ToHoldResponse(RestaurantReservation reservation) =>
            new()
            {
                IdRestaurantReservation = reservation.IdRestaurantReservation,
                ReferenceReservation = reservation.ReferenceReservation,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc
                    ?? throw new InvalidOperationException("Une réservation HOLD doit avoir ExpiresAtUtc."),
                AmountPreview = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                NombreCouverts = reservation.NombreCouverts
            };

        public static RestaurantConfirmPaymentResponseDto ToConfirmPaymentResponse(
            RestaurantReservation reservation,
            RestaurantPayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                Reservation = ToResponseDto(reservation),
                Payment = ToPaymentResponse(payment),
                AlreadyConfirmed = alreadyConfirmed
            };

        public static RestaurantInitiateFlexPayResponseDto ToInitiateFlexPayResponse(
            RestaurantReservation reservation,
            RestaurantPayment payment,
            string orderNumber,
            string? paymentUrl,
            bool flexPayAccepted,
            string message,
            bool alreadyInitiated) =>
            new()
            {
                IdRestaurantReservation = reservation.IdRestaurantReservation,
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

        public static RestaurantReservationListItemDto ToListItemDto(RestaurantReservation reservation) =>
            new()
            {
                IdRestaurantReservation = reservation.IdRestaurantReservation,
                IdSociete = reservation.IdSociete,
                IdRestaurant = reservation.IdRestaurant,
                IdRestaurantCreneau = reservation.IdRestaurantCreneau,
                IdSite = reservation.IdSite,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc,
                MontantSousTotal = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                NombreCouverts = reservation.NombreCouverts,
                DateCreation = reservation.DateCreation,
                DateModification = reservation.DateModification
            };

        public static RestaurantReservationResponseDto ToResponseDto(RestaurantReservation reservation) =>
            new()
            {
                IdRestaurantReservation = reservation.IdRestaurantReservation,
                IdSociete = reservation.IdSociete,
                IdRestaurant = reservation.IdRestaurant,
                IdRestaurantCreneau = reservation.IdRestaurantCreneau,
                IdSite = reservation.IdSite,
                ReferenceReservation = reservation.ReferenceReservation,
                CustomerRef = reservation.CustomerRef,
                Status = reservation.Status.ToString(),
                ExpiresAtUtc = reservation.ExpiresAtUtc,
                MontantSousTotal = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                NombreCouverts = reservation.NombreCouverts,
                DateCreation = reservation.DateCreation,
                DateModification = reservation.DateModification,
                Lines = reservation.Lines
                    .OrderBy(l => l.IdRestaurantReservationLine)
                    .Select(ToLineResponse)
                    .ToList(),
                Payments = reservation.Payments
                    .OrderBy(p => p.IdRestaurantPayment)
                    .Select(ToPaymentResponse)
                    .ToList()
            };

        public static RestaurantReservationLineResponseDto ToLineResponse(RestaurantReservationLine line) =>
            new()
            {
                IdRestaurantReservationLine = line.IdRestaurantReservationLine,
                LineType = line.LineType.ToString(),
                Quantite = line.Quantite,
                PrixUnitaire = line.PrixUnitaire,
                MontantLigne = line.MontantLigne,
                CodeDevise = line.CodeDevise,
                IdRestaurantCreneauGlobalQuota = line.IdRestaurantCreneauGlobalQuota,
                IdRestaurantCreneauZoneQuota = line.IdRestaurantCreneauZoneQuota
            };

        public static RestaurantPaymentResponseDto ToPaymentResponse(RestaurantPayment payment) =>
            new()
            {
                IdRestaurantPayment = payment.IdRestaurantPayment,
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
    }
}
