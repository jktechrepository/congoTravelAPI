using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementModeASmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ModeA_journey_publish_hold_confirm_check_use_availability()
        {
            await using var ctx = BuildDb(nameof(ModeA_journey_publish_hold_confirm_check_use_availability));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);

            var sessionService = PhotoStorageTestFactory.CreateEvenementSessionService(ctx);
            var holdService = CreateHoldService(ctx);
            var availabilityService = new EvenementAvailabilityService(ctx, NullLogger<EvenementAvailabilityService>.Instance);
            var paymentService = CreatePaymentService(ctx);
            var ticketService = new EvenementTicketService(ctx, new ConfigSocieteService(ctx), NullLogger<EvenementTicketService>.Instance);

            var draft = await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "SMOKE-A-2026",
                IdSite = idSite,
                Libelle = "Smoke Mode A",
                StartAtUtc = DateTime.UtcNow.AddHours(2),
                EndAtUtc = DateTime.UtcNow.AddHours(8),
                InventoryMode = "SeatNumbered",
                Sections = new List<EvenementCreateSessionSectionDto>
                {
                    new()
                    {
                        CodeSection = "ORCH",
                        Libelle = "Orchestre",
                        Seats = new List<EvenementCreateSessionSeatDto>
                        {
                            new() { SeatCode = "A-01", PrixUnitaire = 50m, CodeDevise = "USD" },
                            new() { SeatCode = "A-02", PrixUnitaire = 50m, CodeDevise = "USD" }
                        }
                    }
                }
            }, idSociete);

            var published = await sessionService.PublishAsync(draft.IdEvenementSession, idSociete);
            var seatA = published.Seats.Single(s => s.SeatCode == "A-01");

            var availabilityInitial = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession, idSociete);
            Assert.Equal(2, availabilityInitial!.Seats!.Count(s => s.SeatStatus == "Available"));

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { SeatId = seatA.IdEvenementSessionSeat, Quantity = 1 }
                    }
                });

            var availabilityAfterHold = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession, idSociete);
            Assert.Equal("Held", availabilityAfterHold!.Seats!.Single(s => s.SeatCode == "A-01").SeatStatus);
            Assert.Equal("Available", availabilityAfterHold.Seats.Single(s => s.SeatCode == "A-02").SeatStatus);

            var confirm = await paymentService.ConfirmPaymentAsync(
                hold.IdEvenementReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            Assert.Single(confirm.Reservation.Tickets);
            var ticketCode = confirm.Reservation.Tickets[0].TicketCode;

            // Ouvre la fenêtre d'entrée (vente déjà clôturée si StartAtUtc passé).
            var sessionEntity = await ctx.EvenementSessions.SingleAsync(
                s => s.IdEvenementSession == published.IdEvenementSession);
            sessionEntity.StartAtUtc = DateTime.UtcNow.AddHours(-1);
            sessionEntity.EndAtUtc = DateTime.UtcNow.AddHours(6);
            await ctx.SaveChangesAsync();

            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);

            var use = await ticketService.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal("USED", use.Response!.Ticket.Status);

            var availabilityAfterSale = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession, idSociete);
            Assert.Equal("Sold", availabilityAfterSale!.Seats!.Single(s => s.SeatCode == "A-01").SeatStatus);
        }

        [Fact]
        public async Task ModeA_cancel_hold_restores_seat()
        {
            await using var ctx = BuildDb(nameof(ModeA_cancel_hold_restores_seat));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var sessionService = PhotoStorageTestFactory.CreateEvenementSessionService(ctx);
            var holdService = CreateHoldService(ctx);
            var cancelService = CreateCancelService(ctx);
            var availabilityService = new EvenementAvailabilityService(ctx, NullLogger<EvenementAvailabilityService>.Instance);

            var published = await sessionService.PublishAsync(
                (await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "CANCEL-A",
                IdSite = idSite,
                    Libelle = "Cancel A",
                    StartAtUtc = DateTime.UtcNow.AddDays(1),
                    InventoryMode = "SeatNumbered",
                    Seats = new List<EvenementCreateSessionSeatDto>
                    {
                        new() { SeatCode = "C-01", PrixUnitaire = 30m, CodeDevise = "CDF" }
                    }
                }, idSociete)).IdEvenementSession,
                idSociete);

            var seat = published.Seats.Single();
            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { SeatId = seat.IdEvenementSessionSeat, Quantity = 1 }
                    }
                });

            await cancelService.CancelAsync(hold.IdEvenementReservation, idSociete);

            var availability = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession, idSociete);
            Assert.Equal("Available", availability!.Seats!.Single().SeatStatus);
        }

        private static EvenementHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

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
                NullLogger<EvenementReservationService>.Instance);

        private static async Task<(int IdSociete, int IdSite)> SeedSocieteAsync(
            CongoTravelDbContext ctx,
            string nom = "Test Societe") =>
            await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, nom);
    }
}
