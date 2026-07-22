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
    public class EvenementPaymentServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreatePaymentService(ctx);

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
        public async Task ConfirmPaymentAsync_confirms_hold_emits_tickets_and_transfers_stock()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_confirms_hold_emits_tickets_and_transfers_stock));
            var (idSociete, idSession, idReservation) = await SeedHoldAsync(ctx, quantity: 3);
            var service = CreatePaymentService(ctx);

            var result = await service.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
            {
                MethodePaiement = "CASH",
                ReferenceTransaction = "CAISSE-001"
            });

            Assert.False(result.AlreadyConfirmed);
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal("SUCCEEDED", result.Payment.Status);
            Assert.Equal("CASH", result.Payment.Provider);
            Assert.Equal("CAISSE-001", result.Payment.ProviderTxRef);
            Assert.Equal(60m, result.Payment.Montant);
            Assert.Equal(3, result.Reservation.Tickets.Count);
            Assert.All(result.Reservation.Tickets, t => Assert.Equal("ISSUED", t.Status));
            Assert.All(result.Reservation.Tickets, t =>
                Assert.True(t.TicketCode.StartsWith("EVT-TKT-", StringComparison.Ordinal)));

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(3, quota.QuantiteVendue);
        }

        [Fact]
        public async Task ConfirmPaymentAsync_returns_already_confirmed_on_second_call()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_returns_already_confirmed_on_second_call));
            var (idSociete, _, idReservation) = await SeedHoldAsync(ctx, quantity: 2);
            var service = CreatePaymentService(ctx);
            var request = new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" };

            var first = await service.ConfirmPaymentAsync(idReservation, idSociete, request);
            var second = await service.ConfirmPaymentAsync(idReservation, idSociete, request);

            Assert.False(first.AlreadyConfirmed);
            Assert.True(second.AlreadyConfirmed);
            Assert.Equal(first.Payment.IdEvenementPayment, second.Payment.IdEvenementPayment);
            Assert.Equal(1, await ctx.EvenementPayments.CountAsync());
            Assert.Equal(2, await ctx.EvenementTickets.CountAsync());
        }

        [Fact]
        public async Task ConfirmPaymentAsync_replays_payment_idempotency_key()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_replays_payment_idempotency_key));
            var (idSociete, _, idReservation) = await SeedHoldAsync(ctx, quantity: 1);
            var service = CreatePaymentService(ctx);
            var request = new EvenementConfirmPaymentRequestDto
            {
                MethodePaiement = "CASH",
                IdempotencyKey = "pay-idem-001"
            };

            var first = await service.ConfirmPaymentAsync(idReservation, idSociete, request);
            var second = await service.ConfirmPaymentAsync(idReservation, idSociete, request);

            Assert.True(second.AlreadyConfirmed);
            Assert.Equal(first.Payment.ReferencePaiement, second.Payment.ReferencePaiement);
            Assert.Equal(1, await ctx.EvenementPayments.CountAsync());
        }

        [Fact]
        public async Task ConfirmPaymentAsync_rejects_expired_hold()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_rejects_expired_hold));
            var (idSociete, idReservation) = await SeedExpiredHoldAsync(ctx);
            var service = CreatePaymentService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
                {
                    MethodePaiement = "CASH"
                }));
        }

        [Fact]
        public async Task ConfirmPaymentAsync_rejects_non_cash_method()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_rejects_non_cash_method));
            var (idSociete, _, idReservation) = await SeedHoldAsync(ctx, quantity: 1);
            var service = CreatePaymentService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
                {
                    MethodePaiement = "FLEXPAY"
                }));
        }

        [Fact]
        public async Task ConfirmPaymentAsync_throws_conflict_when_hold_stock_missing()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_throws_conflict_when_hold_stock_missing));
            var (idSociete, idReservation) = await SeedHoldWithDrainedQuotaAsync(ctx, quantity: 2);
            var service = CreatePaymentService(ctx);

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                service.ConfirmPaymentAsync(idReservation, idSociete, new EvenementConfirmPaymentRequestDto
                {
                    MethodePaiement = "CASH"
                }));
        }

        private static async Task<(int IdSociete, int IdSession, int IdReservation)> SeedHoldAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var societe = new Societe { Nom = "EVT Pay", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"PAY-{Guid.NewGuid():N}"[..10],
                Libelle = "Payment test",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            ctx.EvenementSessionGlobalQuotas.Add(new EvenementSessionGlobalQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                CapaciteTotale = 50,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            });
            await ctx.SaveChangesAsync();

            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                session.IdEvenementSession,
                societe.IdSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (societe.IdSociete, session.IdEvenementSession, hold.IdEvenementReservation);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedExpiredHoldAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "EVT Expired", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "EXP-1",
                Libelle = "Expired",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var reservation = new EvenementReservation
            {
                IdSociete = societe.IdSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-RES-TEST-EXP",
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
                MontantSousTotal = 20m,
                CodeDevise = "USD",
                DateCreation = DateTime.UtcNow,
                Lines =
                {
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.GlobalQuota,
                        Quantite = 1,
                        PrixUnitaire = 20m,
                        CodeDevise = "USD"
                    }
                }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, reservation.IdEvenementReservation);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedHoldWithDrainedQuotaAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, _, idReservation) = await SeedHoldAsync(ctx, quantity);
            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            quota.QuantiteHold = 0;
            await ctx.SaveChangesAsync();
            return (idSociete, idReservation);
        }
    }
}
