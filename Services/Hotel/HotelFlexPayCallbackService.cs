using System.Globalization;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public interface IHotelFlexPayCallbackService
    {
        Task<HotelFlexPayCallbackProcessResultDto> ProcessCallbackAsync(FlexPayCallbackDto callback, CancellationToken cancellationToken = default);
        Task<HotelFlexPayVerifierResultDto> VerifyAndFinalizeAsync(string orderNumber, int idSociete, CancellationToken cancellationToken = default);
        Task<HotelFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(string orderNumber, string message, CancellationToken cancellationToken = default);
    }

    public class HotelFlexPayCallbackService : IHotelFlexPayCallbackService
    {
        private const decimal AmountTolerance = 0.05m;
        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPay;
        private readonly IHotelCommandeFlexPayService _commandes;
        private readonly IFlexPayRealtimeNotifier _notifier;
        private readonly ILogger<HotelFlexPayCallbackService> _logger;

        public HotelFlexPayCallbackService(CongoTravelDbContext context, IFlexPayService flexPay,
            IHotelCommandeFlexPayService commandes, IFlexPayRealtimeNotifier notifier,
            ILogger<HotelFlexPayCallbackService> logger)
        {
            _context = context; _flexPay = flexPay; _commandes = commandes;
            _notifier = notifier; _logger = logger;
        }

        public async Task<HotelFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber, int idSociete, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("orderNumber requis.", nameof(orderNumber));
            var payment = await FindPaymentAsync(orderNumber, cancellationToken)
                ?? throw new KeyNotFoundException($"Paiement FlexPay hôtel {orderNumber} introuvable.");
            if (payment.IdHotelReservation is > 0)
            {
                var reservation = await LoadReservationAsync(payment.IdHotelReservation.Value, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException($"Paiement FlexPay hôtel {orderNumber} introuvable pour la société {idSociete}.");
                if (payment.Status == HotelPaymentStatus.SUCCEEDED)
                    return ConfirmResult(reservation, payment, true);
            }
            var commande = payment.IdHotelCommandeEnAttente.HasValue
                ? await _context.HotelCommandesEnAttente.AsNoTracking().FirstOrDefaultAsync(
                    c => c.IdHotelCommandeEnAttente == payment.IdHotelCommandeEnAttente && c.IdSociete == idSociete, cancellationToken)
                : null;
            if (commande == null)
                throw new KeyNotFoundException($"Paiement FlexPay hôtel {orderNumber} introuvable pour la société {idSociete}.");
            if (commande.DateExpiration < DateTime.UtcNow)
                return new() { StatusOnly = await AbandonPendingPaymentAsync(orderNumber, HoldExpiredMessage, cancellationToken) };
            var info = await ResolveInfoAsync(idSociete, cancellationToken);
            var check = await _flexPay.VerifierStatutTransactionAsync(info.ApiToken, orderNumber.Trim(), cancellationToken);
            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
                return new()
                {
                    StatusOnly = new HotelFlexPayCallbackProcessResultDto
                    {
                        Success = true, PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdHotelPayment = payment.IdHotelPayment
                    }
                };
            var callback = FlexPayVerifyCallbackHelper.BuildSyntheticCallback(
                check, orderNumber, payment.Montant, payment.CodeDevise,
                FlexPayStatusHelper.IsSuccess(status) ? "0" : "1");
            var result = await ProcessCallbackAsync(callback, cancellationToken);
            if (result.Success && result.IdHotelReservation is > 0)
            {
                var reservation = await LoadReservationAsync(result.IdHotelReservation.Value, idSociete, cancellationToken);
                var finalPayment = await FindPaymentAsync(orderNumber, cancellationToken);
                if (reservation != null && finalPayment != null && finalPayment.Status == HotelPaymentStatus.SUCCEEDED)
                    return ConfirmResult(reservation, finalPayment, result.AlreadyProcessed);
            }
            return new() { StatusOnly = result };
        }

        public async Task<HotelFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callback.OrderNumber)) return Failure("OrderNumber requis.");
                var payment = await FindPaymentAsync(callback.OrderNumber, cancellationToken);
                if (payment == null) return Failure("Paiement hôtel introuvable pour ce orderNumber.");
                if (payment.Status == HotelPaymentStatus.SUCCEEDED && payment.IdHotelReservation is > 0)
                    return new()
                    {
                        Success = true, AlreadyProcessed = true, Message = "Déjà finalisé (idempotence).",
                        IdHotelReservation = payment.IdHotelReservation, IdHotelPayment = payment.IdHotelPayment
                    };
                var commande = payment.IdHotelCommandeEnAttente.HasValue
                    ? await _context.HotelCommandesEnAttente.FirstOrDefaultAsync(
                        c => c.IdHotelCommandeEnAttente == payment.IdHotelCommandeEnAttente, cancellationToken)
                    : null;
                if (commande == null)
                    return payment.Status == HotelPaymentStatus.FAILED
                        ? StatusOnly(payment.IdHotelPayment, "Paiement non confirmé.", true)
                        : Failure("Commande hôtel associée au paiement introuvable.");
                if (!string.Equals(callback.Code, "0", StringComparison.Ordinal))
                    return await FailAsync(commande, payment, callback.OrderNumber.Trim(), "Paiement refusé par FlexPay.", cancellationToken);
                ValidateAmount(callback, payment);
                FlexPayCurrencyPolicy.EnsureCallbackCurrencyMatchesExpected(callback.Currency, payment.CodeDevise, "Callback FlexPay hôtel");
                if (commande.DateExpiration < DateTime.UtcNow)
                    return await FailAsync(commande, payment, callback.OrderNumber.Trim(), HoldExpiredMessage, cancellationToken);
                var reservation = await _commandes.FinalizeCommandeSuccessAsync(commande, payment, cancellationToken);
                await NotifyConfirmedAsync(reservation.IdUtilisateur, callback.OrderNumber, reservation.IdHotelReservation, payment.IdHotelPayment, cancellationToken);
                return new()
                {
                    Success = true, Message = "Réservation hôtel confirmée après callback FlexPay.",
                    IdHotelReservation = reservation.IdHotelReservation, IdHotelPayment = payment.IdHotelPayment
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur traitement callback FlexPay hôtel");
                return Failure(ex.Message);
            }
        }

        public async Task<HotelFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
            string orderNumber, string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("orderNumber requis.", nameof(orderNumber));
            var payment = await FindPaymentAsync(orderNumber, cancellationToken);
            if (payment == null) return Failure("Paiement hôtel introuvable pour ce orderNumber.");
            if (payment.Status == HotelPaymentStatus.SUCCEEDED)
                return new() { Success = true, AlreadyProcessed = true, Message = "Paiement déjà confirmé.", IdHotelReservation = payment.IdHotelReservation, IdHotelPayment = payment.IdHotelPayment };
            var commande = payment.IdHotelCommandeEnAttente.HasValue
                ? await _context.HotelCommandesEnAttente.FirstOrDefaultAsync(
                    c => c.IdHotelCommandeEnAttente == payment.IdHotelCommandeEnAttente, cancellationToken)
                : null;
            if (commande == null) return StatusOnly(payment.IdHotelPayment, message, true);
            return await FailAsync(commande, payment, orderNumber.Trim(), message, cancellationToken);
        }

        private async Task<HotelFlexPayCallbackProcessResultDto> FailAsync(
            HotelCommandeEnAttente commande, HotelPayment payment, string orderNumber,
            string message, CancellationToken cancellationToken)
        {
            var user = commande.IdUtilisateur;
            await _commandes.FailCommandeAsync(commande, payment, cancellationToken);
            if (user is > 0) await NotifyFailedAsync(user.Value, orderNumber, message, cancellationToken);
            return StatusOnly(payment.IdHotelPayment, message);
        }

        private async Task NotifyConfirmedAsync(int? userId, string? order, int reservationId, int paymentId, CancellationToken ct)
        {
            if (userId is null or <= 0 || string.IsNullOrWhiteSpace(order)) return;
            try
            {
                if (_notifier is FlexPayRealtimeNotifier concrete)
                    await concrete.NotifyPaymentConfirmedForDomainAsync(userId.Value, order, reservationId, paymentId, "hotel", ct);
                else
                    await _notifier.NotifyPaymentConfirmedAsync(userId.Value, order, reservationId, paymentId, ct);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SignalR hôtel confirmé non envoyé pour {Order}", order); }
        }

        private async Task NotifyFailedAsync(int userId, string order, string message, CancellationToken ct)
        {
            try
            {
                if (_notifier is FlexPayRealtimeNotifier concrete)
                    await concrete.NotifyPaymentFailedForDomainAsync(userId, order, message, "hotel", ct);
                else
                    await _notifier.NotifyPaymentFailedAsync(userId, order, message, ct);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "SignalR hôtel échoué non envoyé pour {Order}", order); }
        }

        private Task<HotelPayment?> FindPaymentAsync(string order, CancellationToken ct) =>
            _context.HotelPayments.AsNoTracking().FirstOrDefaultAsync(
                p => p.Provider == HotelFlexPayConstants.Provider && p.ProviderTxRef == order.Trim(), ct);

        private Task<HotelReservation?> LoadReservationAsync(int id, int company, CancellationToken ct) =>
            _context.HotelReservations.AsNoTracking().Include(r => r.Lines).Include(r => r.Payments)
                .FirstOrDefaultAsync(r => r.IdHotelReservation == id && r.IdSociete == company, ct);

        private async Task<InfoPaiementSociete> ResolveInfoAsync(int company, CancellationToken ct) =>
            await (from i in _context.InfoPaiementsSociete.AsNoTracking()
                   join s in _context.Sites.AsNoTracking() on i.IdSite equals s.IdSite
                   where i.IdSociete == company && i.Statut && s.Statut
                   orderby s.IsSitePrincipal descending, i.IdSite
                   select i).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Aucune configuration FlexPay active pour la société {company}.");

        private static HotelFlexPayVerifierResultDto ConfirmResult(HotelReservation reservation, HotelPayment payment, bool already) =>
            new()
            {
                ConfirmPayment = new HotelConfirmPaymentResponseDto
                {
                    Reservation = HotelReservationMapper.ToResponse(reservation),
                    Payment = HotelReservationMapper.ToPayment(payment),
                    AlreadyConfirmed = already
                }
            };

        private static void ValidateAmount(FlexPayCallbackDto callback, HotelPayment payment)
        {
            if ((!decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                 && !decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
                || Math.Abs(amount - payment.Montant) <= AmountTolerance) return;
            throw new InvalidOperationException($"Montant callback ({amount}) différent du montant attendu ({payment.Montant}).");
        }

        private static HotelFlexPayCallbackProcessResultDto StatusOnly(int paymentId, string message, bool already = false) =>
            new() { Success = true, AlreadyProcessed = already, PaymentPending = false, Message = message, IdHotelPayment = paymentId };
        private static HotelFlexPayCallbackProcessResultDto Failure(string message) =>
            new() { Success = false, PaymentPending = false, Message = message };

        public const string HoldExpiredMessage = "Hold expiré. Le paiement n’a pas été confirmé à temps.";
        public const string CancelMessage = "Paiement annulé.";
        public const string DeclineMessage = "Paiement refusé.";
    }
}
