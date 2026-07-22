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
    public class EvenementReservationConfirmationServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ConfirmHoldAndEmitTicketsAsync_confirms_hold_and_emits_tickets()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAndEmitTicketsAsync_confirms_hold_and_emits_tickets));
            var (idSociete, idReservation) = await SeedHoldAsync(ctx, quantity: 2);
            var reservation = await ctx.EvenementReservations
                .Include(r => r.Lines)
                .SingleAsync(r => r.IdEvenementReservation == idReservation);

            var service = EvenementTestFactories.CreateConfirmationService(ctx);
            var payment = new EvenementPayment
            {
                ReferencePaiement = "EVT-PAY-TEST-001",
                Provider = "FLEXPAY",
                ProviderTxRef = "FP-ORDER-123",
                Montant = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                MontantTarif = reservation.MontantSousTotal,
                CodeDeviseTarif = reservation.CodeDevise,
                TauxVersDevisePaiement = 1m
            };

            await service.ConfirmHoldAndEmitTicketsAsync(reservation, payment, idSociete);
            await ctx.SaveChangesAsync();

            Assert.Equal(EvenementReservationStatus.CONFIRMED, reservation.Status);
            Assert.Null(reservation.ExpiresAtUtc);
            Assert.Equal(EvenementPaymentStatus.SUCCEEDED, payment.Status);
            Assert.Equal(2, await ctx.EvenementTickets.CountAsync());

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(2, quota.QuantiteVendue);
        }

        [Fact]
        public async Task ConfirmHoldAndEmitTicketsAsync_updates_existing_pending_payment()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAndEmitTicketsAsync_updates_existing_pending_payment));
            var (idSociete, idReservation) = await SeedHoldAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations
                .Include(r => r.Lines)
                .SingleAsync(r => r.IdEvenementReservation == idReservation);

            var pendingPayment = new EvenementPayment
            {
                IdEvenementReservation = idReservation,
                ReferencePaiement = "EVT-PAY-PENDING",
                Provider = "FLEXPAY",
                ProviderTxRef = "FP-ORDER-456",
                Status = EvenementPaymentStatus.PENDING,
                Montant = reservation.MontantSousTotal,
                CodeDevise = reservation.CodeDevise,
                MontantTarif = reservation.MontantSousTotal,
                CodeDeviseTarif = reservation.CodeDevise,
                TauxVersDevisePaiement = 1m,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementPayments.Add(pendingPayment);
            await ctx.SaveChangesAsync();

            var service = EvenementTestFactories.CreateConfirmationService(ctx);
            await service.ConfirmHoldAndEmitTicketsAsync(reservation, pendingPayment, idSociete);
            await ctx.SaveChangesAsync();

            Assert.Equal(EvenementPaymentStatus.SUCCEEDED, pendingPayment.Status);
            Assert.Equal(1, await ctx.EvenementPayments.CountAsync());
            Assert.Equal(1, await ctx.EvenementTickets.CountAsync());
        }

        [Fact]
        public void EnsureHoldConfirmable_rejects_expired_hold()
        {
            var reservation = new EvenementReservation
            {
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
                Lines = { new EvenementReservationLine { Quantite = 1 } }
            };

            var service = EvenementTestFactories.CreateConfirmationService(BuildDb(nameof(EnsureHoldConfirmable_rejects_expired_hold)));

            var ex = Assert.Throws<InvalidOperationException>(() => service.EnsureHoldConfirmable(reservation));
            Assert.Contains("expiré", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<(int IdSociete, int IdReservation)> SeedHoldAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var societe = new Societe { Nom = "EVT Confirm", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"CNF-{Guid.NewGuid():N}"[..10],
                Libelle = "Confirm test",
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

            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

            var hold = await holdService.CreateHoldAsync(
                session.IdEvenementSession,
                societe.IdSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (societe.IdSociete, hold.IdEvenementReservation);
        }
    }
}
