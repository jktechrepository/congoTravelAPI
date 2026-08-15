using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantFlexPayCallbackService : IRestaurantFlexPayCallbackService
    {
        private const decimal MontantTolerance = 0.05m;
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";
        private const string MessagePaiementAnnule = "Paiement annulé.";
        private const string MessagePaiementRefuse = "Paiement refusé.";
        private const string MessagePaiementNonConfirme = "Paiement non confirmé.";

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IRestaurantReservationConfirmationService _confirmationService;
        private readonly IRestaurantReservationService _reservationService;
        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<RestaurantFlexPayCallbackService> _logger;

        public RestaurantFlexPayCallbackService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IRestaurantReservationConfirmationService confirmationService,
            IRestaurantReservationService reservationService,
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<RestaurantFlexPayCallbackService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _confirmationService = confirmationService;
            _reservationService = reservationService;
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task<RestaurantFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var payment = await FindPaymentAsync(orderNumber, cancellationToken)
                ?? throw new InvalidOperationException($"Paiement FlexPay restaurant {orderNumber} introuvable.");

            var reservation = await LoadReservationGraphAsync(
                payment.IdRestaurantReservation,
                idSociete,
                cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    $"Paiement FlexPay restaurant {orderNumber} introuvable pour la société {idSociete}.");
            }

            var trackedPayment = reservation.Payments
                .First(p => p.IdRestaurantPayment == payment.IdRestaurantPayment);

            if (reservation.Status == RestaurantReservationStatus.CONFIRMED
                && trackedPayment.Status == RestaurantPaymentStatus.SUCCEEDED)
            {
                return WrapConfirmResult(reservation, trackedPayment, alreadyConfirmed: true);
            }

            var terminal = await TryFinalizeTerminalLocalStateAsync(
                reservation,
                trackedPayment,
                orderNumber.Trim(),
                cancellationToken);
            if (terminal != null)
                return new RestaurantFlexPayVerifierResultDto { StatusOnly = terminal };

            var infoPaiement = await ResolveInfoPaiementForSocieteAsync(idSociete, cancellationToken);
            var check = await _flexPayService.VerifierStatutTransactionAsync(
                infoPaiement.ApiToken,
                orderNumber.Trim(),
                cancellationToken);

            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
                reservation = await LoadReservationGraphAsync(
                    payment.IdRestaurantReservation,
                    idSociete,
                    cancellationToken)
                    ?? reservation;
                trackedPayment = reservation.Payments
                    .First(p => p.IdRestaurantPayment == payment.IdRestaurantPayment);

                terminal = await TryFinalizeTerminalLocalStateAsync(
                    reservation,
                    trackedPayment,
                    orderNumber.Trim(),
                    cancellationToken);
                if (terminal != null)
                    return new RestaurantFlexPayVerifierResultDto { StatusOnly = terminal };

                if (reservation.Status != RestaurantReservationStatus.HOLD
                    || trackedPayment.Status != RestaurantPaymentStatus.PENDING)
                {
                    return new RestaurantFlexPayVerifierResultDto
                    {
                        StatusOnly = StatusOnlyNotPending(
                            reservation.IdRestaurantReservation,
                            trackedPayment.IdRestaurantPayment,
                            MessagePaiementNonConfirme)
                    };
                }

                _logger.LogInformation(
                    "FlexPay verifier restaurant {OrderNumber} : statut {Status} — paiement toujours en attente.",
                    orderNumber,
                    status ?? "(null)");

                return new RestaurantFlexPayVerifierResultDto
                {
                    StatusOnly = new RestaurantFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdRestaurantReservation = reservation.IdRestaurantReservation,
                        IdRestaurantPayment = trackedPayment.IdRestaurantPayment
                    }
                };
            }

            var callbackCode = FlexPayStatusHelper.IsSuccess(status) ? "0" : "1";
            var callback = new FlexPayCallbackDto
            {
                Code = callbackCode,
                OrderNumber = orderNumber.Trim(),
                Amount = trackedPayment.Montant.ToString(CultureInfo.InvariantCulture),
                Currency = trackedPayment.CodeDevise
            };

            var processResult = await ProcessCallbackAsync(callback, cancellationToken);
            return await WrapVerifierResultAsync(
                processResult,
                idSociete,
                orderNumber,
                cancellationToken);
        }

        public async Task<RestaurantFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callback.OrderNumber))
                {
                    return Failure("OrderNumber requis.");
                }

                var payment = await FindPaymentAsync(callback.OrderNumber, cancellationToken);
                if (payment == null)
                {
                    _logger.LogWarning(
                        "Callback FlexPay restaurant — paiement introuvable pour OrderNumber={OrderNumber}",
                        callback.OrderNumber);
                    return Failure("Paiement restaurant introuvable pour ce orderNumber.");
                }

                var reservation = await _context.RestaurantReservations
                    .Include(r => r.Lines)
                        .ThenInclude(l => l.Tickets)
                    .Include(r => r.Payments)
                    .FirstOrDefaultAsync(
                        r => r.IdRestaurantReservation == payment.IdRestaurantReservation,
                        cancellationToken);

                if (reservation == null)
                {
                    return Failure("Réservation restaurant associée au paiement introuvable.");
                }

                if (reservation.Status == RestaurantReservationStatus.CONFIRMED
                    && payment.Status == RestaurantPaymentStatus.SUCCEEDED)
                {
                    return new RestaurantFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = "Déjà finalisé (idempotence).",
                        IdRestaurantReservation = reservation.IdRestaurantReservation,
                        IdRestaurantPayment = payment.IdRestaurantPayment
                    };
                }

                if (!string.Equals(callback.Code, "0", StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "Callback FlexPay restaurant refusé — Order={OrderNumber}, Code={Code}",
                        callback.OrderNumber,
                        callback.Code);

                    return await FailPendingAndReleaseHoldAsync(
                        reservation,
                        payment,
                        callback.OrderNumber!.Trim(),
                        "Paiement refusé par FlexPay.",
                        cancellationToken);
                }

                ValidateCallbackAmount(callback, payment);

                _logger.LogInformation(
                    "Callback FlexPay restaurant — Order={OrderNumber}, Amount={Amount}, Currency={Currency}, Attendu={Montant} {DevisePaiement} (tarif {MontantTarif} {DeviseTarif})",
                    callback.OrderNumber,
                    callback.Amount,
                    callback.Currency,
                    payment.Montant,
                    payment.CodeDevise,
                    payment.MontantTarif,
                    payment.CodeDeviseTarif);

                var dbStrategy = _context.Database.CreateExecutionStrategy();
                return await dbStrategy.ExecuteAsync(async () =>
                {
                    IDbContextTransaction? transaction = null;
                    if (_context.Database.IsRelational())
                        transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                    try
                    {
                        var trackedPayment = await _context.RestaurantPayments
                            .FirstAsync(p => p.IdRestaurantPayment == payment.IdRestaurantPayment, cancellationToken);

                        var trackedReservation = await _context.RestaurantReservations
                            .Include(r => r.Lines)
                                .ThenInclude(l => l.Tickets)
                            .FirstAsync(
                                r => r.IdRestaurantReservation == reservation.IdRestaurantReservation,
                                cancellationToken);

                        if (trackedReservation.Status == RestaurantReservationStatus.CONFIRMED
                            && trackedPayment.Status == RestaurantPaymentStatus.SUCCEEDED)
                        {
                            if (transaction != null)
                                await transaction.CommitAsync(cancellationToken);

                            return new RestaurantFlexPayCallbackProcessResultDto
                            {
                                Success = true,
                                AlreadyProcessed = true,
                                Message = "Déjà finalisé (idempotence).",
                                IdRestaurantReservation = trackedReservation.IdRestaurantReservation,
                                IdRestaurantPayment = trackedPayment.IdRestaurantPayment
                            };
                        }

                        trackedPayment.ProviderTxRef = callback.OrderNumber?.Trim() ?? trackedPayment.ProviderTxRef;

                        await _confirmationService.ConfirmHoldAndEmitTicketsAsync(
                            trackedReservation,
                            trackedPayment,
                            trackedReservation.IdSociete,
                            cancellationToken);

                        await _context.SaveChangesAsync(cancellationToken);

                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        _logger.LogInformation(
                            "Callback FlexPay restaurant OK — IdReservation={IdReservation}, IdPayment={IdPayment}, Order={OrderNumber}",
                            trackedReservation.IdRestaurantReservation,
                            trackedPayment.IdRestaurantPayment,
                            callback.OrderNumber);

                        await TryNotifyPaymentConfirmedAsync(
                            trackedReservation.IdUtilisateur,
                            callback.OrderNumber,
                            trackedReservation.IdRestaurantReservation,
                            trackedPayment.IdRestaurantPayment,
                            cancellationToken);

                        return new RestaurantFlexPayCallbackProcessResultDto
                        {
                            Success = true,
                            Message = "Réservation restaurant confirmée après callback FlexPay.",
                            IdRestaurantReservation = trackedReservation.IdRestaurantReservation,
                            IdRestaurantPayment = trackedPayment.IdRestaurantPayment
                        };
                    }
                    catch (RestaurantHoldConflictException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Conflit inventaire callback FlexPay restaurant — Order={OrderNumber}",
                            callback.OrderNumber);

                        return Failure(ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Callback FlexPay restaurant refusé — Order={OrderNumber}",
                            callback.OrderNumber);

                        await FailPendingAndReleaseHoldAsync(
                            reservation,
                            payment,
                            callback.OrderNumber!.Trim(),
                            ex.Message,
                            cancellationToken);

                        return Failure(ex.Message);
                    }
                    catch
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                    finally
                    {
                        if (transaction != null)
                            await transaction.DisposeAsync();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur traitement callback FlexPay restaurant");
                return Failure(ex.Message);
            }
        }

        public async Task<RestaurantFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var normalized = orderNumber.Trim();
            var payment = await FindPaymentAsync(normalized, cancellationToken);
            if (payment == null)
            {
                return Failure("Paiement restaurant introuvable pour ce orderNumber.");
            }

            var reservation = await _context.RestaurantReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == payment.IdRestaurantReservation,
                    cancellationToken);

            if (reservation == null)
            {
                return Failure("Réservation restaurant associée au paiement introuvable.");
            }

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                normalized,
                message,
                cancellationToken);
        }

        private async Task<RestaurantFlexPayCallbackProcessResultDto?> TryFinalizeTerminalLocalStateAsync(
            RestaurantReservation reservation,
            RestaurantPayment payment,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (payment.Status is RestaurantPaymentStatus.FAILED or RestaurantPaymentStatus.REFUNDED)
            {
                if (reservation.Status == RestaurantReservationStatus.HOLD)
                {
                    return await FailPendingAndReleaseHoldAsync(
                        reservation,
                        payment,
                        orderNumber,
                        MessagePaiementNonConfirme,
                        cancellationToken);
                }

                return StatusOnlyNotPending(
                    reservation.IdRestaurantReservation,
                    payment.IdRestaurantPayment,
                    MessagePaiementNonConfirme,
                    alreadyProcessed: true);
            }

            var holdTimedOut = reservation.Status == RestaurantReservationStatus.HOLD
                && reservation.ExpiresAtUtc != null
                && reservation.ExpiresAtUtc < DateTime.UtcNow;

            var reservationReleased = reservation.Status is RestaurantReservationStatus.EXPIRED
                or RestaurantReservationStatus.CANCELLED;

            if (!holdTimedOut && !reservationReleased)
                return null;

            if (payment.Status == RestaurantPaymentStatus.SUCCEEDED)
                return null;

            var message = holdTimedOut || reservation.Status == RestaurantReservationStatus.EXPIRED
                ? MessageHoldExpire
                : MessagePaiementAnnule;

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                orderNumber,
                message,
                cancellationToken);
        }

        private async Task<RestaurantFlexPayCallbackProcessResultDto> FailPendingAndReleaseHoldAsync(
            RestaurantReservation reservationSnapshot,
            RestaurantPayment paymentSnapshot,
            string orderNumber,
            string message,
            CancellationToken cancellationToken)
        {
            var trackedPayment = await _context.RestaurantPayments
                .FirstAsync(p => p.IdRestaurantPayment == paymentSnapshot.IdRestaurantPayment, cancellationToken);

            if (trackedPayment.Status == RestaurantPaymentStatus.SUCCEEDED)
            {
                return new RestaurantFlexPayCallbackProcessResultDto
                {
                    Success = true,
                    AlreadyProcessed = true,
                    PaymentPending = false,
                    Message = "Paiement déjà confirmé.",
                    IdRestaurantReservation = reservationSnapshot.IdRestaurantReservation,
                    IdRestaurantPayment = trackedPayment.IdRestaurantPayment
                };
            }

            var alreadyFailed = trackedPayment.Status is RestaurantPaymentStatus.FAILED
                or RestaurantPaymentStatus.REFUNDED;

            if (!alreadyFailed)
            {
                trackedPayment.Status = RestaurantPaymentStatus.FAILED;
                trackedPayment.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var release = await TryReleaseHoldAsync(
                reservationSnapshot.IdRestaurantReservation,
                reservationSnapshot.IdSociete,
                orderNumber,
                cancellationToken);

            if (!release.Released)
            {
                return new RestaurantFlexPayCallbackProcessResultDto
                {
                    Success = false,
                    PaymentPending = false,
                    Message = release.ErrorMessage
                        ?? "Paiement marqué échoué mais le hold n’a pas pu être libéré.",
                    IdRestaurantReservation = reservationSnapshot.IdRestaurantReservation,
                    IdRestaurantPayment = trackedPayment.IdRestaurantPayment
                };
            }

            if (!alreadyFailed)
            {
                await TryNotifyPaymentFailedAsync(
                    reservationSnapshot.IdUtilisateur,
                    orderNumber,
                    message,
                    cancellationToken);
            }

            _logger.LogInformation(
                "FlexPay restaurant abandonné — Order={OrderNumber}, Payment={IdPayment}, Message={Message}",
                orderNumber,
                trackedPayment.IdRestaurantPayment,
                message);

            return StatusOnlyNotPending(
                reservationSnapshot.IdRestaurantReservation,
                trackedPayment.IdRestaurantPayment,
                message,
                alreadyProcessed: alreadyFailed);
        }

        /// <summary>
        /// Détache toute réservation trackée (souvent sans Lines) puis appelle CancelAsync
        /// pour forcer un reload avec graphe complet sur le DbContext partagé.
        /// </summary>
        private async Task<(bool Released, string? ErrorMessage)> TryReleaseHoldAsync(
            int idRestaurantReservation,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            DetachTrackedReservation(idRestaurantReservation);

            var status = await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.IdRestaurantReservation == idRestaurantReservation)
                .Select(r => r.Status)
                .FirstAsync(cancellationToken);

            if (status != RestaurantReservationStatus.HOLD)
                return (true, null);

            try
            {
                DetachTrackedReservation(idRestaurantReservation);
                await _reservationService.CancelAsync(
                    idRestaurantReservation,
                    idSociete,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Libération hold restaurant après abandon FlexPay — Order={OrderNumber}, Reservation={IdReservation}",
                    orderNumber,
                    idRestaurantReservation);
            }

            DetachTrackedReservation(idRestaurantReservation);
            status = await _context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.IdRestaurantReservation == idRestaurantReservation)
                .Select(r => r.Status)
                .FirstAsync(cancellationToken);

            if (status == RestaurantReservationStatus.HOLD)
            {
                return (
                    false,
                    "Impossible de libérer le hold de la réservation. Réessayez l’abandon.");
            }

            return (true, null);
        }

        private void DetachTrackedReservation(int idRestaurantReservation)
        {
            var tracked = _context.ChangeTracker
                .Entries<RestaurantReservation>()
                .Where(e => e.Entity.IdRestaurantReservation == idRestaurantReservation)
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;
        }

        private static RestaurantFlexPayCallbackProcessResultDto StatusOnlyNotPending(
            int idRestaurantReservation,
            int idRestaurantPayment,
            string message,
            bool alreadyProcessed = false) =>
            new()
            {
                Success = true,
                AlreadyProcessed = alreadyProcessed,
                PaymentPending = false,
                Message = message,
                IdRestaurantReservation = idRestaurantReservation,
                IdRestaurantPayment = idRestaurantPayment
            };

        private async Task<RestaurantFlexPayVerifierResultDto> WrapVerifierResultAsync(
            RestaurantFlexPayCallbackProcessResultDto processResult,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (processResult.Success
                && processResult.IdRestaurantReservation.HasValue
                && !processResult.PaymentPending)
            {
                var reservation = await LoadReservationGraphAsync(
                    processResult.IdRestaurantReservation.Value,
                    idSociete,
                    cancellationToken);

                if (reservation?.Status == RestaurantReservationStatus.CONFIRMED)
                {
                    var payment = reservation.Payments
                        .OrderByDescending(p => p.DateCreation)
                        .First(p => string.Equals(p.ProviderTxRef, orderNumber.Trim(), StringComparison.Ordinal)
                                    || p.Status == RestaurantPaymentStatus.SUCCEEDED);

                    return WrapConfirmResult(
                        reservation,
                        payment,
                        processResult.AlreadyProcessed);
                }
            }

            return new RestaurantFlexPayVerifierResultDto { StatusOnly = processResult };
        }

        private static RestaurantFlexPayVerifierResultDto WrapConfirmResult(
            RestaurantReservation reservation,
            RestaurantPayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                ConfirmPayment = RestaurantReservationMapper.ToConfirmPaymentResponse(
                    reservation,
                    payment,
                    alreadyConfirmed)
            };

        private async Task<RestaurantReservation?> LoadReservationGraphAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.RestaurantReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private async Task<InfoPaiementSociete> ResolveInfoPaiementForSocieteAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            var info = await (
                from i in _context.InfoPaiementsSociete.AsNoTracking()
                join s in _context.Sites.AsNoTracking() on i.IdSite equals s.IdSite
                where i.IdSociete == idSociete && i.Statut && s.Statut
                orderby s.IsSitePrincipal descending, i.IdSite
                select i).FirstOrDefaultAsync(cancellationToken);

            if (info == null)
            {
                throw new InvalidOperationException(
                    $"Aucune configuration FlexPay active pour la société {idSociete}.");
            }

            return info;
        }

        private async Task TryNotifyPaymentConfirmedAsync(
            int? idUtilisateur,
            string? orderNumber,
            int idReservation,
            int idPayment,
            CancellationToken cancellationToken)
        {
            try
            {
                if (idUtilisateur is null or <= 0 || string.IsNullOrWhiteSpace(orderNumber))
                    return;

                await _flexPayRealtimeNotifier.NotifyPaymentConfirmedAsync(
                    idUtilisateur.Value,
                    orderNumber,
                    idReservation,
                    idPayment,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "SignalR FlexPayPaymentConfirmed (restaurant) non envoyé pour order {OrderNumber}",
                    orderNumber);
            }
        }

        private async Task TryNotifyPaymentFailedAsync(
            int? idUtilisateur,
            string? orderNumber,
            string message,
            CancellationToken cancellationToken)
        {
            try
            {
                if (idUtilisateur is null or <= 0 || string.IsNullOrWhiteSpace(orderNumber))
                    return;

                await _flexPayRealtimeNotifier.NotifyPaymentFailedAsync(
                    idUtilisateur.Value,
                    orderNumber,
                    message,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "SignalR FlexPayPaymentFailed (restaurant) non envoyé pour order {OrderNumber}",
                    orderNumber);
            }
        }

        private async Task<RestaurantPayment?> FindPaymentAsync(
            string orderNumber,
            CancellationToken cancellationToken)
        {
            var normalized = orderNumber.Trim();
            return await _context.RestaurantPayments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Provider == RestaurantFlexPayConstants.Provider
                         && p.ProviderTxRef == normalized,
                    cancellationToken);
        }

        private static void ValidateCallbackAmount(
            FlexPayCallbackDto callback,
            RestaurantPayment payment)
        {
            if (!decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                && !decimal.TryParse(callback.Amount, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
            {
                return;
            }

            if (Math.Abs(amount - payment.Montant) > MontantTolerance)
            {
                throw new InvalidOperationException(
                    $"Montant callback ({amount}) différent du montant attendu ({payment.Montant}).");
            }
        }

        private static RestaurantFlexPayCallbackProcessResultDto Failure(string message) =>
            new() { Success = false, Message = message, PaymentPending = false };

        public static string CancelMessage => MessagePaiementAnnule;
        public static string DeclineMessage => MessagePaiementRefuse;
    }
}
