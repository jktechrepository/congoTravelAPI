using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Services;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Restaurant.Strategies;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantPhase2CashTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int jwtSocieteId = 999)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(0);
            return mock;
        }

        private static RestaurantReservationWithPaiementService CreateWithPaiementService(
            CongoTravelDbContext ctx,
            ICurrentUserService? currentUser = null)
        {
            var hold = new RestaurantHoldService(
                ctx,
                RestaurantTestFactories.CreateHoldStrategyFactory(ctx),
                new ConfigSocieteService(ctx),
                NullLogger<RestaurantHoldService>.Instance);

            var confirmation = new RestaurantReservationConfirmationService(
                ctx,
                RestaurantTestFactories.CreateConfirmStrategyFactory(ctx),
                NullLogger<RestaurantReservationConfirmationService>.Instance);

            var payment = new RestaurantPaymentService(
                ctx,
                confirmation,
                NullLogger<RestaurantPaymentService>.Instance);

            var reservation = new RestaurantReservationService(
                ctx,
                RestaurantTestFactories.CreateCancelStrategyFactory(ctx),
                NullLogger<RestaurantReservationService>.Instance);

            var flexInit = RestaurantTestFactories.CreateFlexPayInitiationService(
                ctx,
                Mock.Of<IFlexPayService>(),
                enabled: false);

            return new RestaurantReservationWithPaiementService(
                ctx,
                hold,
                payment,
                flexInit,
                reservation,
                currentUser ?? MockClientUser().Object,
                NullLogger<RestaurantReservationWithPaiementService>.Instance);
        }

        private static RestaurantHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            RestaurantTestFactories.CreateHoldService(ctx);

        private static RestaurantReservationService CreateReservationService(CongoTravelDbContext ctx) =>
            RestaurantTestFactories.CreateReservationService(ctx);

        private static async Task<(int IdSociete, int IdSite, int IdCreneau)> SeedPublishedCreneauAsync(
            CongoTravelDbContext ctx,
            string suffix,
            int capacite = 20,
            decimal prixUnitaire = 50m,
            decimal acomptePourcent = 20m,
            decimal? montantAcompte = null)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                ctx, $"Resto P2 {suffix}");

            var etablissementService = new RestaurantEtablissementService(
                ctx, NullLogger<RestaurantEtablissementService>.Instance);
            var creneauService = new RestaurantCreneauService(
                ctx, NullLogger<RestaurantCreneauService>.Instance);

            var etablissement = await etablissementService.PublishAsync(
                (await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
                {
                    CodeRestaurant = $"REST-P2-{suffix}",
                    Nom = $"Restaurant {suffix}",
                    IdSite = idSite,
                    AcomptePourcentDefaut = acomptePourcent
                }, idSociete)).IdRestaurant,
                idSociete);

            var start = DateTime.UtcNow.Date.AddDays(1).AddHours(19);
            var draft = await creneauService.CreateDraftAsync(new RestaurantCreateCreneauRequestDto
            {
                IdRestaurant = etablissement.IdRestaurant,
                DateService = DateOnly.FromDateTime(start),
                StartAtUtc = start,
                EndAtUtc = start.AddHours(2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                MontantAcompte = montantAcompte,
                GlobalQuota = new RestaurantCreateCreneauGlobalQuotaDto
                {
                    CapaciteTotale = capacite,
                    PrixUnitaire = prixUnitaire
                }
            }, idSociete);

            var published = await creneauService.PublishAsync(draft.IdRestaurantCreneau, idSociete);
            return (idSociete, idSite, published.IdRestaurantCreneau);
        }

        [Fact]
        public void AddRestaurantReservations_registers_phase2_services()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_phase2_services)));
            services.AddScoped<IConfigSocieteRepository, ConfigSocieteService>();
            services.AddSingleton(Mock.Of<ICurrentUserService>());
            services.AddSingleton(Mock.Of<IFlexPayRealtimeNotifier>());
            services.AddSingleton(Mock.Of<IFlexPayService>());
            services.AddSingleton(Mock.Of<Microsoft.AspNetCore.Http.IHttpContextAccessor>());
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(
                new CongoTravel.Configuration.FlexPayOptions { Enabled = true }));
            services.AddScoped<IInfoPaiementResolutionService, InfoPaiementResolutionService>();
            services.AddScoped<IDeviseMontantConverter, DeviseMontantConverter>();
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetService<IRestaurantHoldService>());
            Assert.NotNull(provider.GetService<IRestaurantPaymentService>());
            Assert.NotNull(provider.GetService<IRestaurantReservationService>());
            Assert.NotNull(provider.GetService<IRestaurantReservationWithPaiementService>());
            Assert.NotNull(provider.GetService<IRestaurantAvailabilityService>());
            Assert.NotNull(provider.GetService<IRestaurantHoldExpirationRunner>());
            Assert.NotNull(provider.GetService<IRestaurantFlexPayInitiationService>());
            Assert.NotNull(provider.GetService<IRestaurantFlexPayCallbackService>());
        }

        [Fact]
        public async Task With_paiement_CASH_confirms_and_increments_vendue()
        {
            await using var ctx = BuildDb(nameof(With_paiement_CASH_confirms_and_increments_vendue));
            // acompte = 20% * 50 = 10 → total 2 couverts = 20
            var (idSociete, idSite, idCreneau) = await SeedPublishedCreneauAsync(
                ctx, "CASH", capacite: 20, prixUnitaire: 50m, acomptePourcent: 20m);

            var service = CreateWithPaiementService(ctx);

            var result = await service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
            {
                IdRestaurantCreneau = idCreneau,
                CustomerRef = "TABLE-1",
                Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new RestaurantReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-1"
                }
            });

            Assert.Equal("Succes", result.TransactionStatut);
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(2, result.Reservation.NombreCouverts);
            Assert.Equal(20m, result.Reservation.MontantSousTotal);
            Assert.Equal("SUCCEEDED", result.Payment!.Status);
            Assert.Equal(20m, result.Payment.Montant);

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(2, quota.QuantiteVendue);
        }

        [Fact]
        public async Task Cancel_HOLD_releases_quantite_hold()
        {
            await using var ctx = BuildDb(nameof(Cancel_HOLD_releases_quantite_hold));
            var (idSociete, _, idCreneau) = await SeedPublishedCreneauAsync(ctx, "CANCEL", capacite: 10);

            var holdService = CreateHoldService(ctx);
            var reservationService = CreateReservationService(ctx);

            var hold = await holdService.CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 3 } }
                });

            Assert.Equal("HOLD", hold.Status);
            var quotaAfterHold = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(3, quotaAfterHold.QuantiteHold);

            var cancel = await reservationService.CancelAsync(
                hold.IdRestaurantReservation, idSociete);

            Assert.Equal("CANCELLED", cancel.Reservation.Status);
            Assert.False(cancel.AlreadyCancelled);

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task ExpireHoldsAsync_marks_EXPIRED_and_releases_hold()
        {
            await using var ctx = BuildDb(nameof(ExpireHoldsAsync_marks_EXPIRED_and_releases_hold));
            var (idSociete, _, idCreneau) = await SeedPublishedCreneauAsync(ctx, "EXPIRE", capacite: 15);

            var holdService = CreateHoldService(ctx);
            var hold = await holdService.CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 4 } }
                });

            var reservation = await ctx.RestaurantReservations
                .SingleAsync(r => r.IdRestaurantReservation == hold.IdRestaurantReservation);
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await ctx.SaveChangesAsync();

            var runner = new RestaurantHoldExpirationRunner(
                Mock.Of<IFlexPayRealtimeNotifier>(),
                NullLogger<RestaurantHoldExpirationRunner>.Instance);
            await runner.ExpireHoldsAsync(ctx);

            await ctx.Entry(reservation).ReloadAsync();
            Assert.Equal(Models.Restaurant.Enums.RestaurantReservationStatus.EXPIRED, reservation.Status);

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(0, quota.QuantiteHold);
        }

        [Fact]
        public async Task Insufficient_capacity_throws_hold_conflict()
        {
            await using var ctx = BuildDb(nameof(Insufficient_capacity_throws_hold_conflict));
            var (idSociete, _, idCreneau) = await SeedPublishedCreneauAsync(ctx, "CONFLICT", capacite: 2);

            var service = CreateWithPaiementService(ctx);

            await Assert.ThrowsAsync<RestaurantHoldConflictException>(() =>
                service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
                {
                    IdRestaurantCreneau = idCreneau,
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 5 } },
                    Paiement = new RestaurantReservationPaiementBlockDto
                    {
                        MethodePaiement = "CASH"
                    }
                }));

            var quota = await ctx.RestaurantCreneauGlobalQuotas
                .SingleAsync(q => q.IdRestaurantCreneau == idCreneau);
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task With_paiement_rejects_electronic_method()
        {
            await using var ctx = BuildDb(nameof(With_paiement_rejects_electronic_method));
            var (_, _, idCreneau) = await SeedPublishedCreneauAsync(ctx, "ELEC");

            var service = CreateWithPaiementService(ctx);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCashAsync(new RestaurantReservationWithPaiementRequestDto
                {
                    IdRestaurantCreneau = idCreneau,
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new RestaurantReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "0990000000"
                    }
                }));

            Assert.Contains("with-paiement-electronique", ex.Message);
        }
    }
}
