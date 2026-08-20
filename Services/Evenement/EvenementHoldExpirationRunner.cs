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
        private readonly IEvenementReservationService _reservationService;
        private readonly IEvenementCommandeFlexPayService _commandeFlexPayService;
        private readonly ILogger<EvenementHoldExpirationRunner> _logger;

        public EvenementHoldExpirationRunner(
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            IEvenementReservationService reservationService,
            IEvenementCommandeFlexPayService commandeFlexPayService,
            ILogger<EvenementHoldExpirationRunner> logger)
        {
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _reservationService = reservationService;
            _commandeFlexPayService = commandeFlexPayService;
            _logger = logger;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default)
        {
            await ExpirePlanACommandesAsync(context, cancellationToken);
            await ExpireLegacyReservationHoldsAsync(context, cancellationToken);
        }

        private async Task ExpirePlanACommandesAsync(
            CongoTravelDbContext context,
            CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            var expiredCommandes = await context.EvenementCommandesEnAttente
                .Where(c => c.DateExpiration != null && c.DateExpiration < utcNow)
                .ToListAsync(cancellationToken);

            if (expiredCommandes.Count == 0)
                return;

            foreach (var commande in expiredCommandes)
            {
                var payment = commande.IdPaiementEnAttente.HasValue
                    ? await context.EvenementPayments
                        .FirstOrDefaultAsync(
                            p => p.IdEvenementPayment == commande.IdPaiementEnAttente.Value,
                            cancellationToken)
                    : await context.EvenementPayments
                        .FirstOrDefaultAsync(
                            p => p.IdEvenementCommandeEnAttente == commande.IdEvenementCommandeEnAttente
                                 && p.Status == EvenementPaymentStatus.PENDING,
                            cancellationToken);

                var orderNumber = payment?.ProviderTxRef ?? commande.OrderNumberFlexPay;
                var idUtilisateur = commande.IdUtilisateur;

                try
                {
                    await _commandeFlexPayService.FailCommandeAsync(commande, payment, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec expiration commande événement Plan A — Commande={IdCommande}",
                        commande.IdEvenementCommandeEnAttente);
                    continue;
                }

                if (idUtilisateur is null or <= 0 || string.IsNullOrWhiteSpace(orderNumber))
                    continue;

                try
                {
                    await _flexPayRealtimeNotifier.NotifyPaymentFailedAsync(
                        idUtilisateur.Value,
                        orderNumber.Trim(),
                        MessageHoldExpire,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "SignalR FlexPayPaymentFailed (commande expirée) non envoyé — Order={OrderNumber}, Commande={IdCommande}",
                        orderNumber,
                        commande.IdEvenementCommandeEnAttente);
                }
            }

            _logger.LogInformation(
                "Expiration commandes événement Plan A : {Count} commande(s) expirée(s).",
                expiredCommandes.Count);
        }

        private async Task ExpireLegacyReservationHoldsAsync(
            CongoTravelDbContext context,
            CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;

            var expiredHolds = await context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.Status == EvenementReservationStatus.HOLD
                            && r.ExpiresAtUtc != null
                            && r.ExpiresAtUtc < utcNow)
                .Select(r => new { r.IdEvenementReservation, r.IdSociete })
                .ToListAsync(cancellationToken);

            if (expiredHolds.Count == 0)
            {
                _logger.LogDebug("Expiration holds événement : aucun HOLD legacy expiré en attente.");
                return;
            }

            var expiredHoldIds = expiredHolds.Select(h => h.IdEvenementReservation).ToList();

            var pendingFlexPay = await (
                from p in context.EvenementPayments.AsNoTracking()
                join r in context.EvenementReservations.AsNoTracking()
                    on p.IdEvenementReservation equals r.IdEvenementReservation
                where p.IdEvenementReservation != null
                      && expiredHoldIds.Contains(p.IdEvenementReservation.Value)
                      && p.Status == EvenementPaymentStatus.PENDING
                      && p.Provider == EvenementFlexPayConstants.Provider
                select new PendingFlexPayExpireDto(
                    p.IdEvenementPayment,
                    p.IdEvenementReservation!.Value,
                    p.ProviderTxRef,
                    r.IdUtilisateur)).ToListAsync(cancellationToken);

            await ExpireHoldsInventoryAsync(context, expiredHoldIds, cancellationToken);
            await FailPendingFlexPayAndNotifyAsync(context, pendingFlexPay, cancellationToken);

            foreach (var hold in expiredHolds)
            {
                await _reservationService.PurgeNeverConfirmedAsync(
                    hold.IdEvenementReservation,
                    hold.IdSociete,
                    cancellationToken);
            }
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
