using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementGlobalQuotaConfirmStrategyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ConfirmHoldAsync_transfers_hold_to_sold()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAsync_transfers_hold_to_sold));
            var (session, reservation) = await SeedAsync(ctx, hold: 4, sold: 1);

            var strategy = new EvenementGlobalQuotaConfirmStrategy(ctx);
            await strategy.ConfirmHoldAsync(new EvenementInventoryConfirmRequest
            {
                Session = session,
                Reservation = reservation
            });

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(1, quota.QuantiteHold);
            Assert.Equal(4, quota.QuantiteVendue);
        }

        [Fact]
        public async Task ConfirmHoldAsync_throws_conflict_when_hold_insufficient()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAsync_throws_conflict_when_hold_insufficient));
            var (session, reservation) = await SeedAsync(ctx, hold: 1, sold: 0);

            var strategy = new EvenementGlobalQuotaConfirmStrategy(ctx);

            await Assert.ThrowsAsync<EvenementHoldConflictException>(() =>
                strategy.ConfirmHoldAsync(new EvenementInventoryConfirmRequest
                {
                    Session = session,
                    Reservation = reservation
                }));
        }

        private static async Task<(EvenementSession Session, EvenementReservation Reservation)> SeedAsync(
            CongoTravelDbContext ctx,
            int hold,
            int sold)
        {
            var societe = new Societe { Nom = "Confirm EVT", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = "CONF-1",
                Libelle = "Confirm",
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
                CapaciteTotale = 20,
                QuantiteHold = hold,
                QuantiteVendue = sold,
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            });

            var reservation = new EvenementReservation
            {
                IdSociete = societe.IdSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-RES-CONF",
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                MontantSousTotal = 30m,
                CodeDevise = "CDF",
                DateCreation = DateTime.UtcNow,
                Lines =
                {
                    new EvenementReservationLine
                    {
                        LineType = EvenementReservationLineType.GlobalQuota,
                        Quantite = 3,
                        PrixUnitaire = 10m,
                        CodeDevise = "CDF"
                    }
                }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            return (session, reservation);
        }
    }
}
