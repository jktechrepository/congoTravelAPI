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
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementDiRegistrationTests
    {
        [Fact]
        public void AddEvenementTicketing_registers_core_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddEvenementTicketing_registers_core_services)));
            services.AddScoped<CongoTravel.Services.Repositories.IConfigSocieteRepository, ConfigSocieteService>();
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddSingleton<IOptions<FlexPayOptions>>(Options.Create(new FlexPayOptions()));
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<IHttpContextAccessor>());
            services.AddEvenementTicketing();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IEvenementSessionService>());
            Assert.NotNull(provider.GetService<IEvenementHoldService>());
            Assert.NotNull(provider.GetService<IEvenementAvailabilityService>());
            Assert.NotNull(provider.GetService<IEvenementPaymentService>());
            Assert.NotNull(provider.GetService<IEvenementReservationConfirmationService>());
            Assert.NotNull(provider.GetService<IEvenementFlexPayInitiationService>());
            Assert.NotNull(provider.GetService<IEvenementFlexPayCallbackService>());
            Assert.NotNull(provider.GetService<IEvenementDashboardService>());
            Assert.NotNull(provider.GetService<IEvenementReservationService>());
            Assert.NotNull(provider.GetService<IEvenementTicketService>());
            Assert.NotNull(provider.GetService<IEvenementHoldExpirationRunner>());
        }
    }

    public class EvenementModeCSmokeTests
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
            var idSociete = await SeedSocieteAsync(ctx);

            var sessionService = new EvenementSessionService(
                ctx, NullLogger<EvenementSessionService>.Instance);
            var holdService = CreateHoldService(ctx);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);
            var paymentService = CreatePaymentService(ctx);
            var ticketService = new EvenementTicketService(
                ctx, NullLogger<EvenementTicketService>.Instance);

            var draft = await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "SMOKE-2026",
                Libelle = "Smoke Mode C",
                StartAtUtc = DateTime.UtcNow.AddHours(-1),
                EndAtUtc = DateTime.UtcNow.AddHours(6),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 25m,
                    CodeDevise = "USD"
                }
            }, idSociete);

            var published = await sessionService.PublishAsync(draft.IdEvenementSession, idSociete);
            Assert.Equal("Published", published.Status);

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    CustomerRef = "SMOKE-CUST",
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 3 } }
                });

            var availabilityAfterHold = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);
            Assert.NotNull(availabilityAfterHold?.GlobalQuota);
            Assert.Equal(17, availabilityAfterHold.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(3, availabilityAfterHold.GlobalQuota.QuantiteHold);

            var confirm = await paymentService.ConfirmPaymentAsync(
                hold.IdEvenementReservation,
                idSociete,
                new EvenementConfirmPaymentRequestDto { MethodePaiement = "CASH" });

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

            var availabilityAfterSale = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);
            Assert.Equal(17, availabilityAfterSale!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(3, availabilityAfterSale.GlobalQuota.QuantiteVendue);
        }

        [Fact]
        public async Task ModeC_cancel_hold_restores_availability()
        {
            await using var ctx = BuildDb(nameof(ModeC_cancel_hold_restores_availability));
            var idSociete = await SeedSocieteAsync(ctx);

            var sessionService = new EvenementSessionService(
                ctx, NullLogger<EvenementSessionService>.Instance);
            var holdService = CreateHoldService(ctx);
            var cancelService = CreateCancelService(ctx);
            var availabilityService = new EvenementAvailabilityService(
                ctx, NullLogger<EvenementAvailabilityService>.Instance);

            var published = await sessionService.PublishAsync(
                (await sessionService.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "CANCEL-SMOKE",
                    Libelle = "Cancel smoke",
                    StartAtUtc = DateTime.UtcNow.AddDays(1),
                    InventoryMode = "GlobalQuota",
                    GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                    {
                        CapaciteTotale = 10,
                        PrixUnitaire = 5m,
                        CodeDevise = "CDF"
                    }
                }, idSociete)).IdEvenementSession,
                idSociete);

            var hold = await holdService.CreateHoldAsync(
                published.IdEvenementSession,
                idSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 4 } }
                });

            await cancelService.CancelAsync(hold.IdEvenementReservation, idSociete);

            var availability = await availabilityService.GetSessionAvailabilityAsync(
                published.IdEvenementSession,
                idSociete);

            Assert.Equal(10, availability!.GlobalQuota!.QuantiteDisponible);
            Assert.Equal(0, availability.GlobalQuota.QuantiteHold);
        }

        private static EvenementHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new Services.Evenement.Strategies.EvenementInventoryHoldStrategyFactory(
                    new Services.Evenement.Strategies.EvenementGlobalQuotaHoldStrategy(ctx),
                    new Services.Evenement.Strategies.EvenementClassQuotaHoldStrategy(ctx),
                    new Services.Evenement.Strategies.EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

        private static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            EvenementTestFactories.CreatePaymentService(ctx);

        private static EvenementReservationService CreateCancelService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new Services.Evenement.Strategies.EvenementInventoryCancelStrategyFactory(
                    new Services.Evenement.Strategies.EvenementGlobalQuotaCancelStrategy(ctx),
                    new Services.Evenement.Strategies.EvenementClassQuotaCancelStrategy(ctx),
                    new Services.Evenement.Strategies.EvenementSeatNumberedCancelStrategy(ctx)),
                NullLogger<EvenementReservationService>.Instance);

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Smoke Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
