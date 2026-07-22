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

namespace CongoTravel.Services.Evenement
{
    public class EvenementFlexPayCallbackService : IEvenementFlexPayCallbackService
    {
        private const decimal MontantTolerance = 0.05m;

        private readonly CongoTravelDbContext _context;
        private readonly IFlexPayService _flexPayService;
        private readonly IEvenementReservationConfirmationService _confirmationService;
        private readonly ILogger<EvenementFlexPayCallbackService> _logger;

        public EvenementFlexPayCallbackService(
            CongoTravelDbContext context,
            IFlexPayService flexPayService,
            IEvenementReservationConfirmationService confirmationService,
            ILogger<EvenementFlexPayCallbackService> logger)
        {
            _context = context;
            _flexPayService = flexPayService;
            _confirmationService = confirmationService;
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

            var reservation = await LoadReservationGraphAsync(
                payment.IdEvenementReservation,
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

            var infoPaiement = await ResolveInfoPaiementForSocieteAsync(idSociete, cancellationToken);
            var check = await _flexPayService.VerifierStatutTransactionAsync(
                infoPaiement.ApiToken,
                orderNumber.Trim(),
                cancellationToken);

            var status = check.Transaction?.Status ?? check.Code;
            if (FlexPayStatusHelper.ShouldTreatAsPending(status))
            {
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
                    return await MarkPaymentFailedAsync(
                        payment,
                        reservation.IdEvenementReservation,
                        callback,
                        cancellationToken);
                }

                ValidateCallbackAmount(callback, payment);

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

                        var failedPayment = await _context.EvenementPayments
                            .FirstAsync(p => p.IdEvenementPayment == payment.IdEvenementPayment, cancellationToken);
                        failedPayment.Status = EvenementPaymentStatus.FAILED;
                        failedPayment.DateModification = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);

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

        private async Task<EvenementFlexPayCallbackProcessResultDto> MarkPaymentFailedAsync(
            EvenementPayment payment,
            int idEvenementReservation,
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            var trackedPayment = await _context.EvenementPayments
                .FirstAsync(p => p.IdEvenementPayment == payment.IdEvenementPayment, cancellationToken);

            if (trackedPayment.Status != EvenementPaymentStatus.SUCCEEDED)
            {
                trackedPayment.Status = EvenementPaymentStatus.FAILED;
                trackedPayment.ProviderTxRef = callback.OrderNumber?.Trim() ?? trackedPayment.ProviderTxRef;
                trackedPayment.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Callback FlexPay événement refusé — IdPayment={IdPayment}, Order={OrderNumber}, Code={Code}",
                trackedPayment.IdEvenementPayment,
                callback.OrderNumber,
                callback.Code);

            return new EvenementFlexPayCallbackProcessResultDto
            {
                Success = true,
                Message = "Paiement refusé par FlexPay.",
                IdEvenementReservation = idEvenementReservation,
                IdEvenementPayment = trackedPayment.IdEvenementPayment
            };
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
            new() { Success = false, Message = message };
    }
}
