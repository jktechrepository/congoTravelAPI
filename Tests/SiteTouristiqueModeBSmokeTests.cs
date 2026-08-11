using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueModeBSmokeTests
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
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var classeService = new SiteTouristiqueClasseService(
                ctx, NullLogger<SiteTouristiqueClasseService>.Instance);
            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);
            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var availabilityService = new SiteTouristiqueAvailabilityService(
                ctx, NullLogger<SiteTouristiqueAvailabilityService>.Instance);
            var paymentService = SiteTouristiqueTestFactories.CreatePaymentService(ctx);
            var ticketService = new SiteTouristiqueTicketService(
                ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var vipClasse = await classeService.CreateAsync(new SiteTouristiqueCreateClasseRequestDto
            {
                Code = "VIP",
                Libelle = "VIP",
                Description = "Zone VIP"
            }, idSociete);

            var stdClasse = await classeService.CreateAsync(new SiteTouristiqueCreateClasseRequestDto
            {
                Code = "STD",
                Libelle = "Standard"
            }, idSociete);

            var lieu = await lieuService.PublishAsync(
                (await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = "SMOKE-B",
                    Nom = "Parc Smoke B",
                    IdSite = idSite
                }, idSociete)).IdSiteTouristique,
                idSociete);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var draft = await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                DateVisite = today,
                InventoryMode = "ClassQuota",
                CodeDevise = "USD",
                ClassQuotas = new List<SiteTouristiqueCreateJourneeClassQuotaDto>
                {
                    new()
                    {
                        IdSiteTouristiqueClasse = vipClasse.IdSiteTouristiqueClasse,
                        CapaciteTotale = 20,
                        PrixUnitaire = 100m
                    },
                    new()
                    {
                        IdSiteTouristiqueClasse = stdClasse.IdSiteTouristiqueClasse,
                        CapaciteTotale = 30,
                        PrixUnitaire = 30m
                    }
                }
            }, idSociete);

            Assert.Equal(2, draft.ClassQuotas.Count);

            var published = await journeeService.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);
            Assert.Equal("Published", published.Status);
            Assert.Equal("ClassQuota", published.InventoryMode);

            var hold = await holdService.CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    CustomerRef = "SMOKE-B-CUST",
                    Items = new List<SiteTouristiqueHoldItemRequestDto>
                    {
                        new() { ClassId = vipClasse.IdSiteTouristiqueClasse, Quantity = 2 },
                        new() { ClassId = stdClasse.IdSiteTouristiqueClasse, Quantity = 1 }
                    }
                });

            Assert.Equal(230m, hold.AmountPreview);

            var confirm = await paymentService.ConfirmPaymentAsync(
                hold.IdSiteTouristiqueReservation,
                idSociete,
                new SiteTouristiqueConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            Assert.Equal("CONFIRMED", confirm.Reservation.Status);
            Assert.Equal(3, confirm.Reservation.Tickets.Count);
            Assert.Equal(230m, confirm.Payment.Montant);

            var ticketCode = confirm.Reservation.Tickets[0].TicketCode;
            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);
            Assert.Equal("Valide", check.Response.Statut);

            var use = await ticketService.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, use.HttpStatusCode);
            Assert.Equal("USED", use.Response!.Ticket.Status);

            var availabilityAfterSale = await availabilityService.GetJourneeAvailabilityAsync(
                published.IdSiteTouristiqueJournee,
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
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var classeService = new SiteTouristiqueClasseService(
                ctx, NullLogger<SiteTouristiqueClasseService>.Instance);
            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);
            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var cancelService = SiteTouristiqueTestFactories.CreateReservationService(ctx);
            var availabilityService = new SiteTouristiqueAvailabilityService(
                ctx, NullLogger<SiteTouristiqueAvailabilityService>.Instance);

            var vipClasse = await classeService.CreateAsync(new SiteTouristiqueCreateClasseRequestDto
            {
                Code = "VIP",
                Libelle = "VIP"
            }, idSociete);

            var lieu = await lieuService.PublishAsync(
                (await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = "CANCEL-B",
                    Nom = "Cancel B",
                    IdSite = idSite
                }, idSociete)).IdSiteTouristique,
                idSociete);

            var published = await journeeService.PublishAsync(
                (await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
                {
                    IdSiteTouristique = lieu.IdSiteTouristique,
                    DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    InventoryMode = "ClassQuota",
                    CodeDevise = "CDF",
                    ClassQuotas = new List<SiteTouristiqueCreateJourneeClassQuotaDto>
                    {
                        new()
                        {
                            IdSiteTouristiqueClasse = vipClasse.IdSiteTouristiqueClasse,
                            CapaciteTotale = 10,
                            PrixUnitaire = 50m
                        }
                    }
                }, idSociete)).IdSiteTouristiqueJournee,
                idSociete);

            var hold = await holdService.CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    Items = new List<SiteTouristiqueHoldItemRequestDto>
                    {
                        new() { ClassId = vipClasse.IdSiteTouristiqueClasse, Quantity = 3 }
                    }
                });

            await cancelService.CancelAsync(hold.IdSiteTouristiqueReservation, idSociete);

            var availability = await availabilityService.GetJourneeAvailabilityAsync(
                published.IdSiteTouristiqueJournee,
                idSociete);
            Assert.Equal(10, availability!.ClassQuotas!.Single().QuantiteDisponible);
            Assert.Equal(0, availability.ClassQuotas.Single().QuantiteHold);
        }
    }
}
