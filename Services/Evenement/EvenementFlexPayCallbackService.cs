using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementFlexPayCallbackService : IEvenementFlexPayCallbackService
    {
        private const decimal MontantTolerance = 0.05m;
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";
        private const string MessagePaiementAnnule = "Paiement annulé.";
        private const string MessagePaiementRefuse = "Paiement refusé.";
        private const string MessagePaiementNonConfirme = "Paiement non confirmé.";

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IEvenementReservationConfirmationService _confirmationService;
        private readonly IEvenementReservationService _reservationService;
        private readonly IEvenementCommandeFlexPayService _commandeFlexPayService;
        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly IReversementAutomatiqueService _reversementAutomatiqueService;
        private readonly ILogger<EvenementFlexPayCallbackService> _logger;

        public EvenementFlexPayCallbackService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IEvenementReservationConfirmationService confirmationService,
            IEvenementReservationService reservationService,
            IEvenementCommandeFlexPayService commandeFlexPayService,
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            IReversementAutomatiqueService reversementAutomatiqueService,
            ILogger<EvenementFlexPayCallbackService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _confirmationService = confirmationService;
            _reservationService = reservationService;
            _commandeFlexPayService = commandeFlexPayService;
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _reversementAutomatiqueService = reversementAutomatiqueService;
            _logger = logger;
        }

        public async Task<EvenementFlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                throw new ArgumentException("orderNumber requis.", nameof(orderNumber));

            var payment = await FindPaymentAsync(orderNumber, cancellationToken)
                ?? throw new InvalidOperationException($"Paiement FlexPay événement {orderNumber} introuvable.");

            // Plan A — commande en attente (pas de réservation)
            if (payment.IdEvenementCommandeEnAttente.HasValue
                && payment.IdEvenementReservation is null or 0)
            {
                return await VerifyCommandeAndFinalizeAsync(
                    orderNumber.Trim(), payment, idSociete, cancellationToken);
            }

            if (payment.IdEvenementReservation is null or 0)
            {
                throw new KeyNotFoundException(
                    $"Paiement FlexPay événement {orderNumber} introuvable pour la société {idSociete}.");
            }

            var reservation = await LoadReservationGraphAsync(
                payment.IdEvenementReservation.Value,
                idSociete,
                cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException(
                    $"Paiement FlexPay événement {orderNumber} introuvable pour la société {idSociete}.");
            }

            var trackedPayment = reservation.Payments
                .First(p => p.IdEvenementPayment == payment.IdEvenementPayment);

            if (reservation.Status == EvenementReservationStatus.CONFIRMED
                && trackedPayment.Status == EvenementPaymentStatus.SUCCEEDED)
            {
                return WrapConfirmResult(reservation, trackedPayment, alreadyConfirmed: true);
            }

            var terminal = await TryFinalizeTerminalLocalStateAsync(
                reservation,
                trackedPayment,
                orderNumber.Trim(),
                cancellationToken);
            if (terminal != null)
                return new EvenementFlexPayVerifierResultDto { StatusOnly = terminal };

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
                    payment.IdEvenementReservation!.Value,
                    idSociete,
                    cancellationToken)
                    ?? reservation;
                trackedPayment = reservation.Payments
                    .First(p => p.IdEvenementPayment == payment.IdEvenementPayment);

                terminal = await TryFinalizeTerminalLocalStateAsync(
                    reservation,
                    trackedPayment,
                    orderNumber.Trim(),
                    cancellationToken);
                if (terminal != null)
                    return new EvenementFlexPayVerifierResultDto { StatusOnly = terminal };

                // Pending FlexPay uniquement si HOLD actif + paiement encore PENDING.
                if (reservation.Status != EvenementReservationStatus.HOLD
                    || trackedPayment.Status != EvenementPaymentStatus.PENDING)
                {
                    return new EvenementFlexPayVerifierResultDto
                    {
                        StatusOnly = StatusOnlyNotPending(
                            reservation.IdEvenementReservation,
                            trackedPayment.IdEvenementPayment,
                            MessagePaiementNonConfirme)
                    };
                }

                _logger.LogInformation(
                    "FlexPay verifier événement {OrderNumber} : statut {Status} — paiement toujours en attente.",
                    orderNumber,
                    status ?? "(null)");

                return new EvenementFlexPayVerifierResultDto
                {
                    StatusOnly = new EvenementFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdEvenementReservation = reservation.IdEvenementReservation,
                        IdEvenementPayment = trackedPayment.IdEvenementPayment
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

        public async Task<EvenementFlexPayCallbackProcessResultDto> ProcessCallbackAsync(
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
                        "Callback FlexPay événement — paiement introuvable pour OrderNumber={OrderNumber}",
                        callback.OrderNumber);
                    return Failure("Paiement événement introuvable pour ce orderNumber.");
                }

                // Plan A : paiement lié à une commande (pas encore de réservation)
                if (payment.IdEvenementCommandeEnAttente.HasValue
                    && payment.IdEvenementReservation is null or 0)
                {
                    return await ProcessCommandeCallbackAsync(callback, payment, cancellationToken);
                }

                var reservation = await _context.EvenementReservations
                    .Include(r => r.Lines)
                        .ThenInclude(l => l.Tickets)
                    .Include(r => r.Payments)
                    .FirstOrDefaultAsync(
                        r => r.IdEvenementReservation == payment.IdEvenementReservation,
                        cancellationToken);

                if (reservation == null)
                {
                    return Failure("Réservation événement associée au paiement introuvable.");
                }

                if (reservation.Status == EvenementReservationStatus.CONFIRMED
                    && payment.Status == EvenementPaymentStatus.SUCCEEDED)
                {
                    return new EvenementFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = "Déjà finalisé (idempotence).",
                        IdEvenementReservation = reservation.IdEvenementReservation,
                        IdEvenementPayment = payment.IdEvenementPayment
                    };
                }

                if (!string.Equals(callback.Code, "0", StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "Callback FlexPay événement refusé — Order={OrderNumber}, Code={Code}",
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
                    "Callback FlexPay événement");

                _logger.LogInformation(
                    "Callback FlexPay événement — Order={OrderNumber}, Amount={Amount}, Currency={Currency}, Attendu={Montant} {DevisePaiement} (tarif {MontantTarif} {DeviseTarif})",
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
                        var trackedPayment = await _context.EvenementPayments
                            .FirstAsync(p => p.IdEvenementPayment == payment.IdEvenementPayment, cancellationToken);

                        var trackedReservation = await _context.EvenementReservations
                            .Include(r => r.Lines)
                            .FirstAsync(
                                r => r.IdEvenementReservation == reservation.IdEvenementReservation,
                                cancellationToken);

                        if (trackedReservation.Status == EvenementReservationStatus.CONFIRMED
                            && trackedPayment.Status == EvenementPaymentStatus.SUCCEEDED)
                        {
                            if (transaction != null)
                                await transaction.CommitAsync(cancellationToken);

                            return new EvenementFlexPayCallbackProcessResultDto
                            {
                                Success = true,
                                AlreadyProcessed = true,
                                Message = "Déjà finalisé (idempotence).",
                                IdEvenementReservation = trackedReservation.IdEvenementReservation,
                                IdEvenementPayment = trackedPayment.IdEvenementPayment
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
                            "Callback FlexPay événement OK — IdReservation={IdReservation}, IdPayment={IdPayment}, Order={OrderNumber}",
                            trackedReservation.IdEvenementReservation,
                            trackedPayment.IdEvenementPayment,
                            callback.OrderNumber);

                        await TryNotifyPaymentConfirmedAsync(
                            trackedReservation.IdUtilisateur,
                            callback.OrderNumber,
                            trackedReservation.IdEvenementReservation,
                            trackedPayment.IdEvenementPayment,
                            cancellationToken);

                        await _reversementAutomatiqueService.TryDeclencherAsync(
                            ReversementAutomatiqueContext.FromEvenement(trackedPayment, trackedReservation),
                            cancellationToken);

                        return new EvenementFlexPayCallbackProcessResultDto
                        {
                            Success = true,
                            Message = "Réservation événement confirmée après callback FlexPay.",
                            IdEvenementReservation = trackedReservation.IdEvenementReservation,
                            IdEvenementPayment = trackedPayment.IdEvenementPayment
                        };
                    }
                    catch (EvenementHoldConflictException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Conflit inventaire callback FlexPay événement — Order={OrderNumber}",
                            callback.OrderNumber);

                        return Failure(ex.Message);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (transaction != null)
                            await transaction.RollbackAsync(cancellationToken);

                        _logger.LogWarning(
                            ex,
                            "Callback FlexPay événement refusé — Order={OrderNumber}",
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
                _logger.LogError(ex, "Erreur traitement callback FlexPay événement");
                return Failure(ex.Message);
            }
        }

        public async Task<EvenementFlexPayCallbackProcessResultDto> AbandonPendingPaymentAsync(
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
                return Failure("Paiement événement introuvable pour ce orderNumber.");
            }

            if (payment.IdEvenementCommandeEnAttente.HasValue
                && payment.IdEvenementReservation is null or 0)
            {
                var commande = await _context.EvenementCommandesEnAttente
                    .FirstOrDefaultAsync(
                        c => c.IdEvenementCommandeEnAttente == payment.IdEvenementCommandeEnAttente.Value,
                        cancellationToken);

                if (commande == null)
                {
                    if (payment.Status != EvenementPaymentStatus.FAILED)
                    {
                        payment.Status = EvenementPaymentStatus.FAILED;
                        payment.DateModification = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    return StatusOnlyNotPending(0, payment.IdEvenementPayment, message, alreadyProcessed: true);
                }

                var idUtilisateur = commande.IdUtilisateur;
                await _commandeFlexPayService.FailCommandeAsync(commande, payment, cancellationToken);
                await TryNotifyPaymentFailedAsync(idUtilisateur, normalized, message, cancellationToken);
                return StatusOnlyNotPending(0, payment.IdEvenementPayment, message);
            }

            var reservation = await _context.EvenementReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == payment.IdEvenementReservation,
                    cancellationToken);

            if (reservation == null)
            {
                return Failure("Réservation événement associée au paiement introuvable.");
            }

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                normalized,
                message,
                cancellationToken);
        }

        private async Task<EvenementFlexPayVerifierResultDto> VerifyCommandeAndFinalizeAsync(
            string orderNumber,
            EvenementPayment payment,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var commande = await _context.EvenementCommandesEnAttente
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    c => c.IdEvenementCommandeEnAttente == payment.IdEvenementCommandeEnAttente
                         && c.IdSociete == idSociete,
                    cancellationToken);

            if (commande == null)
            {
                throw new KeyNotFoundException(
                    $"Paiement FlexPay événement {orderNumber} introuvable pour la société {idSociete}.");
            }

            if (payment.Status == EvenementPaymentStatus.FAILED)
            {
                return new EvenementFlexPayVerifierResultDto
                {
                    StatusOnly = StatusOnlyNotPending(0, payment.IdEvenementPayment, MessagePaiementNonConfirme, true)
                };
            }

            if (commande.DateExpiration.HasValue && commande.DateExpiration.Value < DateTime.UtcNow)
            {
                var trackedCmd = await _context.EvenementCommandesEnAttente
                    .FirstAsync(c => c.IdEvenementCommandeEnAttente == commande.IdEvenementCommandeEnAttente, cancellationToken);
                var trackedPay = await _context.EvenementPayments
                    .FirstAsync(p => p.IdEvenementPayment == payment.IdEvenementPayment, cancellationToken);
                var idUser = trackedCmd.IdUtilisateur;
                await _commandeFlexPayService.FailCommandeAsync(trackedCmd, trackedPay, cancellationToken);
                await TryNotifyPaymentFailedAsync(idUser, orderNumber, MessageHoldExpire, cancellationToken);
                return new EvenementFlexPayVerifierResultDto
                {
                    StatusOnly = StatusOnlyNotPending(0, payment.IdEvenementPayment, MessageHoldExpire)
                };
            }

            var infoPaiement = await ResolveInfoPaiementForSocieteAsync(idSociete, cancellationToken);
            var check = await _flexPayService.VerifierStatutTransactionAsync(
                infoPaiement.ApiToken,
                orderNumber,
                cancellationToken);

            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
                return new EvenementFlexPayVerifierResultDto
                {
                    StatusOnly = new EvenementFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        PaymentPending = true,
                        Message = "Paiement en attente de validation Mobile Money.",
                        IdEvenementReservation = 0,
                        IdEvenementPayment = payment.IdEvenementPayment
                    }
                };
            }

            var callbackCode = FlexPayStatusHelper.IsSuccess(status) ? "0" : "1";
            var callback = FlexPayVerifyCallbackHelper.BuildSyntheticCallback(
                check,
                orderNumber,
                payment.Montant,
                payment.CodeDevise,
                callbackCode);

            var processResult = await ProcessCallbackAsync(callback, cancellationToken);
            return await WrapVerifierResultAsync(processResult, idSociete, orderNumber, cancellationToken);
        }

        private async Task<EvenementFlexPayCallbackProcessResultDto> ProcessCommandeCallbackAsync(
            FlexPayCallbackDto callback,
            EvenementPayment payment,
            CancellationToken cancellationToken)
        {
            var commande = await _context.EvenementCommandesEnAttente
                .FirstOrDefaultAsync(
                    c => c.IdEvenementCommandeEnAttente == payment.IdEvenementCommandeEnAttente,
                    cancellationToken);

            if (commande == null)
            {
                if (payment.Status == EvenementPaymentStatus.SUCCEEDED
                    && payment.IdEvenementReservation is > 0)
                {
                    return new EvenementFlexPayCallbackProcessResultDto
                    {
                        Success = true,
                        AlreadyProcessed = true,
                        Message = "Déjà finalisé (idempotence).",
                        IdEvenementReservation = payment.IdEvenementReservation,
                        IdEvenementPayment = payment.IdEvenementPayment
                    };
                }

                return Failure("Commande événement associée au paiement introuvable.");
            }

            if (!string.Equals(callback.Code, "0", StringComparison.Ordinal))
            {
                var idUser = commande.IdUtilisateur;
                await _commandeFlexPayService.FailCommandeAsync(commande, payment, cancellationToken);
                await TryNotifyPaymentFailedAsync(
                    idUser,
                    callback.OrderNumber!.Trim(),
                    "Paiement refusé par FlexPay.",
                    cancellationToken);
                return StatusOnlyNotPending(0, payment.IdEvenementPayment, "Paiement refusé par FlexPay.");
            }

            ValidateCallbackAmountForCommande(callback, payment);
            FlexPayCurrencyPolicy.EnsureCallbackCurrencyMatchesExpected(
                callback.Currency,
                payment.CodeDevise,
                "Callback FlexPay événement");

            if (commande.DateExpiration.HasValue && commande.DateExpiration.Value < DateTime.UtcNow)
            {
                var idUser = commande.IdUtilisateur;
                await _commandeFlexPayService.FailCommandeAsync(commande, payment, cancellationToken);
                await TryNotifyPaymentFailedAsync(
                    idUser, callback.OrderNumber!.Trim(), MessageHoldExpire, cancellationToken);
                return Failure(MessageHoldExpire);
            }

            var reservation = await _commandeFlexPayService.FinalizeCommandeSuccessAsync(
                commande, payment, cancellationToken);

            await TryNotifyPaymentConfirmedAsync(
                reservation.IdUtilisateur,
                callback.OrderNumber,
                reservation.IdEvenementReservation,
                payment.IdEvenementPayment,
                cancellationToken);

            await _reversementAutomatiqueService.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation),
                cancellationToken);

            return new EvenementFlexPayCallbackProcessResultDto
            {
                Success = true,
                Message = "Réservation événement confirmée après callback FlexPay.",
                IdEvenementReservation = reservation.IdEvenementReservation,
                IdEvenementPayment = payment.IdEvenementPayment
            };
        }

        private void ValidateCallbackAmountForCommande(FlexPayCallbackDto callback, EvenementPayment payment)
        {
            if (string.IsNullOrWhiteSpace(callback.Amount))
                return;

            if (!decimal.TryParse(
                    callback.Amount,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount))
            {
                throw new InvalidOperationException("Montant callback FlexPay invalide.");
            }

            if (Math.Abs(amount - payment.Montant) > MontantTolerance)
            {
                throw new InvalidOperationException(
                    $"Montant callback ({amount}) ≠ montant paiement ({payment.Montant}).");
            }
        }

        /// <summary>
        /// Sort du poll si l’état local est déjà terminal (FAILED, CANCELLED, EXPIRED, hold périmé).
        /// Ne renvoie jamais <c>paymentPending: true</c>.
        /// </summary>
        private async Task<EvenementFlexPayCallbackProcessResultDto?> TryFinalizeTerminalLocalStateAsync(
            EvenementReservation reservation,
            EvenementPayment payment,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (payment.Status is EvenementPaymentStatus.FAILED or EvenementPaymentStatus.REFUNDED)
            {
                if (reservation.Status == EvenementReservationStatus.HOLD)
                {
                    return await FailPendingAndReleaseHoldAsync(
                        reservation,
                        payment,
                        orderNumber,
                        MessagePaiementNonConfirme,
                        cancellationToken);
                }

                if (reservation.Status is EvenementReservationStatus.CANCELLED
                    or EvenementReservationStatus.EXPIRED)
                {
                    await _reservationService.PurgeNeverConfirmedAsync(
                        reservation.IdEvenementReservation,
                        reservation.IdSociete,
                        cancellationToken);
                }

                return StatusOnlyNotPending(
                    reservation.IdEvenementReservation,
                    payment.IdEvenementPayment,
                    MessagePaiementNonConfirme,
                    alreadyProcessed: true);
            }

            var holdTimedOut = reservation.Status == EvenementReservationStatus.HOLD
                && reservation.ExpiresAtUtc != null
                && reservation.ExpiresAtUtc < DateTime.UtcNow;

            var reservationReleased = reservation.Status is EvenementReservationStatus.EXPIRED
                or EvenementReservationStatus.CANCELLED;

            if (!holdTimedOut && !reservationReleased)
                return null;

            if (payment.Status == EvenementPaymentStatus.SUCCEEDED)
                return null;

            var message = holdTimedOut || reservation.Status == EvenementReservationStatus.EXPIRED
                ? MessageHoldExpire
                : MessagePaiementAnnule;

            return await FailPendingAndReleaseHoldAsync(
                reservation,
                payment,
                orderNumber,
                message,
                cancellationToken);
        }

        private async Task<EvenementFlexPayCallbackProcessResultDto> FailPendingAndReleaseHoldAsync(
            EvenementReservation reservationSnapshot,
            EvenementPayment paymentSnapshot,
            string orderNumber,
            string message,
            CancellationToken cancellationToken)
        {
            var trackedPayment = await _context.EvenementPayments
                .FirstAsync(p => p.IdEvenementPayment == paymentSnapshot.IdEvenementPayment, cancellationToken);

            if (trackedPayment.Status == EvenementPaymentStatus.SUCCEEDED)
            {
                return new EvenementFlexPayCallbackProcessResultDto
                {
                    Success = true,
                    AlreadyProcessed = true,
                    PaymentPending = false,
                    Message = "Paiement déjà confirmé.",
                    IdEvenementReservation = reservationSnapshot.IdEvenementReservation,
                    IdEvenementPayment = trackedPayment.IdEvenementPayment
                };
            }

            var alreadyFailed = trackedPayment.Status is EvenementPaymentStatus.FAILED
                or EvenementPaymentStatus.REFUNDED;

            if (!alreadyFailed)
            {
                trackedPayment.Status = EvenementPaymentStatus.FAILED;
                trackedPayment.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var release = await TryReleaseHoldAsync(
                reservationSnapshot.IdEvenementReservation,
                reservationSnapshot.IdSociete,
                orderNumber,
                cancellationToken);

            if (!release.Released)
            {
                return new EvenementFlexPayCallbackProcessResultDto
                {
                    Success = false,
                    PaymentPending = false,
                    Message = release.ErrorMessage
                        ?? "Paiement marqué échoué mais le hold n’a pas pu être libéré.",
                    IdEvenementReservation = reservationSnapshot.IdEvenementReservation,
                    IdEvenementPayment = trackedPayment.IdEvenementPayment
                };
            }

            await _reservationService.PurgeNeverConfirmedAsync(
                reservationSnapshot.IdEvenementReservation,
                reservationSnapshot.IdSociete,
                cancellationToken);

            if (!alreadyFailed)
            {
                await TryNotifyPaymentFailedAsync(
                    reservationSnapshot.IdUtilisateur,
                    orderNumber,
                    message,
                    cancellationToken);
            }

            _logger.LogInformation(
                "FlexPay événement abandonné — Order={OrderNumber}, Payment={IdPayment}, Message={Message}",
                orderNumber,
                trackedPayment.IdEvenementPayment,
                message);

            return StatusOnlyNotPending(
                reservationSnapshot.IdEvenementReservation,
                trackedPayment.IdEvenementPayment,
                message,
                alreadyProcessed: alreadyFailed);
        }

        /// <summary>
        /// Détache toute réservation trackée (souvent sans Lines) puis appelle CancelAsync
        /// pour forcer un reload avec graphe complet sur le DbContext partagé.
        /// </summary>
        private async Task<(bool Released, string? ErrorMessage)> TryReleaseHoldAsync(
            int idEvenementReservation,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            DetachTrackedReservation(idEvenementReservation);
            var status = await _context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.IdEvenementReservation == idEvenementReservation)
                .Select(r => (EvenementReservationStatus?)r.Status)
                .FirstOrDefaultAsync(cancellationToken);

            // Déjà absente (purge CancelAsync HOLD) ou plus en HOLD → inventaire libéré.
            if (status is null || status != EvenementReservationStatus.HOLD)
                return (true, null);

            try
            {
                DetachTrackedReservation(idEvenementReservation);
                await _reservationService.CancelAsync(
                    idEvenementReservation,
                    idSociete,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Libération hold événement après abandon FlexPay — Order={OrderNumber}, Reservation={IdReservation}",
                    orderNumber,
                    idEvenementReservation);
            }

            DetachTrackedReservation(idEvenementReservation);
            status = await _context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.IdEvenementReservation == idEvenementReservation)
                .Select(r => (EvenementReservationStatus?)r.Status)
                .FirstOrDefaultAsync(cancellationToken);

            if (status == EvenementReservationStatus.HOLD)
            {
                return (
                    false,
                    "Impossible de libérer le hold de la réservation. Réessayez l’abandon.");
            }

            return (true, null);
        }

        private void DetachTrackedReservation(int idEvenementReservation)
        {
            var tracked = _context.ChangeTracker
                .Entries<EvenementReservation>()
                .Where(e => e.Entity.IdEvenementReservation == idEvenementReservation)
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;
        }

        private static EvenementFlexPayCallbackProcessResultDto StatusOnlyNotPending(
            int idEvenementReservation,
            int idEvenementPayment,
            string message,
            bool alreadyProcessed = false) =>
            new()
            {
                Success = true,
                AlreadyProcessed = alreadyProcessed,
                PaymentPending = false,
                Message = message,
                IdEvenementReservation = idEvenementReservation,
                IdEvenementPayment = idEvenementPayment
            };

        private async Task<EvenementFlexPayVerifierResultDto> WrapVerifierResultAsync(
            EvenementFlexPayCallbackProcessResultDto processResult,
            int idSociete,
            string orderNumber,
            CancellationToken cancellationToken)
        {
            if (processResult.Success
                && processResult.IdEvenementReservation.HasValue
                && !processResult.PaymentPending)
            {
                var reservation = await LoadReservationGraphAsync(
                    processResult.IdEvenementReservation.Value,
                    idSociete,
                    cancellationToken);

                if (reservation?.Status == EvenementReservationStatus.CONFIRMED)
                {
                    var payment = reservation.Payments
                        .OrderByDescending(p => p.DateCreation)
                        .First(p => string.Equals(p.ProviderTxRef, orderNumber.Trim(), StringComparison.Ordinal)
                                    || p.Status == EvenementPaymentStatus.SUCCEEDED);

                    return WrapConfirmResult(
                        reservation,
                        payment,
                        processResult.AlreadyProcessed);
                }
            }

            return new EvenementFlexPayVerifierResultDto { StatusOnly = processResult };
        }

        private static EvenementFlexPayVerifierResultDto WrapConfirmResult(
            EvenementReservation reservation,
            EvenementPayment payment,
            bool alreadyConfirmed) =>
            new()
            {
                ConfirmPayment = EvenementReservationMapper.ToConfirmPaymentResponse(
                    reservation,
                    payment,
                    alreadyConfirmed)
            };

        private async Task<EvenementReservation?> LoadReservationGraphAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.EvenementReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
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
                    "SignalR FlexPayPaymentConfirmed (événement) non envoyé pour order {OrderNumber}",
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
                    "SignalR FlexPayPaymentFailed (événement) non envoyé pour order {OrderNumber}",
                    orderNumber);
            }
        }

        private async Task<EvenementPayment?> FindPaymentAsync(
            string orderNumber,
            CancellationToken cancellationToken)
        {
            var normalized = orderNumber.Trim();
            return await _context.EvenementPayments
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Provider == EvenementFlexPayConstants.Provider
                         && p.ProviderTxRef == normalized,
                    cancellationToken);
        }

        private static void ValidateCallbackAmount(
            FlexPayCallbackDto callback,
            EvenementPayment payment)
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

        private static EvenementFlexPayCallbackProcessResultDto Failure(string message) =>
            new() { Success = false, Message = message, PaymentPending = false };

        // Exposed for controller message constants consistency
        public static string CancelMessage => MessagePaiementAnnule;
        public static string DeclineMessage => MessagePaiementRefuse;
    }
}
