using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueDiRegistrationTests
    {
        [Fact]
        public void AddSiteTouristiqueTicketing_registers_core_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddSiteTouristiqueTicketing_registers_core_services)));
            services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            services.AddSingleton<IOptions<FlexPayOptions>>(Options.Create(new FlexPayOptions()));
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<IHttpContextAccessor>());
            services.AddSingleton(Mock.Of<IFlexPayRealtimeNotifier>());
            services.AddSiteTouristiqueTicketing();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<ISiteTouristiqueLieuService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueJourneeService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueHoldService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueAvailabilityService>());
            Assert.NotNull(provider.GetService<ISiteTouristiquePaymentService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueReservationConfirmationService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueFlexPayInitiationService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueFlexPayCallbackService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueDashboardService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueReservationService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueTicketService>());
            Assert.NotNull(provider.GetService<ISiteTouristiqueHoldExpirationRunner>());
        }
    }

    public class SiteTouristiqueModeCSmokeTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ModeC_journey_publish_hold_confirm_check_use_availability()
        {
            await using var ctx = BuildDb(nameof(ModeC_journey_publish_hold_confirm_check_use_availability));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);
            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var availabilityService = new SiteTouristiqueAvailabilityService(
                ctx, NullLogger<SiteTouristiqueAvailabilityService>.Instance);
            var paymentService = SiteTouristiqueTestFactories.CreatePaymentService(ctx);
            var ticketService = new SiteTouristiqueTicketService(
                ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = "SMOKE-C",
                Nom = "Parc Smoke C",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var draft = await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                DateVisite = today,
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 25m
                }
            }, idSociete);

            var published = await journeeService.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);
            Assert.Equal("Published", published.Status);

            var hold = await holdService.CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    CustomerRef = "SMOKE-CUST",
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 3 } }
                });

            var availabilityAfterHold = await availabilityService.GetJourneeAvailabilityAsync(
                published.IdSiteTouristiqueJournee,
                idSociete);
            Assert.NotNull(availabilityAfterHold?.GlobalQuota);
            Assert.Equal(17, availabilityAfterHold.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(3, availabilityAfterHold.GlobalQuota.QuantiteHold);

            var confirm = await paymentService.ConfirmPaymentAsync(
                hold.IdSiteTouristiqueReservation,
                idSociete,
                new SiteTouristiqueConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            Assert.Equal("CONFIRMED", confirm.Reservation.Status);
            Assert.Equal(3, confirm.Reservation.Tickets.Count);

            var ticketCode = confirm.Reservation.Tickets[0].TicketCode;

            var check = await ticketService.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, check.HttpStatusCode);
            Assert.Equal("Valide", check.Response.Statut);

            var use = await ticketService.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, use.HttpStatusCode);
            Assert.False(use.Response!.AlreadyUsed);
            Assert.Equal("USED", use.Response.Ticket.Status);

            var availabilityAfterSale = await availabilityService.GetJourneeAvailabilityAsync(
                published.IdSiteTouristiqueJournee,
                idSociete);
            Assert.Equal(17, availabilityAfterSale!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(3, availabilityAfterSale.GlobalQuota.QuantiteVendue);
        }

        [Fact]
        public async Task ModeC_cancel_hold_restores_availability()
        {
            await using var ctx = BuildDb(nameof(ModeC_cancel_hold_restores_availability));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx);

            var lieuService = new SiteTouristiqueLieuService(ctx, NullLogger<SiteTouristiqueLieuService>.Instance);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);
            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var cancelService = SiteTouristiqueTestFactories.CreateReservationService(ctx);
            var availabilityService = new SiteTouristiqueAvailabilityService(
                ctx, NullLogger<SiteTouristiqueAvailabilityService>.Instance);

            var lieu = await lieuService.PublishAsync(
                (await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = "CANCEL-C",
                    Nom = "Cancel C",
                    IdSite = idSite
                }, idSociete)).IdSiteTouristique,
                idSociete);

            var published = await journeeService.PublishAsync(
                (await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
                {
                    IdSiteTouristique = lieu.IdSiteTouristique,
                    DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    InventoryMode = "GlobalQuota",
                    CodeDevise = "CDF",
                    GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                    {
                        CapaciteTotale = 10,
                        PrixUnitaire = 5m
                    }
                }, idSociete)).IdSiteTouristiqueJournee,
                idSociete);

            var hold = await holdService.CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 4 } }
                });

            await cancelService.CancelAsync(hold.IdSiteTouristiqueReservation, idSociete);

            var availability = await availabilityService.GetJourneeAvailabilityAsync(
                published.IdSiteTouristiqueJournee,
                idSociete);

            Assert.Equal(10, availability!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(0, availability.GlobalQuota.QuantiteHold);
        }
    }
}
