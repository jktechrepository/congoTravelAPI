using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueFlexPayCallbackService : ISiteTouristiqueFlexPayCallbackService
    {
        private const decimal MontantTolerance = 0.05m;
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";
        private const string MessagePaiementAnnule = "Paiement annulé.";
        private const string MessagePaiementRefuse = "Paiement refusé.";
        private const string MessagePaiementNonConfirme = "Paiement non confirmé.";

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly ISiteTouristiqueReservationConfirmationService _confirmationService;
        private readonly ISiteTouristiqueReservationService _reservationService;
        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<SiteTouristiqueFlexPayCallbackService> _logger;

        public SiteTouristiqueFlexPayCallbackService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            ISiteTouristiqueReservationConfirmationService confirmationService,
            ISiteTouristiqueReservationService reservationService,
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<SiteTouristiqueFlexPayCallbackService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _confirmationService = confirmationService;
            _reservationService = reservationService;
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task<SiteTouristiqueFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var payment = await FindPaymentAsync(orderNumber, cancellationToken)
                ?? throw new InvalidOperationException($"Paiement FlexPay site touristique {orderNumber} introuvable.");

            var reservation = await LoadReservationGraphAsync(
                payment.IdSiteTouristiqueReservation,
                idSociete,
                cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    $"Paiement FlexPay site touristique {orderNumber} introuvable pour la société {idSociete}.");
            }

            var trackedPayment = reservation.Payments
                .First(p => p.IdSiteTouristiquePayment == payment.IdSiteTouristiquePayment);

            if (reservation.Status == SiteTouristiqueReservationStatus.CONFIRMED
                && trackedPayment.Status == SiteTouristiquePaymentStatus.SUCCEEDED)
            {
                return WrapConfirmResult(reservation, trackedPayment, alreadyConfirmed: true);
            }

            var terminal = await TryFinalizeTerminalLocalStateAsync(
                reservation,
                trackedPayment,
                orderNumber.Trim(),
                cancellationToken);
            if (terminal != null)
                return new SiteTouristiqueFlexPayVerifierResultDto { StatusOnly = terminal };

            var infoPaiement = await ResolveInfoPaiementForSocieteAsync(idSociete, cancellationToken);
            var check = await _flexPayService.VerifierStatutTransactionAsync(
                infoPaiement.ApiToken,
                orderNumber.Trim(),
                cancellationToken);

            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
                // Recharger : le hold / paiement peut être devenu terminal pendant l’appel FlexPay.
                reservation = await LoadReservationGraphAsync(
                    payment.IdSiteTouristiqueReservation,
                    idSociete,
                    cancellationToken)
                    ?? reservation;
                trackedPayment = reservation.Payments
                    .First(p => p.IdSiteTouristiquePayment == payment.IdSiteTouristiquePayment);

                terminal = await TryFinalizeTerminalLocalStateAsync(
                    reservation,
                    trackedPayment,
                    orderNumber.Trim(),
                    cancellationToken);
                if (terminal != null)
                    return new SiteTouristiqueFlexPayVerifierResultDto { StatusOnly = terminal };

                // Pending FlexPay uniquement si HOLD actif + paiement encore PENDING.
                if (reservation.Status != SiteTouristiqueReservationStatus.HOLD
                    || trackedPayment.Status != SiteTouristiquePaymentStatus.PENDING)
                {
                    return new SiteTouristiqueFlexPayVerifierResultDto
                    {
                        StatusOnly = StatusOnlyNotPending(
                            reservation.IdSiteTouristiqueReservation,
                            trackedPayment.IdSiteTouristiquePayment,
                            MessagePaiementNonConfirme)
                    };
                }

                _logger.LogInformation(
                    "FlexPay verifier site touristique {OrderNumber} : statut {Status} — paiement toujours en attente.",
                    orderNumber,
                    status ?? "(null)");

                return new SiteTouristiqueFlexPayVerifierResultDto
                {
                    StatusOnly = new SiteTouristiqueFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                        IdSiteTouristiquePayment = trackedPayment.IdSiteTouristiquePayment
                    }
                };
            }

            var callbackCode = FlexPayStatusHelper.IsSuccess(status) ? "0" : "1";
            var callback = FlexPayVerifyCallbackHelper.BuildSyntheticCallback(
                check,
                orderNumber,
                trackedPayment.Montant,
                trackedPayment.CodeDevise,
                callbackCode);

            var processResult = await ProcessCallbackAsync(callback, cancellationToken);
            return await WrapVerifierResultAsync(
                processResult,
                idSociete,
                orderNumber,
                cancellationToken);
        }

        public async Task<SiteTouristiqueFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
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
                        "Callback FlexPay site touristique — paiement introuvable pour OrderNumber={OrderNumber}",
                        callback.OrderNumber);
                    return Failure("Paiement site touristique introuvable pour ce orderNumber.");
                }

                var reservation = await _context.SiteTouristiqueReservations
                    .Include(r => r.Lines)
                        .ThenInclude(l => l.Tickets)
                    .Include(r => r.Payments)
                    .FirstOrDefaultAsync(
                        r => r.IdSiteTouristiqueReservation == payment.IdSiteTouristiqueReservation,
                        cancellationToken);

                if (reservation == null)
                {
                    return Failure("Réservation site touristique associée au paiement introuvable.");
                }

                if (reservation.Status == SiteTouristiqueReservationStatus.CONFIRMED
                    && payment.Status == SiteTouristiquePaymentStatus.SUCCEEDED)
                {
                    return new SiteTouristiqueFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = "Déjà finalisé (idempotence).",
                        IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation,
                        IdSiteTouristiquePayment = payment.IdSiteTouristiquePayment
                    };
                }

                if (!string.Equals(callback.Code, "0", StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "Callback FlexPay site touristique refusé — Order={OrderNumber}, Code={Code}",
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
                FlexPayCurrencyPolicy.EnsureCallbackCurrencyMatchesExpected(
                    callback.Currency,
                    payment.CodeDevise,
                    "Callback FlexPay site touristique");

                _logger.LogInformation(
                    "Callback FlexPay site touristique — Order={OrderNumber}, Amount={Amount}, Currency={Currency}, Attendu={Montant} {DevisePaiement} (tarif {MontantTarif} {DeviseTarif})",
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
                        var trackedPayment = await _context.SiteTouristiquePayments
                            .FirstAsync(p => p.IdSiteTouristiquePayment == payment.IdSiteTouristiquePayment, cancellationToken);

                        var trackedReservation = await _context.SiteTouristiqueReservations
                            .Include(r => r.Lines)
                            .FirstAsync(
                                r => r.IdSiteTouristiqueReservation == reservation.IdSiteTouristiqueReservation,
                                cancellationToken);

                        if (trackedReservation.Status == SiteTouristiqueReservationStatus.CONFIRMED
                            && trackedPayment.Status == SiteTouristiquePaymentStatus.SUCCEEDED)
                        {
                            if (transaction != null)
                                await transaction.CommitAsync(cancellationToken);

                            return new SiteTouristiqueFlexPayCallbackProcessResultDto
                            {
                                Success = true,
                                AlreadyProcessed = true,
                                Message = "Déjà finalisé (idempotence).",
                                IdSiteTouristiqueReservation = trackedReservation.IdSiteTouristiqueReservation,
                                IdSiteTouristiquePayment = trackedPayment.IdSiteTouristiquePayment
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
                            "Callback FlexPay site touristique OK — IdReservation={IdReservation}, IdPayment={IdPayment}, Order={OrderNumber}",
                            trackedReservation.IdSiteTouristiqueReservation,
                            trackedPayment.IdSiteTouristiquePayment,
                            callback.OrderNumber);

                        await TryNotifyPaymentConfirmedAsync(
                            trackedReservation.IdUtilisateur,
                            callback.OrderNumber,
                            trackedReservation.IdSiteTouristiqueReservation,
                            trackedPayment.IdSiteTouristiquePayment,
                            cancellationToken);

                        return new SiteTouristiqueFlexPayCallbackProcessResultDto
                        {
                            Success = true,
                            Message = "Réservation site touristique confirmée après callback FlexPay.",
                            IdSiteTouristiqueReservation = trackedReservation.IdSiteTouristiqueReservation,
                            IdSiteTouristiquePayment = trackedPayment.IdSiteTouristiquePayment
                        };
                    }
                    catch (SiteTouristiqueHoldConflictException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Conflit inventaire callback FlexPay site touristique — Order={OrderNumber}",
                            callback.OrderNumber);

                        return Failure(ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Callback FlexPay site touristique refusé — Order={OrderNumber}",
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
                _logger.LogError(ex, "Erreur traitement callback FlexPay site touristique");
                return Failure(ex.Message);
            }
        }

        public async Task<SiteTouristiqueFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
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
                return Failure("Paiement site touristique introuvable pour ce orderNumber.");
            }

            var reservation = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == payment.IdSiteTouristiqueReservation,
                    cancellationToken);

            if (reservation == null)
            {
                return Failure("Réservation site touristique associée au paiement introuvable.");
            }

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                normalized,
                message,
                cancellationToken);
        }

        /// <summary>
        /// Sort du poll si l’état local est déjà terminal (FAILED, CANCELLED, EXPIRED, hold périmé).
        /// Ne renvoie jamais <c>paymentPending: true</c>.
        /// </summary>
        private async Task<SiteTouristiqueFlexPayCallbackProcessResultDto?> TryFinalizeTerminalLocalStateAsync(
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (payment.Status is SiteTouristiquePaymentStatus.FAILED or SiteTouristiquePaymentStatus.REFUNDED)
            {
                if (reservation.Status == SiteTouristiqueReservationStatus.HOLD)
                {
                    return await FailPendingAndReleaseHoldAsync(
                        reservation,
                        payment,
                        orderNumber,
                        MessagePaiementNonConfirme,
                        cancellationToken);
                }

                return StatusOnlyNotPending(
                    reservation.IdSiteTouristiqueReservation,
                    payment.IdSiteTouristiquePayment,
                    MessagePaiementNonConfirme,
                    alreadyProcessed: true);
            }

            var holdTimedOut = reservation.Status == SiteTouristiqueReservationStatus.HOLD
                && reservation.ExpiresAtUtc != null
                && reservation.ExpiresAtUtc < DateTime.UtcNow;

            var reservationReleased = reservation.Status is SiteTouristiqueReservationStatus.EXPIRED
                or SiteTouristiqueReservationStatus.CANCELLED;

            if (!holdTimedOut && !reservationReleased)
                return null;

            if (payment.Status == SiteTouristiquePaymentStatus.SUCCEEDED)
                return null;

            var message = holdTimedOut || reservation.Status == SiteTouristiqueReservationStatus.EXPIRED
                ? MessageHoldExpire
                : MessagePaiementAnnule;

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                orderNumber,
                message,
                cancellationToken);
        }

        private async Task<SiteTouristiqueFlexPayCallbackProcessResultDto> FailPendingAndReleaseHoldAsync(
            SiteTouristiqueReservation reservationSnapshot,
            SiteTouristiquePayment paymentSnapshot,
            string orderNumber,
            string message,
            CancellationToken cancellationToken)
        {
            var trackedPayment = await _context.SiteTouristiquePayments
                .FirstAsync(p => p.IdSiteTouristiquePayment == paymentSnapshot.IdSiteTouristiquePayment, cancellationToken);

            if (trackedPayment.Status == SiteTouristiquePaymentStatus.SUCCEEDED)
            {
                return new SiteTouristiqueFlexPayCallbackProcessResultDto
                {
                    Success = true,
                    AlreadyProcessed = true,
                    PaymentPending = false,
                    Message = "Paiement déjà confirmé.",
                    IdSiteTouristiqueReservation = reservationSnapshot.IdSiteTouristiqueReservation,
                    IdSiteTouristiquePayment = trackedPayment.IdSiteTouristiquePayment
                };
            }

            var alreadyFailed = trackedPayment.Status is SiteTouristiquePaymentStatus.FAILED
                or SiteTouristiquePaymentStatus.REFUNDED;

            if (!alreadyFailed)
            {
                trackedPayment.Status = SiteTouristiquePaymentStatus.FAILED;
                trackedPayment.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var release = await TryReleaseHoldAsync(
                reservationSnapshot.IdSiteTouristiqueReservation,
                reservationSnapshot.IdSociete,
                orderNumber,
                cancellationToken);

            if (!release.Released)
            {
                return new SiteTouristiqueFlexPayCallbackProcessResultDto
                {
                    Success = false,
                    PaymentPending = false,
                    Message = release.ErrorMessage
                        ?? "Paiement marqué échoué mais le hold n’a pas pu être libéré.",
                    IdSiteTouristiqueReservation = reservationSnapshot.IdSiteTouristiqueReservation,
                    IdSiteTouristiquePayment = trackedPayment.IdSiteTouristiquePayment
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
                "FlexPay site touristique abandonné — Order={OrderNumber}, Payment={IdPayment}, Message={Message}",
                orderNumber,
                trackedPayment.IdSiteTouristiquePayment,
                message);

            return StatusOnlyNotPending(
                reservationSnapshot.IdSiteTouristiqueReservation,
                trackedPayment.IdSiteTouristiquePayment,
                message,
                alreadyProcessed: alreadyFailed);
        }

        /// <summary>
        /// Détache toute réservation trackée (souvent sans Lines) puis appelle CancelAsync
        /// pour forcer un reload avec graphe complet sur le DbContext partagé.
        /// </summary>
        private async Task<(bool Released, string? ErrorMessage)> TryReleaseHoldAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            DetachTrackedReservation(idSiteTouristiqueReservation);

            var status = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation)
                .Select(r => r.Status)
                .FirstAsync(cancellationToken);

            if (status != SiteTouristiqueReservationStatus.HOLD)
                return (true, null);

            try
            {
                DetachTrackedReservation(idSiteTouristiqueReservation);
                await _reservationService.CancelAsync(
                    idSiteTouristiqueReservation,
                    idSociete,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Libération hold site touristique après abandon FlexPay — Order={OrderNumber}, Reservation={IdReservation}",
                    orderNumber,
                    idSiteTouristiqueReservation);
            }

            DetachTrackedReservation(idSiteTouristiqueReservation);
            status = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation)
                .Select(r => r.Status)
                .FirstAsync(cancellationToken);

            if (status == SiteTouristiqueReservationStatus.HOLD)
            {
                return (
                    false,
                    "Impossible de libérer le hold de la réservation. Réessayez l’abandon.");
            }

            return (true, null);
        }

        private void DetachTrackedReservation(int idSiteTouristiqueReservation)
        {
            var tracked = _context.ChangeTracker
                .Entries<SiteTouristiqueReservation>()
                .Where(e => e.Entity.IdSiteTouristiqueReservation == idSiteTouristiqueReservation)
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;
        }

        private static SiteTouristiqueFlexPayCallbackProcessResultDto StatusOnlyNotPending(
            int idSiteTouristiqueReservation,
            int idSiteTouristiquePayment,
            string message,
            bool alreadyProcessed = false) =>
            new()
            {
                Success = true,
                AlreadyProcessed = alreadyProcessed,
                PaymentPending = false,
                Message = message,
                IdSiteTouristiqueReservation = idSiteTouristiqueReservation,
                IdSiteTouristiquePayment = idSiteTouristiquePayment
            };

        private async Task<SiteTouristiqueFlexPayVerifierResultDto> WrapVerifierResultAsync(
            SiteTouristiqueFlexPayCallbackProcessResultDto processResult,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (processResult.Success
                && processResult.IdSiteTouristiqueReservation.HasValue
                && !processResult.PaymentPending)
            {
                var reservation = await LoadReservationGraphAsync(
                    processResult.IdSiteTouristiqueReservation.Value,
                    idSociete,
                    cancellationToken);

                if (reservation?.Status == SiteTouristiqueReservationStatus.CONFIRMED)
                {
                    var payment = reservation.Payments
                        .OrderByDescending(p => p.DateCreation)
                        .First(p => string.Equals(p.ProviderTxRef, orderNumber.Trim(), StringComparison.Ordinal)
                                    || p.Status == SiteTouristiquePaymentStatus.SUCCEEDED);

                    return WrapConfirmResult(
                        reservation,
                        payment,
                        processResult.AlreadyProcessed);
                }
            }

            return new SiteTouristiqueFlexPayVerifierResultDto { StatusOnly = processResult };
        }

        private static SiteTouristiqueFlexPayVerifierResultDto WrapConfirmResult(
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                ConfirmPayment = SiteTouristiqueReservationMapper.ToConfirmPaymentResponse(
                    reservation,
                    payment,
                    alreadyConfirmed)
            };

        private async Task<SiteTouristiqueReservation?> LoadReservationGraphAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
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
                    "SignalR FlexPayPaymentConfirmed (site touristique) non envoyé pour order {OrderNumber}",
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
                    "SignalR FlexPayPaymentFailed (site touristique) non envoyé pour order {OrderNumber}",
                    orderNumber);
            }
        }

        private async Task<SiteTouristiquePayment?> FindPaymentAsync(
            string orderNumber,
            CancellationToken cancellationToken)
        {
            var normalized = orderNumber.Trim();
            return await _context.SiteTouristiquePayments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Provider == SiteTouristiqueFlexPayConstants.Provider
                         && p.ProviderTxRef == normalized,
                    cancellationToken);
        }

        private static void ValidateCallbackAmount(
            FlexPayCallbackDto callback,
            SiteTouristiquePayment payment)
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

        private static SiteTouristiqueFlexPayCallbackProcessResultDto Failure(string message) =>
            new() { Success = false, Message = message, PaymentPending = false };

        // Exposed for controller message constants consistency
        public static string CancelMessage => MessagePaiementAnnule;
        public static string DeclineMessage => MessagePaiementRefuse;
    }
}
