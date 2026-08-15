using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueLieuHorairesTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task Create_and_update_persists_horaires_in_detail_and_list()
        {
            await using var ctx = BuildDb(nameof(Create_and_update_persists_horaires_in_detail_and_list));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Horaires Co");
            var service = SiteTouristiqueTestFactories.CreateLieuService(ctx);

            var created = await service.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = "LIEU-HOR",
                Nom = "Parc National",
                IdSite = idSite,
                HeureOuverture = new TimeOnly(8, 0),
                HeureFermeture = new TimeOnly(17, 30),
                JourOuverture = "Lun-Dim"
            }, idSociete);

            Assert.Equal(new TimeOnly(8, 0), created.HeureOuverture);
            Assert.Equal(new TimeOnly(17, 30), created.HeureFermeture);
            Assert.Equal("Lun-Dim", created.JourOuverture);

            var listed = Assert.Single(await service.ListAsync(idSociete));
            Assert.Equal(new TimeOnly(8, 0), listed.HeureOuverture);
            Assert.Equal("Lun-Dim", listed.JourOuverture);

            var updated = await service.UpdateAsync(
                created.IdSiteTouristique,
                new SiteTouristiqueUpdateLieuRequestDto
                {
                    Nom = "Parc National",
                    HeureOuverture = new TimeOnly(9, 0),
                    HeureFermeture = new TimeOnly(16, 0),
                    JourOuverture = "  "
                },
                idSociete);

            Assert.NotNull(updated);
            Assert.Equal(new TimeOnly(9, 0), updated!.HeureOuverture);
            Assert.Equal(new TimeOnly(16, 0), updated.HeureFermeture);
            Assert.Null(updated.JourOuverture);
        }

        [Fact]
        public async Task CreateDraftAsync_rejects_fermeture_before_or_equal_ouverture()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_rejects_fermeture_before_or_equal_ouverture));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Bad Horaires");
            var service = SiteTouristiqueTestFactories.CreateLieuService(ctx);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = "LIEU-BAD",
                    Nom = "Lieu invalide",
                    IdSite = idSite,
                    HeureOuverture = new TimeOnly(10, 0),
                    HeureFermeture = new TimeOnly(10, 0)
                }, idSociete));

            Assert.Contains("HeureFermeture", ex.Message);
            Assert.Equal(0, await ctx.SiteTouristiques.CountAsync());
        }
    }
}
