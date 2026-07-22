using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementFlexPayInitiationServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task InitiateAsync_creates_pending_payment_and_calls_flexpay_mobile_money()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_creates_pending_payment_and_calls_flexpay_mobile_money));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 2);
            var flexApi = CreateFlexApiMock(mobileOrderNumber: "FP-EVT-001");
            var service = CreateService(ctx, flexApi.Object, enabled: true);

            var result = await service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
            {
                MethodePaiement = "MOBILE_MONEY",
                Phone = "243900000001",
                IdSite = idSite
            });

            Assert.False(result.AlreadyInitiated);
            Assert.True(result.FlexPayAccepted);
            Assert.Equal("FP-EVT-001", result.OrderNumber);
            Assert.Equal("PENDING", result.Payment.Status);
            Assert.Equal("FLEXPAY", result.Payment.Provider);
            Assert.Equal(40m, result.Payment.Montant);
            Assert.Equal(40m, result.MontantFlexPay);
            Assert.Equal("USD", result.CodeDevisePaiement);
            Assert.Equal(40m, result.MontantTarif);
            Assert.Equal("USD", result.CodeDeviseTarif);
            Assert.Equal(1m, result.TauxApplique);
            Assert.Null(result.PaymentUrl);

            flexApi.Verify(f => f.InitierPaiementMobileMoneyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243900000001",
                40m, "USD", It.Is<string>(u => u.Contains("/api/events/flexpay/callback")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_carte_returns_payment_url()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_carte_returns_payment_url));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var flexApi = CreateFlexApiMock(
                cardOrderNumber: "FP-CARD-001",
                paymentUrl: "https://card.flexpay.cd/pay/abc");
            var service = CreateService(ctx, flexApi.Object, enabled: true);

            var result = await service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
            {
                MethodePaiement = "CARTE_BANCAIRE",
                IdSite = idSite
            });

            Assert.Equal("https://card.flexpay.cd/pay/abc", result.PaymentUrl);
            flexApi.Verify(f => f.InitierPaiementCarteV1Async(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string>(u => u.Contains("/api/events/flexpay/callback")),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_replays_idempotency_key()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_replays_idempotency_key));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var flexApi = CreateFlexApiMock(mobileOrderNumber: "FP-IDEM-001");
            var service = CreateService(ctx, flexApi.Object, enabled: true);
            var request = new EvenementInitiateFlexPayRequestDto
            {
                MethodePaiement = "MOBILE_MONEY",
                Phone = "243900000001",
                IdSite = idSite,
                IdempotencyKey = "flex-idem-evt-001"
            };

            var first = await service.InitiateAsync(idReservation, idSociete, request);
            var second = await service.InitiateAsync(idReservation, idSociete, request);

            Assert.False(first.AlreadyInitiated);
            Assert.True(second.AlreadyInitiated);
            Assert.Equal(first.Payment.IdEvenementPayment, second.Payment.IdEvenementPayment);
            Assert.Equal(1, await ctx.EvenementPayments.CountAsync());
            flexApi.Verify(f => f.InitierPaiementMobileMoneyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_converts_tarif_usd_to_paiement_cdf()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_converts_tarif_usd_to_paiement_cdf));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 2);
            // Tarif hold = 40 USD ; 1 USD = 2800 CDF → 112000 CDF
            ctx.TauxChanges.Add(new TauxChange
            {
                IdSociete = idSociete,
                CodeDeviseSource = "USD",
                CodeDeviseCible = "CDF",
                Taux = 2800m,
                DateEffet = DateTime.UtcNow.Date.AddDays(-1),
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var flexApi = CreateFlexApiMock(mobileOrderNumber: "FP-EVT-CDF-001");
            var service = CreateService(ctx, flexApi.Object, enabled: true);

            var result = await service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
            {
                MethodePaiement = "MOBILE_MONEY",
                Phone = "243900000001",
                IdSite = idSite,
                CodeDevisePaiement = "CDF"
            });

            Assert.Equal(40m, result.MontantTarif);
            Assert.Equal("USD", result.CodeDeviseTarif);
            Assert.Equal(112000m, result.MontantFlexPay);
            Assert.Equal("CDF", result.CodeDevisePaiement);
            Assert.Equal(2800m, result.TauxApplique);
            Assert.Equal(112000m, result.Payment.Montant);
            Assert.Equal("CDF", result.Payment.CodeDevise);

            flexApi.Verify(f => f.InitierPaiementMobileMoneyAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243900000001",
                112000m, "CDF", It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_rejects_invalid_code_devise_paiement()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_rejects_invalid_code_devise_paiement));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var service = CreateService(ctx, Mock.Of<IFlexPayService>(), enabled: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite,
                    CodeDevisePaiement = "EUR"
                }));
        }

        [Fact]
        public async Task InitiateAsync_rejects_when_event_flexpay_disabled()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_rejects_when_event_flexpay_disabled));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var service = CreateService(ctx, Mock.Of<IFlexPayService>(), enabled: false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }));
        }

        [Fact]
        public async Task InitiateAsync_rejects_expired_hold()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_rejects_expired_hold));
            var (idSociete, idSite, idReservation) = await SeedExpiredHoldWithFlexPayConfigAsync(ctx);
            var service = CreateService(ctx, Mock.Of<IFlexPayService>(), enabled: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }));
        }

        [Fact]
        public async Task InitiateAsync_rejects_duplicate_pending_payment()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_rejects_duplicate_pending_payment));
            var (idSociete, idSite, idReservation) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            ctx.EvenementPayments.Add(new EvenementPayment
            {
                IdEvenementReservation = idReservation,
                ReferencePaiement = "EVT-PAY-PENDING-DUP",
                Provider = "FLEXPAY",
                ProviderTxRef = "PENDING-EVT-1",
                Status = EvenementPaymentStatus.PENDING,
                Montant = 20m,
                CodeDevise = "USD",
                MontantTarif = 20m,
                CodeDeviseTarif = "USD",
                TauxVersDevisePaiement = 1m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx, Mock.Of<IFlexPayService>(), enabled: true);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }));
        }

        private static Mock<IFlexPayService> CreateFlexApiMock(
            string? mobileOrderNumber = null,
            string? cardOrderNumber = null,
            string? paymentUrl = null)
        {
            var flexApi = new Mock<IFlexPayService>();
            if (mobileOrderNumber != null)
            {
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
            }

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
                        PaymentUrl = paymentUrl,
                        Message = "OK"
                    });
            }

            return flexApi;
        }

        private static EvenementFlexPayInitiationService CreateService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService,
            bool enabled)
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
                EvenementTestFactories.CreateConfirmationService(ctx),
                new DeviseMontantConverter(ctx),
                NullLogger<EvenementFlexPayInitiationService>.Instance);
        }

        private static async Task<(int IdSociete, int IdSite, int IdReservation)> SeedExpiredHoldWithFlexPayConfigAsync(
            CongoTravelDbContext ctx)
        {
            var (idSociete, idSite, _) = await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var reservation = await ctx.EvenementReservations.SingleAsync();
            reservation.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5);
            await ctx.SaveChangesAsync();
            return (idSociete, idSite, reservation.IdEvenementReservation);
        }
    }
}
