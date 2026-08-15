using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Restaurant.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Tests
{
    internal static class RestaurantTestFactories
    {
        public static RestaurantPhotoService CreatePhotoService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<RestaurantPhotoService>.Instance);

        public static RestaurantEtablissementService CreateEtablissementService(CongoTravelDbContext ctx) =>
            new(ctx, CreatePhotoService(ctx), NullLogger<RestaurantEtablissementService>.Instance);

        public static RestaurantInventoryConfirmStrategyFactory CreateConfirmStrategyFactory(CongoTravelDbContext ctx) =>
            new(
                new RestaurantGlobalQuotaConfirmStrategy(ctx),
                new RestaurantClassQuotaConfirmStrategy(ctx));

        public static RestaurantInventoryHoldStrategyFactory CreateHoldStrategyFactory(CongoTravelDbContext ctx) =>
            new(
                new RestaurantGlobalQuotaHoldStrategy(ctx),
                new RestaurantClassQuotaHoldStrategy(ctx));

        public static RestaurantInventoryCancelStrategyFactory CreateCancelStrategyFactory(CongoTravelDbContext ctx) =>
            new(
                new RestaurantGlobalQuotaCancelStrategy(ctx),
                new RestaurantClassQuotaCancelStrategy(ctx));

        public static RestaurantReservationConfirmationService CreateConfirmationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmStrategyFactory(ctx),
                NullLogger<RestaurantReservationConfirmationService>.Instance);

        public static RestaurantPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmationService(ctx),
                NullLogger<RestaurantPaymentService>.Instance);

        public static RestaurantHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateHoldStrategyFactory(ctx),
                new ConfigSocieteService(ctx),
                NullLogger<RestaurantHoldService>.Instance);

        public static RestaurantReservationService CreateReservationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateCancelStrategyFactory(ctx),
                NullLogger<RestaurantReservationService>.Instance);

        public static RestaurantTicketService CreateTicketService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new ConfigSocieteService(ctx),
                NullLogger<RestaurantTicketService>.Instance);

        public static RestaurantFlexPayCallbackService CreateCallbackService(
            CongoTravelDbContext ctx,
            IFlexPayService? flexPayService = null,
            IFlexPayRealtimeNotifier? realtimeNotifier = null) =>
            new(
                ctx,
                flexPayService ?? Mock.Of<IFlexPayService>(),
                CreateConfirmationService(ctx),
                CreateReservationService(ctx),
                realtimeNotifier ?? Mock.Of<IFlexPayRealtimeNotifier>(),
                NullLogger<RestaurantFlexPayCallbackService>.Instance);

        public static Mock<IFlexPayService> CreateFlexPayApiMock(
            string mobileOrderNumber = "FP-RST-001",
            string checkStatus = "0")
        {
            var flexApi = new Mock<IFlexPayService>();
            flexApi
                .Setup(f => f.VerifierStatutTransactionAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayCheckResponseDto
                {
                    Code = checkStatus,
                    Transaction = new FlexPayTransactionDto { Status = checkStatus }
                });
            flexApi
                .Setup(f => f.InitierPaiementMobileMoneyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto
                {
                    Code = "0",
                    OrderNumber = mobileOrderNumber,
                    Message = "OK"
                });
            return flexApi;
        }

        public static RestaurantFlexPayInitiationService CreateFlexPayInitiationService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService,
            bool enabled = true)
        {
            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            return new RestaurantFlexPayInitiationService(
                ctx,
                flexPayService,
                httpAccessor.Object,
                Options.Create(new FlexPayOptions
                {
                    Enabled = enabled,
                    RestaurantEnabled = enabled,
                    CallbackBaseUrl = "https://api.test.example/api/FlexPay/callback",
                    RestaurantCallbackRelativePath = "/api/restaurants/flexpay/callback"
                }),
                new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance),
                CreateConfirmationService(ctx),
                new DeviseMontantConverter(ctx),
                NullLogger<RestaurantFlexPayInitiationService>.Instance);
        }

        public static async Task<(int IdSociete, int IdSite, int IdCreneau)> SeedPublishedCreneauAsync(
            CongoTravelDbContext ctx,
            string suffix,
            int capacite = 20,
            decimal prixUnitaire = 50m,
            decimal acomptePourcent = 20m,
            decimal? montantAcompte = null)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(
                ctx, $"Resto FlexPay {suffix}");

            var etablissementService = RestaurantTestFactories.CreateEtablissementService(ctx);
            var creneauService = new RestaurantCreneauService(
                ctx, NullLogger<RestaurantCreneauService>.Instance);

            var etablissement = await etablissementService.PublishAsync(
                (await etablissementService.CreateDraftAsync(new RestaurantCreateEtablissementRequestDto
                {
                    CodeRestaurant = $"REST-FP-{suffix}",
                    Nom = $"Restaurant FP {suffix}",
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

        public static async Task<(int IdSociete, int IdSite, int IdCreneau)> SeedPublishedCreneauWithFlexPayAsync(
            CongoTravelDbContext ctx,
            string suffix,
            int capacite = 20,
            decimal prixUnitaire = 50m,
            decimal acomptePourcent = 20m)
        {
            var (idSociete, idSite, idCreneau) = await SeedPublishedCreneauAsync(
                ctx, suffix, capacite, prixUnitaire, acomptePourcent);

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeMarchand = "MERCHANT-RST",
                ApiToken = "token-test",
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                Statut = true
            });
            await ctx.SaveChangesAsync();

            return (idSociete, idSite, idCreneau);
        }

        public static async Task<(int IdSociete, int IdReservation, string OrderNumber)> SeedPendingFlexPayPaymentAsync(
            CongoTravelDbContext ctx,
            int quantity,
            string orderNumber = "RST-CALLBACK-TEST-001",
            int? idUtilisateur = null,
            decimal acompteUnitaire = 10m)
        {
            var (idSociete, idSite, idCreneau) = await SeedPublishedCreneauWithFlexPayAsync(
                ctx, Guid.NewGuid().ToString("N")[..6], capacite: 50, prixUnitaire: 50m, acomptePourcent: 20m);

            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    IdSite = idSite,
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            if (idUtilisateur is > 0)
            {
                var reservation = await ctx.RestaurantReservations
                    .FirstAsync(r => r.IdRestaurantReservation == hold.IdRestaurantReservation);
                reservation.IdUtilisateur = idUtilisateur;
            }

            var montant = acompteUnitaire * quantity;
            ctx.RestaurantPayments.Add(new RestaurantPayment
            {
                IdRestaurantReservation = hold.IdRestaurantReservation,
                IdSite = idSite,
                ReferencePaiement = $"RST-PAY-{orderNumber}",
                Provider = RestaurantFlexPayConstants.Provider,
                ProviderTxRef = orderNumber,
                Status = RestaurantPaymentStatus.PENDING,
                Montant = montant,
                CodeDevise = "USD",
                MontantTarif = montant,
                CodeDeviseTarif = "USD",
                TauxVersDevisePaiement = 1m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return (idSociete, hold.IdRestaurantReservation, orderNumber);
        }
    }
}
