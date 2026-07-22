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
    public class EvenementSeatNumberedHoldStrategyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ReserveHoldAsync_marks_seats_held()
        {
            await using var ctx = BuildDb(nameof(ReserveHoldAsync_marks_seats_held));
            var (session, seatA, seatB) = await SeedPublishedSeatSessionAsync(ctx);

            var strategy = new EvenementSeatNumberedHoldStrategy(ctx);
            var result = await strategy.ReserveHoldAsync(new EvenementInventoryHoldRequest
            {
                Session = session,
                HoldExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                Items = new[]
                {
                    new EvenementHoldItemRequestDto { SeatId = seatA, Quantity = 1 },
                    new EvenementHoldItemRequestDto { SeatId = seatB, Quantity = 1 }
                }
            });

            Assert.Equal(2, result.Lines.Count);
            Assert.Equal(40m, result.MontantSousTotal);
            Assert.All(result.Lines, l => Assert.Equal(1, l.Quantite));

            var seats = await ctx.EvenementSessionSeats.OrderBy(s => s.SeatCode).ToListAsync();
            Assert.All(seats, s => Assert.Equal(Models.Evenement.Enums.EvenementSessionSeatStatus.Held, s.SeatStatus));
        }

        [Fact]
        public async Task CreateHoldAsync_creates_seat_hold_via_service()
        {
            await using var ctx = BuildDb(nameof(CreateHoldAsync_creates_seat_hold_via_service));
            var (idSociete, idSession, seatA, _) = await SeedPublishedSeatSessionForHoldAsync(ctx);
            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

            var hold = await holdService.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
            {
                Items = new List<EvenementHoldItemRequestDto>
                {
                    new() { SeatId = seatA, Quantity = 1 }
                }
            });

            Assert.Equal(20m, hold.AmountPreview);
            var line = await ctx.EvenementReservationLines.SingleAsync();
            Assert.Equal(Models.Evenement.Enums.EvenementReservationLineType.Seat, line.LineType);
            Assert.Equal(seatA, line.IdEvenementSessionSeat);

            var seat = await ctx.EvenementSessionSeats.SingleAsync(s => s.IdEvenementSessionSeat == seatA);
            Assert.Equal(Models.Evenement.Enums.EvenementSessionSeatStatus.Held, seat.SeatStatus);
            Assert.Equal(hold.IdEvenementReservation, seat.IdEvenementReservationCourante);
        }

        private static async Task<(Models.Evenement.EvenementSession Session, int SeatAId, int SeatBId)>
            SeedPublishedSeatSessionAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Seat Hold", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new Models.Evenement.EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "SEAT-HOLD",
                Libelle = "Seat hold",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = Models.Evenement.Enums.EvenementInventoryMode.SeatNumbered,
                Status = Models.Evenement.Enums.EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var seatA = new Models.Evenement.EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "A-01",
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            };
            var seatB = new Models.Evenement.EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "A-02",
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionSeats.AddRange(seatA, seatB);
            await ctx.SaveChangesAsync();

            session.Seats.Add(seatA);
            session.Seats.Add(seatB);
            return (session, seatA.IdEvenementSessionSeat, seatB.IdEvenementSessionSeat);
        }

        private static async Task<(int IdSociete, int IdSession, int SeatAId, int SeatBId)>
            SeedPublishedSeatSessionForHoldAsync(CongoTravelDbContext ctx)
        {
            var (session, seatA, seatB) = await SeedPublishedSeatSessionAsync(ctx);
            return (session.IdSociete, session.IdEvenementSession, seatA, seatB);
        }
    }
}
