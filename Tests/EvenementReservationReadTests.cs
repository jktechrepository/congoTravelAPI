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
    public class EvenementReservationReadTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementReservationService CreateService(CongoTravelDbContext ctx) =>
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

        private static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreatePaymentService(ctx);

        [Fact]
        public async Task GetByIdAsync_returns_full_graph_for_own_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_full_graph_for_own_societe));
            var (idSociete, idReservation) = await SeedConfirmedReservationAsync(ctx, quantity: 2);
            var service = CreateService(ctx);

            var result = await service.GetByIdAsync(idReservation, idSociete);

            Assert.NotNull(result);
            Assert.Equal("CONFIRMED", result!.Status);
            Assert.NotEmpty(result.Lines);
            Assert.Equal(2, result.Tickets.Count);
            Assert.Single(result.Payments);
        }

        [Fact]
        public async Task GetByIdAsync_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_null_for_other_societe));
            var (idSociete, idReservation) = await SeedHoldReservationAsync(ctx, quantity: 1);
            var service = CreateService(ctx);

            var result = await service.GetByIdAsync(idReservation, idSociete + 999);

            Assert.Null(result);
        }

        [Fact]
        public async Task ListAsync_filters_by_status_and_session()
        {
            await using var ctx = BuildDb(nameof(ListAsync_filters_by_status_and_session));
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx);
            var holdService = CreateHoldService(ctx);

            var hold = await holdService.CreateHoldAsync(
                idSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            await CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdEvenementReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var secondHold = await holdService.CreateHoldAsync(
                idSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            var service = CreateService(ctx);
            var confirmed = await service.ListAsync(
                idSociete,
                new EvenementReservationListFilter
                {
                    Status = EvenementReservationStatus.CONFIRMED,
                    IdEvenementSession = idSession
                });

            Assert.Single(confirmed);
            Assert.Equal(hold.IdEvenementReservation, confirmed[0].IdEvenementReservation);

            var holds = await service.ListAsync(
                idSociete,
                new EvenementReservationListFilter { Status = EvenementReservationStatus.HOLD });

            Assert.Single(holds);
            Assert.Equal(secondHold.IdEvenementReservation, holds[0].IdEvenementReservation);
        }

        [Fact]
        public async Task GetByReferenceAsync_returns_matching_reservation()
        {
            await using var ctx = BuildDb(nameof(GetByReferenceAsync_returns_matching_reservation));
            var (idSociete, idReservation) = await SeedHoldReservationAsync(ctx, quantity: 1);
            var reference = await ctx.EvenementReservations
                .Where(r => r.IdEvenementReservation == idReservation)
                .Select(r => r.ReferenceReservation)
                .SingleAsync();
            var service = CreateService(ctx);

            var found = await service.GetByReferenceAsync(reference, idSociete);
            var missing = await service.GetByReferenceAsync("REF-INEXISTANTE", idSociete);

            Assert.NotNull(found);
            Assert.Equal(idReservation, found!.IdEvenementReservation);
            Assert.Null(missing);
        }

        [Fact]
        public async Task ListByDateRangeAsync_returns_reservations_in_range()
        {
            await using var ctx = BuildDb(nameof(ListByDateRangeAsync_returns_reservations_in_range));
            var idSociete = await SeedSocieteAsync(ctx);
            var (societeId, idSession) = await SeedPublishedSessionAsync(ctx, idSociete);
            var holdService = CreateHoldService(ctx);

            await holdService.CreateHoldAsync(
                idSession,
                societeId,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            var oldReservation = new EvenementReservation
            {
                IdSociete = societeId,
                IdEvenementSession = idSession,
                ReferenceReservation = "EVT-OLD",
                Status = EvenementReservationStatus.CANCELLED,
                MontantSousTotal = 10m,
                CodeDevise = "CDF",
                DateCreation = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)
            };
            ctx.EvenementReservations.Add(oldReservation);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var inRange = await service.ListByDateRangeAsync(
                DateTime.UtcNow.Date.AddDays(-1),
                DateTime.UtcNow.Date.AddDays(1),
                societeId);

            var oldOnly = await service.ListByDateRangeAsync(
                new DateTime(2026, 1, 10),
                new DateTime(2026, 1, 10),
                societeId);

            Assert.Single(inRange);
            Assert.Equal("HOLD", inRange[0].Status);
            Assert.Single(oldOnly);
            Assert.Equal("EVT-OLD", oldOnly[0].ReferenceReservation);
        }

        [Fact]
        public async Task GetTicketsByReservationAsync_returns_null_when_reservation_missing()
        {
            await using var ctx = BuildDb(nameof(GetTicketsByReservationAsync_returns_null_when_reservation_missing));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var tickets = await service.GetTicketsByReservationAsync(999, idSociete);

            Assert.Null(tickets);
        }

        [Fact]
        public async Task ListBySocieteAndSessionAsync_returns_null_when_session_not_in_societe()
        {
            await using var ctx = BuildDb(nameof(ListBySocieteAndSessionAsync_returns_null_when_session_not_in_societe));
            var (idSocieteA, idSessionA) = await SeedPublishedSessionAsync(ctx);
            var idSocieteB = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.ListBySocieteAndSessionAsync(idSocieteB, idSessionA);

            Assert.Null(result);

            var valid = await service.ListBySocieteAndSessionAsync(idSocieteA, idSessionA);
            Assert.NotNull(valid);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedHoldReservationAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, idSession) = await SeedPublishedSessionAsync(ctx);
            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                idSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (idSociete, hold.IdEvenementReservation);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedConfirmedReservationAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, idReservation) = await SeedHoldReservationAsync(ctx, quantity);
            await CreatePaymentService(ctx).ConfirmPaymentAsync(
                idReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            return (idSociete, idReservation);
        }

        private static async Task<(int IdSociete, int IdSession)> SeedPublishedSessionAsync(
            CongoTravelDbContext ctx,
            int? idSociete = null)
        {
            var societeId = idSociete ?? await SeedSocieteAsync(ctx);
            var session = new EvenementSession
            {
                IdSociete = societeId,
                CodeSession = $"READ-{Guid.NewGuid():N}"[..12],
                Libelle = "Read test",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
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
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            });
            await ctx.SaveChangesAsync();

            return (societeId, session.IdEvenementSession);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Read Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
