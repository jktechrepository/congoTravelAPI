using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class ElectronicPaymentSupplementTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Fact]
        public async Task Helper_computes_supplement_per_place_same_currency()
        {
            var config = new ConfigSociete
            {
                MontAddPaieElectronique = 500m,
                CodeDeviseMontAddPaieElectronique = "CDF"
            };

            var supplement = await ElectronicPaymentSupplementHelper.ComputeSupplementInVoyageCurrencyAsync(
                config,
                nombreDePlace: 2,
                codeDeviseVoyage: "CDF",
                idSociete: 1,
                new DeviseMontantConverter(new CongoTravelDbContext(Options(nameof(Helper_computes_supplement_per_place_same_currency)))),
                DateTime.UtcNow);

            Assert.Equal(1000m, supplement);
        }

        [Fact]
        public async Task Helper_returns_zero_when_montant_zero()
        {
            var config = new ConfigSociete { MontAddPaieElectronique = 0m };

            var supplement = await ElectronicPaymentSupplementHelper.ComputeSupplementInVoyageCurrencyAsync(
                config,
                nombreDePlace: 2,
                codeDeviseVoyage: "CDF",
                idSociete: 1,
                new DeviseMontantConverter(new CongoTravelDbContext(Options(nameof(Helper_returns_zero_when_montant_zero)))),
                DateTime.UtcNow);

            Assert.Equal(0m, supplement);
        }

        [Fact]
        public async Task Helper_converts_supplement_to_voyage_currency()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(Helper_converts_supplement_to_voyage_currency)));
            ctx.TauxChanges.Add(new TauxChange
            {
                IdSociete = 1,
                CodeDeviseSource = "CDF",
                CodeDeviseCible = "USD",
                Taux = 0.0004m,
                DateEffet = DateTime.UtcNow.Date.AddDays(-1),
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var config = new ConfigSociete
            {
                MontAddPaieElectronique = 2500m,
                CodeDeviseMontAddPaieElectronique = "CDF"
            };

            var supplement = await ElectronicPaymentSupplementHelper.ComputeSupplementInVoyageCurrencyAsync(
                config,
                nombreDePlace: 1,
                codeDeviseVoyage: "USD",
                idSociete: 1,
                new DeviseMontantConverter(ctx),
                DateTime.UtcNow);

            Assert.Equal(1.00m, supplement);
        }
    }
}
