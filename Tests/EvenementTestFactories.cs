using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;

namespace CongoTravel.Tests
{
    internal static class EvenementTestFactories
    {
        public static EvenementInventoryConfirmStrategyFactory CreateConfirmStrategyFactory(CongoTravelDbContext ctx) =>
            new(
                new EvenementGlobalQuotaConfirmStrategy(ctx),
                new EvenementClassQuotaConfirmStrategy(ctx),
                new EvenementSeatNumberedConfirmStrategy(ctx));

        public static EvenementReservationConfirmationService CreateConfirmationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmStrategyFactory(ctx),
                NullLogger<EvenementReservationConfirmationService>.Instance);

        public static EvenementPaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmationService(ctx),
                NullLogger<EvenementPaymentService>.Instance);

        public static EvenementFlexPayCallbackService CreateCallbackService(
            CongoTravelDbContext ctx,
            IFlexPayService? flexPayService = null) =>
            new(
                ctx,
                flexPayService ?? Mock.Of<IFlexPayService>(),
                CreateConfirmationService(ctx),
                NullLogger<EvenementFlexPayCallbackService>.Instance);

        public static IFlexPayService CreateFlexPayCheckMock(string checkStatus) =>
            CreateFlexPayCheckMockBuilder(checkStatus).Object;

        public static Mock<IFlexPayService> CreateFlexPayCheckMockBuilder(string checkStatus)
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
            return flexApi;
        }

        public static Mock<IFlexPayService> CreateFlexPayApiMock(
            string mobileOrderNumber = "FP-SMOKE-001",
            string? cardOrderNumber = null,
            string? cardPaymentUrl = null,
            string checkStatus = "0")
        {
            var flexApi = CreateFlexPayCheckMockBuilder(checkStatus);
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

            if (cardOrderNumber != null)
            {
                flexApi
                    .Setup(f => f.InitierPaiementCarteV1Async(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new FlexPayPaymentResponseDto
                    {
                        Code = "0",
                        OrderNumber = cardOrderNumber,
                        PaymentUrl = cardPaymentUrl,
                        Message = "OK"
                    });
            }

            return flexApi;
        }

        public static EvenementFlexPayInitiationService CreateFlexPayInitiationService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService,
            bool enabled = true)
        {
            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            return new EvenementFlexPayInitiationService(
                ctx,
                flexPayService,
                httpAccessor.Object,
                Options.Create(new FlexPayOptions
                {
                    Enabled = enabled,
                    EventEnabled = enabled,
                    CallbackBaseUrl = "https://api.test.example/api/FlexPay/callback",
                    EventCallbackRelativePath = "/api/events/flexpay/callback"
                }),
                new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance),
                CreateConfirmationService(ctx),
                new DeviseMontantConverter(ctx),
                NullLogger<EvenementFlexPayInitiationService>.Instance);
        }

        public static async Task<(int IdSociete, int IdSite, int IdReservation)> SeedHoldWithFlexPayConfigAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var societe = new Societe { Nom = "EVT FlexPay", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var site = new Site
            {
                IdSociete = societe.IdSociete,
                NomSite = "Site EVT",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
                CodeMarchand = "MERCHANT-EVT",
                ApiToken = "token-test",
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                Statut = true
            });

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"FP-{Guid.NewGuid():N}"[..10],
                Libelle = "FlexPay test",
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
                NullLogger<EvenementHoldService>.Instance);

            var hold = await holdService.CreateHoldAsync(
                session.IdEvenementSession,
                societe.IdSociete,
                new EvenementHoldRequestDto
                {
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (societe.IdSociete, site.IdSite, hold.IdEvenementReservation);
        }

        public static async Task<(int IdSociete, int IdReservation, string OrderNumber)> SeedPendingFlexPayPaymentAsync(
            CongoTravelDbContext ctx,
            int quantity,
            string orderNumber = "FP-CALLBACK-TEST-001")
        {
            var (idSociete, _, idReservation) = await SeedHoldWithFlexPayConfigAsync(ctx, quantity);

            ctx.EvenementPayments.Add(new EvenementPayment
            {
                IdEvenementReservation = idReservation,
                ReferencePaiement = $"EVT-PAY-{orderNumber}",
                Provider = "FLEXPAY",
                ProviderTxRef = orderNumber,
                Status = EvenementPaymentStatus.PENDING,
                Montant = 20m * quantity,
                CodeDevise = "USD",
                MontantTarif = 20m * quantity,
                CodeDeviseTarif = "USD",
                TauxVersDevisePaiement = 1m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return (idSociete, idReservation, orderNumber);
        }
    }
}
