using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementClassQuotaConfirmCancelStrategyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreatePaymentService(ctx);

        private static EvenementReservationService CreateCancelService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new EvenementInventoryCancelStrategyFactory(
                    new EvenementGlobalQuotaCancelStrategy(ctx),
                    new EvenementClassQuotaCancelStrategy(ctx),
                    new EvenementSeatNumberedCancelStrategy(ctx)),
                Moq.Mock.Of<CongoTravel.Services.Repositories.IFlexPayRealtimeNotifier>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementReservationService>.Instance);

        private static EvenementHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

        [Fact]
        public async Task ConfirmHoldAsync_transfers_hold_to_sold_per_class_quota()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAsync_transfers_hold_to_sold_per_class_quota));
            var (session, reservation, vipQuotaId, stdQuotaId) = await SeedClassHoldReservationAsync(ctx, vipQty: 2, stdQty: 3);

            var strategy = new EvenementClassQuotaConfirmStrategy(ctx);
            await strategy.ConfirmHoldAsync(new EvenementInventoryConfirmRequest
            {
                Session = session,
                Reservation = reservation
            });

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteHold);
            Assert.Equal(2, vipQuota.QuantiteVendue);
            Assert.Equal(0, stdQuota.QuantiteHold);
            Assert.Equal(3, stdQuota.QuantiteVendue);
        }

        [Fact]
        public async Task ConfirmHoldAsync_throws_conflict_when_class_hold_insufficient()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAsync_throws_conflict_when_class_hold_insufficient));
            var (session, reservation, _, _) = await SeedClassHoldReservationAsync(ctx, vipQty: 2, stdQty: 3, vipHold: 1);

            var strategy = new EvenementClassQuotaConfirmStrategy(ctx);

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                strategy.ConfirmHoldAsync(new EvenementInventoryConfirmRequest
                {
                    Session = session,
                    Reservation = reservation
                }));
        }

        [Fact]
        public async Task ReleaseReservationAsync_releases_hold_per_class_quota()
        {
            await using var ctx = BuildDb(nameof(ReleaseReservationAsync_releases_hold_per_class_quota));
            var (session, reservation, vipQuotaId, stdQuotaId) = await SeedClassHoldReservationAsync(ctx, vipQty: 2, stdQty: 1);

            var strategy = new EvenementClassQuotaCancelStrategy(ctx);
            await strategy.ReleaseReservationAsync(new EvenementInventoryCancelRequest
            {
                Session = session,
                Reservation = reservation,
                FromConfirmedSale = false
            });

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteHold);
            Assert.Equal(0, stdQuota.QuantiteHold);
        }

        [Fact]
        public async Task ReleaseReservationAsync_releases_sold_per_class_quota()
        {
            await using var ctx = BuildDb(nameof(ReleaseReservationAsync_releases_sold_per_class_quota));
            var (session, reservation, vipQuotaId, stdQuotaId) = await SeedClassHoldReservationAsync(
                ctx, vipQty: 2, stdQty: 1, vipHold: 0, stdHold: 0, vipSold: 2, stdSold: 1);

            var strategy = new EvenementClassQuotaCancelStrategy(ctx);
            await strategy.ReleaseReservationAsync(new EvenementInventoryCancelRequest
            {
                Session = session,
                Reservation = reservation,
                FromConfirmedSale = true
            });

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteVendue);
            Assert.Equal(0, stdQuota.QuantiteVendue);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_class_quota_emits_tickets_and_transfers_stock()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_class_quota_emits_tickets_and_transfers_stock));
            var (idSociete, idReservation, vipQuotaId, stdQuotaId) = await SeedClassHoldViaServiceAsync(ctx);
            var paymentService = CreatePaymentService(ctx);

            var result = await paymentService.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
            {
                MethodePaiement = "CASH"
            });

            Assert.False(result.AlreadyConfirmed);
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(3, result.Reservation.Tickets.Count);

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteHold);
            Assert.Equal(2, vipQuota.QuantiteVendue);
            Assert.Equal(0, stdQuota.QuantiteHold);
            Assert.Equal(1, stdQuota.QuantiteVendue);
        }

        [Fact]
        public async Task CancelAsync_class_quota_releases_hold()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_class_quota_releases_hold));
            var (idSociete, idReservation, vipQuotaId, stdQuotaId) = await SeedClassHoldViaServiceAsync(ctx);
            var cancelService = CreateCancelService(ctx);

            var result = await cancelService.CancelAsync(idReservation, idSociete);

            Assert.False(result.AlreadyCancelled);
            Assert.Equal("CANCELLED", result.Reservation.Status);
            Assert.Empty(await ctx.EvenementReservations.ToListAsync());

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteHold);
            Assert.Equal(0, stdQuota.QuantiteHold);
        }

        [Fact]
        public async Task CancelAsync_class_quota_voids_tickets_and_releases_sold()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_class_quota_voids_tickets_and_releases_sold));
            var (idSociete, idReservation, vipQuotaId, stdQuotaId) = await SeedClassHoldViaServiceAsync(ctx);
            var paymentService = CreatePaymentService(ctx);
            var cancelService = CreateCancelService(ctx);

            await paymentService.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
            {
                MethodePaiement = "CASH"
            });

            var result = await cancelService.CancelAsync(idReservation, idSociete);

            Assert.False(result.AlreadyCancelled);
            Assert.Equal(3, result.TicketsVoided);
            Assert.All(result.Reservation.Tickets, t => Assert.Equal("VOID", t.Status));

            var vipQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == vipQuotaId);
            var stdQuota = await ctx.EvenementSessionClassQuotas.SingleAsync(q => q.IdEvenementSessionClassQuota == stdQuotaId);
            Assert.Equal(0, vipQuota.QuantiteVendue);
            Assert.Equal(0, stdQuota.QuantiteVendue);
        }

        private static async Task<(EvenementSession Session, EvenementReservation Reservation, int VipQuotaId, int StdQuotaId)>
            SeedClassHoldReservationAsync(
                CongoTravelDbContext ctx,
                int vipQty,
                int stdQty,
                int? vipHold = null,
                int? stdHold = null,
                int vipSold = 0,
                int stdSold = 0)
        {
            var vipHoldDb = vipHold ?? vipQty;
            var stdHoldDb = stdHold ?? stdQty;
            var societe = new Societe { Nom = "Class Confirm", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var vip = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var std = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "STD",
                Libelle = "Standard",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.AddRange(vip, std);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "CLASS-CONF",
                Libelle = "Class confirm",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.ClassQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var vipQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = vip.IdEvenementClasse,
                CapaciteTotale = 20,
                QuantiteHold = vipHoldDb,
                QuantiteVendue = vipSold,
                PrixUnitaire = 50m,
                CodeDevise = "USD"
            };
            var stdQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = std.IdEvenementClasse,
                CapaciteTotale = 30,
                QuantiteHold = stdHoldDb,
                QuantiteVendue = stdSold,
                PrixUnitaire = 15m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionClassQuotas.AddRange(vipQuota, stdQuota);
            await ctx.SaveChangesAsync();

            var reservation = new EvenementReservation
            {
                IdSociete = societe.IdSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-CLASS-CONF",
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                MontantSousTotal = vipQty * 50m + stdQty * 15m,
                CodeDevise = "USD",
                DateCreation = DateTime.UtcNow,
                Lines =
                {
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.ClassQuota,
                        Quantite = vipQty,
                        PrixUnitaire = 50m,
                        CodeDevise = "USD",
                        IdEvenementSessionClassQuota = vipQuota.IdEvenementSessionClassQuota
                    },
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.ClassQuota,
                        Quantite = stdQty,
                        PrixUnitaire = 15m,
                        CodeDevise = "USD",
                        IdEvenementSessionClassQuota = stdQuota.IdEvenementSessionClassQuota
                    }
                }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            return (session, reservation, vipQuota.IdEvenementSessionClassQuota, stdQuota.IdEvenementSessionClassQuota);
        }

        private static async Task<(int IdSociete, int IdReservation, int VipQuotaId, int StdQuotaId)>
            SeedClassHoldViaServiceAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Class Svc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var vip = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var std = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "STD",
                Libelle = "Standard",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.AddRange(vip, std);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "CLASS-SVC",
                Libelle = "Class svc",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.ClassQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var vipQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = vip.IdEvenementClasse,
                CapaciteTotale = 20,
                PrixUnitaire = 50m,
                CodeDevise = "USD"
            };
            var stdQuota = new EvenementSessionClassQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                IdEvenementClasse = std.IdEvenementClasse,
                CapaciteTotale = 30,
                PrixUnitaire = 15m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionClassQuotas.AddRange(vipQuota, stdQuota);
            await ctx.SaveChangesAsync();

            var holdService = CreateHoldService(ctx);
            var hold = await holdService.CreateHoldAsync(session.IdEvenementSession, societe.IdSociete, new EvenementHoldRequestDto
            {
                Items = new List<EvenementHoldItemRequestDto>
                {
                    new() { ClassId = vip.IdEvenementClasse, Quantity = 2 },
                    new() { ClassId = std.IdEvenementClasse, Quantity = 1 }
                }
            });

            return (societe.IdSociete, hold.IdEvenementReservation, vipQuota.IdEvenementSessionClassQuota, stdQuota.IdEvenementSessionClassQuota);
        }
    }
}
