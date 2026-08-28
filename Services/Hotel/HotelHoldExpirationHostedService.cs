using CongoTravel.Data;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelHoldExpirationRunner : IHotelHoldExpirationRunner
    {
        private readonly ILogger<HotelHoldExpirationRunner> _logger;
        private readonly IServiceProvider? _services;
        public HotelHoldExpirationRunner(ILogger<HotelHoldExpirationRunner> logger,
            IServiceProvider? services = null)
        {
            _logger = logger; _services = services;
        }

        public async Task ExpireHoldsAsync(CongoTravelDbContext context,
            CancellationToken cancellationToken = default)
        {
            await ExpirePlanACommandesAsync(context, cancellationToken);
            if (context.Database.IsRelational()
                && context.Database.ProviderName == "Pomelo.EntityFrameworkCore.MySql")
            {
                await context.Database.ExecuteSqlRawAsync(
                    "CALL `sp_ExpireHotelHolds`()", cancellationToken);
                return;
            }
            var now = DateTime.UtcNow;
            var holds = await context.HotelReservations.Include(r => r.Lines)
                .Where(r => r.Status == HotelReservationStatus.HOLD
                    && r.ExpiresAtUtc != null && r.ExpiresAtUtc < now)
                .ToListAsync(cancellationToken);
            foreach (var hold in holds)
            {
                if (hold.InventoryMode == HotelInventoryMode.GlobalQuota)
                {
                    var quantity = hold.Lines.Sum(l => l.Quantity);
                    var nights = await context.HotelNights.Where(n =>
                            n.IdHotel == hold.IdHotel
                            && n.NightDate >= hold.CheckInDate && n.NightDate < hold.CheckOutDate)
                        .ToListAsync(cancellationToken);
                    foreach (var night in nights)
                        night.QuantiteHold = Math.Max(0, night.QuantiteHold - quantity);
                }
                else
                {
                    foreach (var line in hold.Lines)
                    {
                        var allotments = await context.HotelNightAllotments.Where(a =>
                                a.IdHotel == hold.IdHotel && a.IdHotelRoomType == line.IdHotelRoomType
                                && a.NightDate >= hold.CheckInDate && a.NightDate < hold.CheckOutDate)
                            .ToListAsync(cancellationToken);
                        foreach (var allotment in allotments)
                            allotment.QuantiteHold = Math.Max(0, allotment.QuantiteHold - line.Quantity);
                    }
                }
                hold.Status = HotelReservationStatus.EXPIRED;
                hold.ExpiresAtUtc = null;
                hold.DateModification = now;
            }
            if (holds.Count > 0) await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("{Count} hold(s) hôtel expiré(s).", holds.Count);
        }

        private async Task ExpirePlanACommandesAsync(
            CongoTravelDbContext context, CancellationToken cancellationToken)
        {
            var commandes = _services?.GetService<IHotelCommandeFlexPayService>();
            var notifier = _services?.GetService<IFlexPayRealtimeNotifier>();
            if (commandes == null) return;
            var expired = await context.HotelCommandesEnAttente
                .Where(c => c.DateExpiration != null && c.DateExpiration < DateTime.UtcNow)
                .ToListAsync(cancellationToken);
            foreach (var commande in expired)
            {
                var user = commande.IdUtilisateur;
                var payment = await context.HotelPayments.FirstOrDefaultAsync(
                    p => p.IdHotelCommandeEnAttente == commande.IdHotelCommandeEnAttente
                         && p.Status == HotelPaymentStatus.PENDING, cancellationToken);
                var order = payment?.ProviderTxRef ?? commande.OrderNumberFlexPay;
                try { await commandes.FailCommandeAsync(commande, payment, cancellationToken); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Échec expiration commande hôtel Plan A — Commande={Id}", commande.IdHotelCommandeEnAttente);
                    continue;
                }
                if (notifier != null && user is > 0 && !string.IsNullOrWhiteSpace(order))
                {
                    try
                    {
                        if (notifier is FlexPayRealtimeNotifier concrete)
                            await concrete.NotifyPaymentFailedForDomainAsync(user.Value, order.Trim(),
                                HotelFlexPayCallbackService.HoldExpiredMessage, "hotel", cancellationToken);
                        else
                            await notifier.NotifyPaymentFailedAsync(user.Value, order.Trim(),
                                HotelFlexPayCallbackService.HoldExpiredMessage, cancellationToken);
                    }
                    catch (Exception ex) { _logger.LogWarning(ex, "SignalR expiration hôtel non envoyé."); }
                }
            }
        }
    }

    public class HotelHoldExpirationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<HotelHoldExpirationHostedService> _logger;
        public HotelHoldExpirationHostedService(IServiceScopeFactory scopeFactory,
            ILogger<HotelHoldExpirationHostedService> logger)
        {
            _scopeFactory = scopeFactory; _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<CongoTravelDbContext>();
                    var runner = scope.ServiceProvider.GetRequiredService<IHotelHoldExpirationRunner>();
                    await runner.ExpireHoldsAsync(context, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Erreur expiration holds hôtel."); }
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
