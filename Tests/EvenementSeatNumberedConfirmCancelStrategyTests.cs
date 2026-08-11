using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
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
    public class EvenementSeatNumberedConfirmCancelStrategyTests
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
                NullLogger<EvenementReservationService>.Instance);

        private static EvenementHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

        [Fact]
        public async Task ConfirmHoldAsync_transfers_held_to_sold()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAsync_transfers_held_to_sold));
            var (session, reservation, seatA, seatB) = await SeedSeatHoldReservationAsync(ctx);

            var strategy = new EvenementSeatNumberedConfirmStrategy(ctx);
            await strategy.ConfirmHoldAsync(new EvenementInventoryConfirmRequest
            {
                Session = session,
                Reservation = reservation
            });

            var seats = await ctx.EvenementSessionSeats.OrderBy(s => s.SeatCode).ToListAsync();
            Assert.All(seats, s => Assert.Equal(EvenementSessionSeatStatus.Sold, s.SeatStatus));
            Assert.All(seats, s => Assert.Null(s.HoldExpireAtUtc));
        }

        [Fact]
        public async Task ConfirmPaymentAsync_seat_numbered_emits_tickets()
        {
            await using var ctx = BuildDb(nameof(ConfirmPaymentAsync_seat_numbered_emits_tickets));
            var (idSociete, idReservation, seatA, _) = await SeedSeatHoldViaServiceAsync(ctx);

            var result = await CreatePaymentService(ctx).ConfirmPaymentAsync(
                idReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            Assert.Equal(2, result.Reservation.Tickets.Count);
            var seat = await ctx.EvenementSessionSeats.SingleAsync(s => s.IdEvenementSessionSeat == seatA);
            Assert.Equal(EvenementSessionSeatStatus.Sold, seat.SeatStatus);
        }

        [Fact]
        public async Task CancelAsync_seat_hold_restores_availability()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_seat_hold_restores_availability));
            var (idSociete, idReservation, seatA, _) = await SeedSeatHoldViaServiceAsync(ctx);

            await CreateCancelService(ctx).CancelAsync(idReservation, idSociete);

            var seat = await ctx.EvenementSessionSeats.SingleAsync(s => s.IdEvenementSessionSeat == seatA);
            Assert.Equal(EvenementSessionSeatStatus.Available, seat.SeatStatus);
            Assert.Null(seat.IdEvenementReservationCourante);
        }

        private static async Task<(EvenementSession Session, EvenementReservation Reservation, int SeatAId, int SeatBId)>
            SeedSeatHoldReservationAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Seat Confirm", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "SEAT-CONF",
                Libelle = "Seat confirm",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.SeatNumbered,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var seatA = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "A-01",
                SeatStatus = EvenementSessionSeatStatus.Held,
                PrixUnitaire = 20m,
                CodeDevise = "USD",
                HoldExpireAtUtc = DateTime.UtcNow.AddMinutes(10)
            };
            var seatB = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "A-02",
                SeatStatus = EvenementSessionSeatStatus.Held,
                PrixUnitaire = 15m,
                CodeDevise = "USD",
                HoldExpireAtUtc = DateTime.UtcNow.AddMinutes(10)
            };
            ctx.EvenementSessionSeats.AddRange(seatA, seatB);
            await ctx.SaveChangesAsync();

            var reservation = new EvenementReservation
            {
                IdSociete = societe.IdSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-SEAT-CONF",
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                MontantSousTotal = 35m,
                CodeDevise = "USD",
                DateCreation = DateTime.UtcNow,
                Lines =
                {
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.Seat,
                        Quantite = 1,
                        PrixUnitaire = 20m,
                        CodeDevise = "USD",
                        IdEvenementSessionSeat = seatA.IdEvenementSessionSeat
                    },
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.Seat,
                        Quantite = 1,
                        PrixUnitaire = 15m,
                        CodeDevise = "USD",
                        IdEvenementSessionSeat = seatB.IdEvenementSessionSeat
                    }
                }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            return (session, reservation, seatA.IdEvenementSessionSeat, seatB.IdEvenementSessionSeat);
        }

        private static async Task<(int IdSociete, int IdReservation, int SeatAId, int SeatBId)>
            SeedSeatHoldViaServiceAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Seat Svc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "SEAT-SVC",
                Libelle = "Seat svc",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.SeatNumbered,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var seatA = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "B-01",
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            };
            var seatB = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "B-02",
                PrixUnitaire = 15m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionSeats.AddRange(seatA, seatB);
            await ctx.SaveChangesAsync();

            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                session.IdEvenementSession,
                societe.IdSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { SeatId = seatA.IdEvenementSessionSeat, Quantity = 1 },
                        new() { SeatId = seatB.IdEvenementSessionSeat, Quantity = 1 }
                    }
                });

            return (societe.IdSociete, hold.IdEvenementReservation, seatA.IdEvenementSessionSeat, seatB.IdEvenementSessionSeat);
        }
    }
}
