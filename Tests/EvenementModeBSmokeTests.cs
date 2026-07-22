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
    public class EvenementModeBSmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ModeB_journey_classes_publish_hold_confirm_check_use_availability()
        {
            await using var ctx = BuildDb(nameof(ModeB_journey_classes_publish_hold_confirm_check_use_availability));
            var idSociete = await SeedSocieteAsync(ctx);

            var classeService = new EvenementClasseService(
                ctx, NullLogger<EvenementClasseService>.Instance);
            var sessionService = new EvenementSessionService(
                ctx, NullLogger<EvenementSessionService>.Instance);
            var holdService = CreateHoldService(ctx);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);
            var paymentService = CreatePaymentService(ctx);
            var ticketService = new EvenementTicketService(
                ctx, NullLogger<EvenementTicketService>.Instance);

            var vipClasse = await classeService.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "VIP",
                Libelle = "VIP",
                Description = "Zone VIP"
            }, idSociete);

            var stdClasse = await classeService.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "STD",
                Libelle = "Standard"
            }, idSociete);

            var draft = await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "SMOKE-B-2026",
                Libelle = "Smoke Mode B",
                StartAtUtc = DateTime.UtcNow.AddHours(-1),
                EndAtUtc = DateTime.UtcNow.AddHours(6),
                InventoryMode = "ClassQuota",
                ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                {
                    new()
                    {
                        IdEvenementClasse = vipClasse.IdEvenementClasse,
                        CapaciteTotale = 20,
                        PrixUnitaire = 100m,
                        CodeDevise = "USD"
                    },
                    new()
                    {
                        IdEvenementClasse = stdClasse.IdEvenementClasse,
                        CapaciteTotale = 30,
                        PrixUnitaire = 30m,
                        CodeDevise = "USD"
                    }
                }
            }, idSociete);

            Assert.Equal(2, draft.ClassQuotas.Count);

            var published = await sessionService.PublishAsync(draft.IdEvenementSession, idSociete);
            Assert.Equal("Published", published.Status);
            Assert.Equal("ClassQuota", published.InventoryMode);

            var availabilityInitial = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);
            Assert.NotNull(availabilityInitial?.ClassQuotas);
            Assert.Equal(2, availabilityInitial.ClassQuotas!.Count);
            Assert.Equal(20, availabilityInitial.ClassQuotas.Single(q => q.CodeClasse == "VIP").QuantiteDisponible);
            Assert.Equal(30, availabilityInitial.ClassQuotas.Single(q => q.CodeClasse == "STD").QuantiteDisponible);

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    CustomerRef = "SMOKE-B-CUST",
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { ClassId = vipClasse.IdEvenementClasse, Quantity = 2 },
                        new() { ClassId = stdClasse.IdEvenementClasse, Quantity = 1 }
                    }
                });

            Assert.Equal(230m, hold.AmountPreview);

            var availabilityAfterHold = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);
            var vipAfterHold = availabilityAfterHold!.ClassQuotas!.Single(q => q.CodeClasse == "VIP");
            var stdAfterHold = availabilityAfterHold.ClassQuotas.Single(q => q.CodeClasse == "STD");
            Assert.Equal(18, vipAfterHold.QuantiteDisponible);
            Assert.Equal(2, vipAfterHold.QuantiteHold);
            Assert.Equal(29, stdAfterHold.QuantiteDisponible);
            Assert.Equal(1, stdAfterHold.QuantiteHold);

            var confirm = await paymentService.ConfirmPaymentAsync(
                hold.IdEvenementReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            Assert.Equal("CONFIRMED", confirm.Reservation.Status);
            Assert.Equal(3, confirm.Reservation.Tickets.Count);
            Assert.Equal(230m, confirm.Payment.Montant);

            var ticketCode = confirm.Reservation.Tickets[0].TicketCode;
            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);
            Assert.Equal("Valide", check.Response.Statut);

            var use = await ticketService.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, use.HttpStatusCode);
            Assert.False(use.Response!.AlreadyUsed);
            Assert.Equal("USED", use.Response.Ticket.Status);

            var availabilityAfterSale = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);
            var vipAfterSale = availabilityAfterSale!.ClassQuotas!.Single(q => q.CodeClasse == "VIP");
            var stdAfterSale = availabilityAfterSale.ClassQuotas.Single(q => q.CodeClasse == "STD");
            Assert.Equal(18, vipAfterSale.QuantiteDisponible);
            Assert.Equal(2, vipAfterSale.QuantiteVendue);
            Assert.Equal(29, stdAfterSale.QuantiteDisponible);
            Assert.Equal(1, stdAfterSale.QuantiteVendue);
        }

        [Fact]
        public async Task ModeB_cancel_hold_restores_class_availability()
        {
            await using var ctx = BuildDb(nameof(ModeB_cancel_hold_restores_class_availability));
            var idSociete = await SeedSocieteAsync(ctx);

            var classeService = new EvenementClasseService(
                ctx, NullLogger<EvenementClasseService>.Instance);
            var sessionService = new EvenementSessionService(
                ctx, NullLogger<EvenementSessionService>.Instance);
            var holdService = CreateHoldService(ctx);
            var cancelService = CreateCancelService(ctx);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);

            var vipClasse = await classeService.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "VIP",
                Libelle = "VIP"
            }, idSociete);

            var published = await sessionService.PublishAsync(
                (await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "CANCEL-B-SMOKE",
                    Libelle = "Cancel smoke B",
                    StartAtUtc = DateTime.UtcNow.AddDays(1),
                    InventoryMode = "ClassQuota",
                    ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                    {
                        new()
                        {
                            IdEvenementClasse = vipClasse.IdEvenementClasse,
                            CapaciteTotale = 10,
                            PrixUnitaire = 50m,
                            CodeDevise = "CDF"
                        }
                    }
                }, idSociete)).IdEvenementSession,
                idSociete);

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { ClassId = vipClasse.IdEvenementClasse, Quantity = 4 }
                    }
                });

            await cancelService.CancelAsync(hold.IdEvenementReservation, idSociete);

            var availability = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);

            var vip = availability!.ClassQuotas!.Single();
            Assert.Equal(10, vip.QuantiteDisponible);
            Assert.Equal(0, vip.QuantiteHold);
            Assert.Equal(0, vip.QuantiteVendue);
        }

        [Fact]
        public async Task ModeB_cancel_confirmed_restores_class_sold_stock()
        {
            await using var ctx = BuildDb(nameof(ModeB_cancel_confirmed_restores_class_sold_stock));
            var idSociete = await SeedSocieteAsync(ctx);

            var classeService = new EvenementClasseService(
                ctx, NullLogger<EvenementClasseService>.Instance);
            var sessionService = new EvenementSessionService(
                ctx, NullLogger<EvenementSessionService>.Instance);
            var holdService = CreateHoldService(ctx);
            var paymentService = CreatePaymentService(ctx);
            var cancelService = CreateCancelService(ctx);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);

            var stdClasse = await classeService.CreateAsync(new EvenementCreateClasseRequestDto
            {
                CodeClasse = "STD",
                Libelle = "Standard"
            }, idSociete);

            var published = await sessionService.PublishAsync(
                (await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "CANCEL-CONF-B",
                    Libelle = "Cancel confirmed B",
                    StartAtUtc = DateTime.UtcNow.AddDays(2),
                    InventoryMode = "ClassQuota",
                    ClassQuotas = new List<EvenementCreateSessionClassQuotaDto>
                    {
                        new()
                        {
                            IdEvenementClasse = stdClasse.IdEvenementClasse,
                            CapaciteTotale = 15,
                            PrixUnitaire = 20m,
                            CodeDevise = "USD"
                        }
                    }
                }, idSociete)).IdEvenementSession,
                idSociete);

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto>
                    {
                        new() { ClassId = stdClasse.IdEvenementClasse, Quantity = 3 }
                    }
                });

            await paymentService.ConfirmPaymentAsync(
                hold.IdEvenementReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var cancel = await cancelService.CancelAsync(hold.IdEvenementReservation, idSociete);
            Assert.Equal(3, cancel.TicketsVoided);

            var availability = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);

            var std = availability!.ClassQuotas!.Single();
            Assert.Equal(15, std.QuantiteDisponible);
            Assert.Equal(0, std.QuantiteHold);
            Assert.Equal(0, std.QuantiteVendue);
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
                NullLogger<EvenementReservationService>.Instance);

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Smoke B Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
