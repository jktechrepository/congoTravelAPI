using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueFlexPayInitiationServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task InitiateAsync_converts_tarif_usd_to_paiement_cdf()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_converts_tarif_usd_to_paiement_cdf));
            var (idSociete, idSite, idReservation) =
                await SiteTouristiqueTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 2);
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

            var flexApi = SiteTouristiqueTestFactories.CreateFlexPayApiMock("FP-ST-CDF-001");
            var service = SiteTouristiqueTestFactories.CreateFlexPayInitiationService(ctx, flexApi.Object);

            var result = await service.InitiateAsync(idReservation, idSociete, new SiteTouristiqueInitiateFlexPayRequestDto
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
            var (idSociete, idSite, idReservation) =
                await SiteTouristiqueTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);
            var service = SiteTouristiqueTestFactories.CreateFlexPayInitiationService(
                ctx, Mock.Of<IFlexPayService>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new SiteTouristiqueInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite,
                    CodeDevisePaiement = "EUR"
                }));
        }

        [Fact]
        public async Task InitiateAsync_rejects_currency_not_supported_by_mobile_money_channel()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_rejects_currency_not_supported_by_mobile_money_channel));
            var (idSociete, idSite, idReservation) =
                await SiteTouristiqueTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);

            var flexApi = SiteTouristiqueTestFactories.CreateFlexPayApiMock();
            var service = SiteTouristiqueTestFactories.CreateFlexPayInitiationService(
                ctx,
                flexApi.Object,
                flexPayOptions: new FlexPayOptions
                {
                    Enabled = true,
                    SiteTouristiqueEnabled = true,
                    MobileMoneySupportedCurrencies = new List<string> { "CDF" }
                });

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new SiteTouristiqueInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite,
                    CodeDevisePaiement = "USD"
                }));

            Assert.Contains("USD", ex.Message);
            Assert.Contains("MOBILE_MONEY", ex.Message);
        }
    }

    public class SiteTouristiqueFlexPayCallbackServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ProcessCallbackAsync_rejects_currency_mismatch()
        {
            await using var ctx = BuildDb(nameof(ProcessCallbackAsync_rejects_currency_mismatch));
            var (_, _, orderNumber) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = SiteTouristiqueTestFactories.CreateCallbackService(ctx);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = "20",
                Currency = "CDF"
            });

            Assert.False(result.Success);
            Assert.Contains("devise callback", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task VerifyAndFinalizeAsync_rejects_currency_mismatch_from_provider()
        {
            await using var ctx = BuildDb(nameof(VerifyAndFinalizeAsync_rejects_currency_mismatch_from_provider));
            var (idSociete, _, orderNumber) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);
            var service = SiteTouristiqueTestFactories.CreateCallbackService(
                ctx,
                SiteTouristiqueTestFactories.CreateFlexPayCheckMockBuilder("0", amount: "20", currency: "CDF").Object);

            var result = await service.VerifyAndFinalizeAsync(orderNumber, idSociete);

            Assert.False(result.IsConfirmSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.False(result.StatusOnly!.Success);
            Assert.Contains("devise callback", result.StatusOnly.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(SiteTouristiquePaymentStatus.PENDING,
                await ctx.SiteTouristiquePayments.Select(p => p.Status).SingleAsync());
        }
    }
}
