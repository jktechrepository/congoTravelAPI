using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageElectronicSupplementEnrichmentTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Fact]
        public async Task Enrich_sets_supplement_from_config()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(Enrich_sets_supplement_from_config)));
            ctx.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = 10,
                MontAddPaieElectronique = 500m,
                CodeDeviseMontAddPaieElectronique = "CDF",
                PoidsBagageParKiloOffert = 20m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var dtos = new List<VoyageResponseDto>
            {
                new() { Id = 1, IdSociete = 10 }
            };

            await VoyageConfigEnrichmentHelper.EnrichElectronicSupplementAsync(ctx, dtos);

            Assert.Equal(500m, dtos[0].MontAddPaieElectronique);
            Assert.Equal("CDF", dtos[0].CodeDeviseMontAddPaieElectronique);
            Assert.Equal(20m, dtos[0].PoidsBagageParKiloOffert);
        }

        [Fact]
        public async Task Enrich_defaults_zero_when_no_config()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(Enrich_defaults_zero_when_no_config)));

            var dtos = new List<VoyageResponseDto>
            {
                new() { Id = 1, IdSociete = 99 }
            };

            await VoyageConfigEnrichmentHelper.EnrichElectronicSupplementAsync(ctx, dtos);

            Assert.Equal(0m, dtos[0].MontAddPaieElectronique);
            Assert.Null(dtos[0].CodeDeviseMontAddPaieElectronique);
            Assert.Equal(0m, dtos[0].PoidsBagageParKiloOffert);
        }

        [Fact]
        public async Task Enrich_batch_multiple_voyages_same_societe()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(Enrich_batch_multiple_voyages_same_societe)));
            ctx.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = 5,
                MontAddPaieElectronique = 250m,
                CodeDeviseMontAddPaieElectronique = "USD",
                PoidsBagageParKiloOffert = 15.5m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var dtos = new List<VoyageResponseDto>
            {
                new() { Id = 1, IdSociete = 5 },
                new() { Id = 2, IdSociete = 5 },
                new() { Id = 3, IdSociete = 5 }
            };

            await VoyageConfigEnrichmentHelper.EnrichElectronicSupplementAsync(ctx, dtos);

            Assert.All(dtos, d =>
            {
                Assert.Equal(250m, d.MontAddPaieElectronique);
                Assert.Equal("USD", d.CodeDeviseMontAddPaieElectronique);
                Assert.Equal(15.5m, d.PoidsBagageParKiloOffert);
            });
        }
    }
}
