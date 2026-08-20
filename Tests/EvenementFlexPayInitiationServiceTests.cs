using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>Plan A : initiation FlexPay sur HOLD existante refusée (façade with-paiement-electronique uniquement).</summary>
    public class EvenementFlexPayInitiationServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task InitiateAsync_on_existing_hold_is_refused_plan_a()
        {
            await using var ctx = BuildDb(nameof(InitiateAsync_on_existing_hold_is_refused_plan_a));
            var (idSociete, idSite, idReservation) =
                await EvenementTestFactories.SeedHoldWithFlexPayConfigAsync(ctx, quantity: 1);

            var service = new EvenementFlexPayInitiationService(
                ctx,
                Mock.Of<IFlexPayService>(),
                Mock.Of<IHttpContextAccessor>(),
                Options.Create(new FlexPayOptions { Enabled = true, EventEnabled = true }),
                Mock.Of<IInfoPaiementResolutionService>(),
                EvenementTestFactories.CreateConfirmationService(ctx),
                Mock.Of<IDeviseMontantConverter>(),
                NullLogger<EvenementFlexPayInitiationService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateAsync(idReservation, idSociete, new EvenementInitiateFlexPayRequestDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }));

            Assert.Contains("with-paiement-electronique", ex.Message);
        }
    }
}
