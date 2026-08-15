using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueLieuLocalisationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task Create_and_update_persists_localisation_fields_in_detail_and_list()
        {
            await using var ctx = BuildDb(nameof(Create_and_update_persists_localisation_fields_in_detail_and_list));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Loc Co");
            var service = SiteTouristiqueTestFactories.CreateLieuService(ctx);

            var created = await service.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = "LIEU-LOC",
                Nom = "Chutes de la Lukaya",
                Description = "Site naturel",
                Province = "Kinshasa",
                Ville = "Mont Ngafula",
                Adresse = "Route de Kasangulu",
                Telephone = "+243810000001",
                IdSite = idSite
            }, idSociete);

            Assert.Equal("Kinshasa", created.Province);
            Assert.Equal("Mont Ngafula", created.Ville);
            Assert.Equal("Route de Kasangulu", created.Adresse);
            Assert.Equal("+243810000001", created.Telephone);

            var listed = await service.ListAsync(idSociete);
            var item = Assert.Single(listed);
            Assert.Equal("Kinshasa", item.Province);
            Assert.Equal("Mont Ngafula", item.Ville);
            Assert.Equal("Route de Kasangulu", item.Adresse);
            Assert.Equal("+243810000001", item.Telephone);

            var updated = await service.UpdateAsync(
                created.IdSiteTouristique,
                new SiteTouristiqueUpdateLieuRequestDto
                {
                    Nom = "Chutes de la Lukaya",
                    Description = "Site naturel",
                    Province = "Kongo Central",
                    Ville = "  ",
                    Adresse = "Avenue principale",
                    Telephone = null
                },
                idSociete);

            Assert.NotNull(updated);
            Assert.Equal("Kongo Central", updated!.Province);
            Assert.Null(updated.Ville);
            Assert.Equal("Avenue principale", updated.Adresse);
            Assert.Null(updated.Telephone);

            var entity = await ctx.SiteTouristiques.SingleAsync(l => l.IdSiteTouristique == created.IdSiteTouristique);
            Assert.Equal("Kongo Central", entity.Province);
            Assert.Null(entity.Ville);
            Assert.Equal("Avenue principale", entity.Adresse);
            Assert.Null(entity.Telephone);
        }
    }
}
