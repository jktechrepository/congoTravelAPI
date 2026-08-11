using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantHoldExpirationRunner : IRestaurantHoldExpirationRunner
    {
        private const string ExpireProcedureSql = "CALL `sp_ExpireRestaurantHolds`()";
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<RestaurantHoldExpirationRunner> _logger;

        public RestaurantHoldExpirationRunner(
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<RestaurantHoldExpirationRunner> logger)
        {
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var expiredHoldIds = await context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.Status == RestaurantReservationStatus.HOLD
                            && r.ExpiresAtUtc != null
                            && r.ExpiresAtUtc < utcNow)
                .Select(r => r.IdRestaurantReservation)
                .ToListAsync(cancellationToken);

            if (expiredHoldIds.Count == 0)
            {
                _logger.LogDebug("Expiration holds restaurant : aucun HOLD expiré en attente.");
                return;
            }

            var pendingFlexPay = await (
                from p in context.RestaurantPayments.AsNoTracking()
                join r in context.RestaurantReservations.AsNoTracking()
                    on p.IdRestaurantReservation equals r.IdRestaurantReservation
                where expiredHoldIds.Contains(p.IdRestaurantReservation)
                      && p.Status == RestaurantPaymentStatus.PENDING
                      && p.Provider == RestaurantFlexPayConstants.Provider
                select new PendingFlexPayExpireDto(
                    p.IdRestaurantPayment,
                    p.IdRestaurantReservation,
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
                        "Expiration holds restaurant : {PendingCount} réservation(s) HOLD expirée(s) traitée(s) via sp_ExpireRestaurantHolds.",
                        expiredHoldIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec sp_ExpireRestaurantHolds ({PendingCount} HOLD en attente). Vérifier Scripts/production_restaurant_hold_expiration_procedure_only.sql.",
                        expiredHoldIds.Count);
                }

                return;
            }

            // InMemory / tests : EXPIRED + restitution QuantiteHold (Global + Zone)
            var holds = await context.RestaurantReservations
                .Include(r => r.Lines)
                .Where(r => expiredHoldIds.Contains(r.IdRestaurantReservation)
                            && r.Status == RestaurantReservationStatus.HOLD)
                .ToListAsync(cancellationToken);

            foreach (var hold in holds)
            {
                var globalQty = hold.Lines
                    .Where(l => l.LineType == RestaurantReservationLineType.GlobalQuota)
                    .Sum(l => l.Quantite);

                if (globalQty > 0)
                {
                    var quota = await context.RestaurantCreneauGlobalQuotas
                        .FirstOrDefaultAsync(
                            g => g.IdRestaurantCreneau == hold.IdRestaurantCreneau,
                            cancellationToken);

                    if (quota != null && quota.QuantiteHold >= globalQty)
                        quota.QuantiteHold -= globalQty;
                    else if (quota != null)
                        quota.QuantiteHold = Math.Max(0, quota.QuantiteHold - globalQty);
                }

                foreach (var line in hold.Lines.Where(l =>
                             l.LineType == RestaurantReservationLineType.ClassQuota
                             && l.IdRestaurantCreneauZoneQuota.HasValue))
                {
                    var zoneQuota = await context.RestaurantCreneauZoneQuotas
                        .FirstOrDefaultAsync(
                            q => q.IdRestaurantCreneauZoneQuota == line.IdRestaurantCreneauZoneQuota!.Value,
                            cancellationToken);

                    if (zoneQuota == null)
                        continue;

                    if (zoneQuota.QuantiteHold >= line.Quantite)
                        zoneQuota.QuantiteHold -= line.Quantite;
                    else
                        zoneQuota.QuantiteHold = Math.Max(0, zoneQuota.QuantiteHold - line.Quantite);
                }

                hold.Status = RestaurantReservationStatus.EXPIRED;
                hold.DateModification = DateTime.UtcNow;
            }

            if (holds.Count > 0)
                await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Expiration holds restaurant (non-MySQL) : {Count} réservation(s) marquées EXPIRED avec restitution hold.",
                holds.Count);
        }

        private async Task FailPendingFlexPayAndNotifyAsync(
            CongoTravelDbContext context,
            IReadOnlyList<PendingFlexPayExpireDto> pendingFlexPay,
            CancellationToken cancellationToken)
        {
            if (pendingFlexPay.Count == 0)
                return;

            var paymentIds = pendingFlexPay.Select(p => p.IdRestaurantPayment).ToList();
            var payments = await context.RestaurantPayments
                .Where(p => paymentIds.Contains(p.IdRestaurantPayment)
                            && p.Status == RestaurantPaymentStatus.PENDING)
                .ToListAsync(cancellationToken);

            var utcNow = DateTime.UtcNow;
            foreach (var payment in payments)
            {
                payment.Status = RestaurantPaymentStatus.FAILED;
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
                        item.IdRestaurantReservation);
                }
            }

            _logger.LogInformation(
                "Expiration holds restaurant : {Count} paiement(s) FlexPay PENDING → FAILED (+ SignalR si utilisateur).",
                payments.Count);
        }

        private sealed record PendingFlexPayExpireDto(
            int IdRestaurantPayment,
            int IdRestaurantReservation,
            string? ProviderTxRef,
            int? IdUtilisateur);
    }
}
