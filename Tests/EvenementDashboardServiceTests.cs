using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementDashboardServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task GetSocieteDashboardAsync_returns_metrics_after_cash_confirm()
        {
            await using var ctx = BuildDb(nameof(GetSocieteDashboardAsync_returns_metrics_after_cash_confirm));
            var (idSociete, idReservation) = await SeedConfirmedReservationAsync(ctx, quantity: 2, provider: "CASH");
            var service = new EvenementDashboardService(ctx, NullLogger<EvenementDashboardService>.Instance);
            var (monthStart, monthEnd) = GetCurrentMonthRange();

            var dashboard = await service.GetSocieteDashboardAsync(idSociete, monthStart, monthEnd);

            Assert.Equal(idSociete, dashboard.IdSociete);
            Assert.Equal(1, dashboard.Summary.SessionsPubliees);
            Assert.Equal(1, dashboard.Summary.ReservationsConfirmeesMois);
            Assert.Equal(2, dashboard.Summary.TicketsEmisMois);
            Assert.Equal(1, dashboard.Reservations.Confirmed);
            Assert.Contains(dashboard.RevenuParProvider, r => r.Provider == "CASH" && r.Montant == 40m);
            Assert.Single(dashboard.Top5SessionsCa);
            Assert.Equal(2, dashboard.Top5SessionsCa[0].TicketsVendus);
            Assert.NotEmpty(dashboard.ReservationsRecentes);
            Assert.NotEmpty(dashboard.PaiementsRecents);
        }

        [Fact]
        public async Task GetSocieteDashboardAsync_splits_cash_and_flexpay_revenue()
        {
            await using var ctx = BuildDb(nameof(GetSocieteDashboardAsync_splits_cash_and_flexpay_revenue));
            var (idSociete, _) = await SeedConfirmedReservationAsync(ctx, quantity: 1, provider: "CASH", unitPrice: 20m);
            await SeedConfirmedReservationAsync(ctx, quantity: 1, provider: "FLEXPAY", unitPrice: 30m, reuseSocieteId: idSociete);
            var service = new EvenementDashboardService(ctx, NullLogger<EvenementDashboardService>.Instance);
            var (monthStart, monthEnd) = GetCurrentMonthRange();

            var dashboard = await service.GetSocieteDashboardAsync(idSociete, monthStart, monthEnd);

            Assert.Equal(2, dashboard.Reservations.Confirmed);
            Assert.Contains(dashboard.RevenuParProvider, r => r.Provider == "CASH" && r.Montant == 20m);
            Assert.Contains(dashboard.RevenuParProvider, r => r.Provider == "FLEXPAY" && r.Montant == 30m);
        }

        [Fact]
        public async Task GetSuperAdminDashboardAsync_aggregates_societes()
        {
            await using var ctx = BuildDb(nameof(GetSuperAdminDashboardAsync_aggregates_societes));
            await SeedConfirmedReservationAsync(ctx, quantity: 1, provider: "CASH");
            await SeedConfirmedReservationAsync(ctx, quantity: 2, provider: "CASH");
            var service = new EvenementDashboardService(ctx, NullLogger<EvenementDashboardService>.Instance);
            var (monthStart, monthEnd) = GetCurrentMonthRange();

            var dashboard = await service.GetSuperAdminDashboardAsync(monthStart, monthEnd);

            Assert.Equal(2, dashboard.Global.TotalSocietesActives);
            Assert.Equal(2, dashboard.Societes.Count);
            Assert.Equal(2, dashboard.Global.ReservationsConfirmeesMois);
            Assert.Equal(3, dashboard.Global.TicketsEmisMois);
        }

        private static (DateTime MonthStart, DateTime MonthEnd) GetCurrentMonthRange()
        {
            var (_, monthStart, _) = SocieteTransportMetricsHelper.GetUtcBoundaries(DateTime.UtcNow);
            return (monthStart, monthStart.AddMonths(1));
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedConfirmedReservationAsync(
            CongoTravelDbContext ctx,
            int quantity,
            string provider,
            decimal unitPrice = 20m,
            int? reuseSocieteId = null)
        {
            int idSociete;
            int idSite;
            int idReservation;

            if (reuseSocieteId.HasValue)
            {
                idSociete = reuseSocieteId.Value;
                idSite = await ctx.Sites.Where(s => s.IdSociete == idSociete).Select(s => s.IdSite).FirstAsync();

                var session = new Models.Evenement.EvenementSession
                {
                    IdSociete = idSociete,
                    CodeSession = $"DB-{Guid.NewGuid():N}"[..10],
                    Libelle = "Dashboard session 2",
                    StartAtUtc = DateTime.UtcNow.AddHours(-1),
                    EndAtUtc = DateTime.UtcNow.AddHours(6),
                    InventoryMode = EvenementInventoryMode.GlobalQuota,
                    Status = EvenementSessionStatus.Published,
                    DateCreation = DateTime.UtcNow
                };
                ctx.EvenementSessions.Add(session);
                await ctx.SaveChangesAsync();

                ctx.EvenementSessionGlobalQuotas.Add(new Models.Evenement.EvenementSessionGlobalQuota
                {
                    IdEvenementSession = session.IdEvenementSession,
                    CapaciteTotale = 20,
                    PrixUnitaire = unitPrice,
                    CodeDevise = "USD"
                });
                await ctx.SaveChangesAsync();

                var holdService = new EvenementHoldService(
                    ctx,
                    new EvenementInventoryHoldStrategyFactory(
                        new EvenementGlobalQuotaHoldStrategy(ctx),
                        new EvenementClassQuotaHoldStrategy(ctx),
                        new EvenementSeatNumberedHoldStrategy(ctx)),
                    new ConfigSocieteService(ctx),
                    NullLogger<EvenementHoldService>.Instance);

                var hold = await holdService.CreateHoldAsync(
                    session.IdEvenementSession,
                    idSociete,
                    new EvenementHoldRequestDto
                    {
                        Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                    });
                idReservation = hold.IdEvenementReservation;
            }
            else
            {
                (idSociete, idSite, idReservation) =
                    await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity);

                var reservationForPricing = await ctx.EvenementReservations
                    .SingleAsync(r => r.IdEvenementReservation == idReservation);
                var session = await ctx.EvenementSessions
                    .SingleAsync(s => s.IdEvenementSession == reservationForPricing.IdEvenementSession);
                session.StartAtUtc = DateTime.UtcNow.AddHours(-1);
                session.EndAtUtc = DateTime.UtcNow.AddHours(6);
                var quota = await ctx.EvenementSessionGlobalQuotas
                    .SingleAsync(q => q.IdEvenementSession == session.IdEvenementSession);
                quota.PrixUnitaire = unitPrice;
                reservationForPricing.MontantSousTotal = unitPrice * quantity;
                await ctx.SaveChangesAsync();
            }

            if (string.Equals(provider, "CASH", StringComparison.OrdinalIgnoreCase))
            {
                await EvenementTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                    idReservation,
                    idSociete,
                    new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });
            }
            else
            {
                var reservation = await ctx.EvenementReservations.SingleAsync(r => r.IdEvenementReservation == idReservation);
                ctx.EvenementPayments.Add(new Models.Evenement.EvenementPayment
                {
                    IdEvenementReservation = idReservation,
                    ReferencePaiement = $"EVT-PAY-{Guid.NewGuid():N}"[..20],
                    Provider = "FLEXPAY",
                    ProviderTxRef = $"FP-{Guid.NewGuid():N}"[..12],
                    Status = EvenementPaymentStatus.SUCCEEDED,
                    Montant = unitPrice * quantity,
                    CodeDevise = reservation.CodeDevise,
                    MontantTarif = unitPrice * quantity,
                    CodeDeviseTarif = reservation.CodeDevise,
                    TauxVersDevisePaiement = 1m,
                    DateCreation = DateTime.UtcNow
                });
                reservation.Status = EvenementReservationStatus.CONFIRMED;
                reservation.DateModification = DateTime.UtcNow;
                await ctx.SaveChangesAsync();

                for (var i = 0; i < quantity; i++)
                {
                    var line = await ctx.EvenementReservationLines
                        .FirstAsync(l => l.IdEvenementReservation == idReservation);
                    ctx.EvenementTickets.Add(new Models.Evenement.EvenementTicket
                    {
                        IdEvenementReservationLine = line.IdEvenementReservationLine,
                        TicketCode = $"EVT-TKT-TEST-{Guid.NewGuid():N}"[..24],
                        Status = EvenementTicketStatus.ISSUED,
                        IssuedAtUtc = DateTime.UtcNow
                    });
                }

                await ctx.SaveChangesAsync();
            }

            return (idSociete, idReservation);
        }
    }
}
