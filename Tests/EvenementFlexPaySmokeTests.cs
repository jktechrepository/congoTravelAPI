using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>Parcours bout-en-bout FlexPay événement Plan A (commande sans réservation avant succès).</summary>
    public class EvenementFlexPaySmokeTests
    {
        private const string SmokeOrderNumber = "FP-SMOKE-E2E-001";

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task FlexPay_journey_commande_initiate_callback_check_use_availability()
        {
            await using var ctx = BuildDb(nameof(FlexPay_journey_commande_initiate_callback_check_use_availability));
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock(SmokeOrderNumber);
            var (idSociete, idSite, idSession) = await SeedPublishedSessionForCommandeAsync(ctx, quantityPrix: 25m);

            var commandeService = EvenementTestFactories.CreateCommandeFlexPayService(ctx, flexApi.Object);
            var callbackService = EvenementTestFactories.CreateCallbackService(ctx, flexApi.Object);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);
            var ticketService = new EvenementTicketService(
                ctx, new ConfigSocieteService(ctx), NullLogger<EvenementTicketService>.Instance);

            var initiated = await commandeService.InitiateElectronicAsync(
                new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 2 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000001",
                        IdSite = idSite
                    }
                },
                idSociete,
                idSite);

            Assert.True(initiated.FlexPayAccepted);
            Assert.Equal(SmokeOrderNumber, initiated.OrderNumber);
            Assert.Equal("PENDING", initiated.Payment!.Status);
            Assert.Equal(0, initiated.Reservation.IdEvenementReservation);
            Assert.Equal("EN_ATTENTE_PAIEMENT", initiated.Reservation.Status);
            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
            Assert.Equal(1, await ctx.EvenementCommandesEnAttente.CountAsync());

            var availabilityAfterHold = await availabilityService.GetSessionAvailabilityAsync(idSession, idSociete);
            Assert.Equal(8, availabilityAfterHold!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(2, availabilityAfterHold.GlobalQuota.QuantiteHold);

            var callback = await callbackService.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = SmokeOrderNumber,
                Amount = "50",
                Currency = "USD"
            });

            Assert.True(callback.Success);
            Assert.False(callback.AlreadyProcessed);
            Assert.NotNull(callback.IdEvenementReservation);
            Assert.Equal(0, await ctx.EvenementCommandesEnAttente.CountAsync());

            var reservation = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(EvenementReservationStatus.CONFIRMED, reservation.Status);
            Assert.Equal(2, await ctx.EvenementTickets.CountAsync());
            Assert.Equal(callback.IdEvenementReservation, reservation.IdEvenementReservation);

            var confirmGraph = await ctx.EvenementReservations
                .Include(r => r.Lines).ThenInclude(l => l.Tickets)
                .SingleAsync();
            var ticketCode = confirmGraph.Lines.SelectMany(l => l.Tickets).First().TicketCode;

            await OpenEntryWindowAsync(ctx, idSession);

            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);
            Assert.Equal("Valide", check.Response.Statut);

            var use = await ticketService.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, use.HttpStatusCode);
            Assert.Equal("USED", use.Response!.Ticket.Status);

            var availabilityAfterSale = await availabilityService.GetSessionAvailabilityAsync(idSession, idSociete);
            Assert.Equal(8, availabilityAfterSale!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(2, availabilityAfterSale.GlobalQuota.QuantiteVendue);
            Assert.Equal(0, availabilityAfterSale.GlobalQuota.QuantiteHold);
        }

        [Fact]
        public async Task FlexPay_journey_commande_initiate_verify_check_ticket()
        {
            await using var ctx = BuildDb(nameof(FlexPay_journey_commande_initiate_verify_check_ticket));
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock(SmokeOrderNumber, checkStatus: "0");
            var (idSociete, idSite, idSession) = await SeedPublishedSessionForCommandeAsync(ctx, quantityPrix: 25m);

            var commandeService = EvenementTestFactories.CreateCommandeFlexPayService(ctx, flexApi.Object);
            var callbackService = EvenementTestFactories.CreateCallbackService(ctx, flexApi.Object);
            var ticketService = new EvenementTicketService(
                ctx, new ConfigSocieteService(ctx), NullLogger<EvenementTicketService>.Instance);

            var initiated = await commandeService.InitiateElectronicAsync(
                new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000002",
                        IdSite = idSite
                    }
                },
                idSociete,
                idSite);

            var verified = await callbackService.VerifyAndFinalizeAsync(initiated.OrderNumber!, idSociete);

            Assert.True(verified.IsConfirmSuccess);
            Assert.NotNull(verified.ConfirmPayment);
            Assert.Equal("CONFIRMED", verified.ConfirmPayment!.Reservation.Status);
            Assert.Single(verified.ConfirmPayment.Reservation.Tickets);

            var ticketCode = verified.ConfirmPayment.Reservation.Tickets[0].TicketCode;
            await OpenEntryWindowAsync(ctx, idSession);
            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);
        }

        [Fact]
        public async Task FlexPay_journey_callback_idempotent_after_confirm()
        {
            await using var ctx = BuildDb(nameof(FlexPay_journey_callback_idempotent_after_confirm));
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock(SmokeOrderNumber);
            var (idSociete, idSite, idSession) = await SeedPublishedSessionForCommandeAsync(ctx, quantityPrix: 25m);

            var commandeService = EvenementTestFactories.CreateCommandeFlexPayService(ctx, flexApi.Object);
            var callbackService = EvenementTestFactories.CreateCallbackService(ctx, flexApi.Object);

            await commandeService.InitiateElectronicAsync(
                new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000003",
                        IdSite = idSite
                    }
                },
                idSociete,
                idSite);

            var callback = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = SmokeOrderNumber,
                Amount = "25",
                Currency = "USD"
            };

            var first = await callbackService.ProcessCallbackAsync(callback);
            var second = await callbackService.ProcessCallbackAsync(callback);

            Assert.False(first.AlreadyProcessed);
            Assert.True(second.AlreadyProcessed);
            Assert.Equal(1, await ctx.EvenementTickets.CountAsync());
            Assert.Equal(1, await ctx.EvenementPayments.CountAsync());
            Assert.Equal(0, await ctx.EvenementCommandesEnAttente.CountAsync());
        }

        private static async Task<(int IdSociete, int IdSite, int IdSession)> SeedPublishedSessionForCommandeAsync(
            CongoTravelDbContext ctx,
            decimal quantityPrix)
        {
            var (idSociete, idSite, idReservation) =
                await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);

            var reservation = await ctx.EvenementReservations.SingleAsync(r => r.IdEvenementReservation == idReservation);
            var idSession = reservation.IdEvenementSession;

            // Plan A smoke : pas de HOLD métier — on retire le seed HOLD et on libère le quota.
            var cancel = EvenementTestFactories.CreateReservationService(ctx);
            await cancel.CancelAsync(idReservation, idSociete);

            var session = await ctx.EvenementSessions.SingleAsync(s => s.IdEvenementSession == idSession);
            session.EndAtUtc = DateTime.UtcNow.AddDays(2).AddHours(6);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            quota.CapaciteTotale = 10;
            quota.PrixUnitaire = quantityPrix;
            quota.QuantiteHold = 0;
            quota.QuantiteVendue = 0;
            await ctx.SaveChangesAsync();

            return (idSociete, idSite, idSession);
        }

        private static async Task OpenEntryWindowAsync(CongoTravelDbContext ctx, int idSession)
        {
            var session = await ctx.EvenementSessions.SingleAsync(s => s.IdEvenementSession == idSession);
            session.StartAtUtc = DateTime.UtcNow.AddHours(-1);
            session.EndAtUtc = DateTime.UtcNow.AddHours(6);
            await ctx.SaveChangesAsync();
        }
    }
}
