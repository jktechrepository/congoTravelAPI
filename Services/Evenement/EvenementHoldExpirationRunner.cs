using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementHoldExpirationRunner : IEvenementHoldExpirationRunner
    {
        private const string ExpireProcedureSql = "CALL `sp_ExpireEvenementHolds`()";
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<EvenementHoldExpirationRunner> _logger;

        public EvenementHoldExpirationRunner(
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<EvenementHoldExpirationRunner> logger)
        {
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var expiredHoldIds = await context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.Status == EvenementReservationStatus.HOLD
                            && r.ExpiresAtUtc != null
                            && r.ExpiresAtUtc < utcNow)
                .Select(r => r.IdEvenementReservation)
                .ToListAsync(cancellationToken);

            if (expiredHoldIds.Count == 0)
            {
                _logger.LogDebug("Expiration holds événement : aucun HOLD expiré en attente.");
                return;
            }

            var pendingFlexPay = await (
                from p in context.EvenementPayments.AsNoTracking()
                join r in context.EvenementReservations.AsNoTracking()
                    on p.IdEvenementReservation equals r.IdEvenementReservation
                where expiredHoldIds.Contains(p.IdEvenementReservation)
                      && p.Status == EvenementPaymentStatus.PENDING
                      && p.Provider == EvenementFlexPayConstants.Provider
                select new PendingFlexPayExpireDto(
                    p.IdEvenementPayment,
                    p.IdEvenementReservation,
                    p.ProviderTxRef,
                    r.IdUtilisateur)).ToListAsync(cancellationToken);

            await ExpireHoldsInventoryAsync(context, expiredHoldIds, cancellationToken);
            await FailPendingFlexPayAndNotifyAsync(context, pendingFlexPay, cancellationToken);
        }

        private async Task ExpireHoldsInventoryAsync(
            CongoTravelDbContext context,
            IReadOnlyList<int> expiredHoldIds,
            CancellationToken cancellationToken)
        {
            if (context.Database.IsRelational()
                && string.Equals(context.Database.ProviderName, "Pomelo.EntityFrameworkCore.MySql", StringComparison.Ordinal))
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(ExpireProcedureSql, cancellationToken);
                    _logger.LogInformation(
                        "Expiration holds événement : {PendingCount} réservation(s) HOLD expirée(s) traitée(s) via sp_ExpireEvenementHolds.",
                        expiredHoldIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec sp_ExpireEvenementHolds ({PendingCount} HOLD en attente). Vérifier Scripts/production_evenement_hold_expiration_job.sql.",
                        expiredHoldIds.Count);
                }

                return;
            }

            // InMemory / tests : marque EXPIRED sans procédure SQL (inventaire non restitué ici).
            var holds = await context.EvenementReservations
                .Where(r => expiredHoldIds.Contains(r.IdEvenementReservation)
                            && r.Status == EvenementReservationStatus.HOLD)
                .ToListAsync(cancellationToken);

            foreach (var hold in holds)
            {
                hold.Status = EvenementReservationStatus.EXPIRED;
                hold.DateModification = DateTime.UtcNow;
            }

            if (holds.Count > 0)
                await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Expiration holds événement (non-MySQL) : {Count} réservation(s) marquées EXPIRED.",
                holds.Count);
        }

        private async Task FailPendingFlexPayAndNotifyAsync(
            CongoTravelDbContext context,
            IReadOnlyList<PendingFlexPayExpireDto> pendingFlexPay,
            CancellationToken cancellationToken)
        {
            if (pendingFlexPay.Count == 0)
                return;

            var paymentIds = pendingFlexPay.Select(p => p.IdEvenementPayment).ToList();
            var payments = await context.EvenementPayments
                .Where(p => paymentIds.Contains(p.IdEvenementPayment)
                            && p.Status == EvenementPaymentStatus.PENDING)
                .ToListAsync(cancellationToken);

            var utcNow = DateTime.UtcNow;
            foreach (var payment in payments)
            {
                payment.Status = EvenementPaymentStatus.FAILED;
                payment.DateModification = utcNow;
            }

            if (payments.Count > 0)
                await context.SaveChangesAsync(cancellationToken);

            foreach (var item in pendingFlexPay)
            {
                if (item.IdUtilisateur is null or <= 0 || string.IsNullOrWhiteSpace(item.ProviderTxRef))
                    continue;

                try
                {
                    await _flexPayRealtimeNotifier.NotifyPaymentFailedAsync(
                        item.IdUtilisateur.Value,
                        item.ProviderTxRef.Trim(),
                        MessageHoldExpire,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "SignalR FlexPayPaymentFailed (hold expiré) non envoyé — Order={OrderNumber}, Reservation={IdReservation}",
                        item.ProviderTxRef,
                        item.IdEvenementReservation);
                }
            }

            _logger.LogInformation(
                "Expiration holds événement : {Count} paiement(s) FlexPay PENDING → FAILED (+ SignalR si utilisateur).",
                payments.Count);
        }

        private sealed record PendingFlexPayExpireDto(
            int IdEvenementPayment,
            int IdEvenementReservation,
            string? ProviderTxRef,
            int? IdUtilisateur);
    }
}
