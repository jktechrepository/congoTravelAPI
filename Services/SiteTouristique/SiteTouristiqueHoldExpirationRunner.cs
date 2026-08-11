using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueHoldExpirationRunner : ISiteTouristiqueHoldExpirationRunner
    {
        private const string ExpireProcedureSql = "CALL `sp_ExpireSiteTouristiqueHolds`()";
        private const string MessageHoldExpire =
            "Hold expiré. Le paiement n’a pas été confirmé à temps.";

        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<SiteTouristiqueHoldExpirationRunner> _logger;

        public SiteTouristiqueHoldExpirationRunner(
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<SiteTouristiqueHoldExpirationRunner> logger)
        {
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            var expiredHoldIds = await context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.Status == SiteTouristiqueReservationStatus.HOLD
                            && r.ExpiresAtUtc != null
                            && r.ExpiresAtUtc < utcNow)
                .Select(r => r.IdSiteTouristiqueReservation)
                .ToListAsync(cancellationToken);

            if (expiredHoldIds.Count == 0)
            {
                _logger.LogDebug("Expiration holds site touristique : aucun HOLD expiré en attente.");
                return;
            }

            var pendingFlexPay = await (
                from p in context.SiteTouristiquePayments.AsNoTracking()
                join r in context.SiteTouristiqueReservations.AsNoTracking()
                    on p.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where expiredHoldIds.Contains(p.IdSiteTouristiqueReservation)
                      && p.Status == SiteTouristiquePaymentStatus.PENDING
                      && p.Provider == SiteTouristiqueFlexPayConstants.Provider
                select new PendingFlexPayExpireDto(
                    p.IdSiteTouristiquePayment,
                    p.IdSiteTouristiqueReservation,
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
                        "Expiration holds site touristique : {PendingCount} réservation(s) HOLD expirée(s) traitée(s) via sp_ExpireSiteTouristiqueHolds.",
                        expiredHoldIds.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec sp_ExpireSiteTouristiqueHolds ({PendingCount} HOLD en attente). Vérifier Scripts/production_site_touristique_hold_expiration_job.sql.",
                        expiredHoldIds.Count);
                }

                return;
            }

            // InMemory / tests : marque EXPIRED sans procédure SQL (inventaire non restitué ici).
            var holds = await context.SiteTouristiqueReservations
                .Where(r => expiredHoldIds.Contains(r.IdSiteTouristiqueReservation)
                            && r.Status == SiteTouristiqueReservationStatus.HOLD)
                .ToListAsync(cancellationToken);

            foreach (var hold in holds)
            {
                hold.Status = SiteTouristiqueReservationStatus.EXPIRED;
                hold.DateModification = DateTime.UtcNow;
            }

            if (holds.Count > 0)
                await context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Expiration holds site touristique (non-MySQL) : {Count} réservation(s) marquées EXPIRED.",
                holds.Count);
        }

        private async Task FailPendingFlexPayAndNotifyAsync(
            CongoTravelDbContext context,
            IReadOnlyList<PendingFlexPayExpireDto> pendingFlexPay,
            CancellationToken cancellationToken)
        {
            if (pendingFlexPay.Count == 0)
                return;

            var paymentIds = pendingFlexPay.Select(p => p.IdSiteTouristiquePayment).ToList();
            var payments = await context.SiteTouristiquePayments
                .Where(p => paymentIds.Contains(p.IdSiteTouristiquePayment)
                            && p.Status == SiteTouristiquePaymentStatus.PENDING)
                .ToListAsync(cancellationToken);

            var utcNow = DateTime.UtcNow;
            foreach (var payment in payments)
            {
                payment.Status = SiteTouristiquePaymentStatus.FAILED;
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
                        item.IdSiteTouristiqueReservation);
                }
            }

            _logger.LogInformation(
                "Expiration holds site touristique : {Count} paiement(s) FlexPay PENDING → FAILED (+ SignalR si utilisateur).",
                payments.Count);
        }

        private sealed record PendingFlexPayExpireDto(
            int IdSiteTouristiquePayment,
            int IdSiteTouristiqueReservation,
            string? ProviderTxRef,
            int? IdUtilisateur);
    }
}
