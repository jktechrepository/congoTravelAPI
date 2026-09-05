using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueJourneeDeleteTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static SiteTouristiqueJourneeService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);

        private static async Task<(int IdSociete, int IdLieu, SiteTouristiqueJourneeService JourneeService)>
            SeedPublishedLieuAsync(CongoTravelDbContext ctx, string suffix)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, $"ST Del {suffix}");
            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = CreateService(ctx);

            var code = $"DEL-{suffix}";
            if (code.Length > 10)
                code = code[..10];

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = code,
                Nom = $"Lieu Delete {suffix}",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            return (idSociete, lieu.IdSiteTouristique, journeeService);
        }

        [Fact]
        public async Task DeleteAsync_draft_without_sales_removes_journee()
        {
            await using var ctx = BuildDb(nameof(DeleteAsync_draft_without_sales_removes_journee));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "D1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 1000m
                }
            }, idSociete);

            await svc.DeleteAsync(draft.IdSiteTouristiqueJournee, idSociete);

            Assert.Null(await svc.GetByIdAsync(draft.IdSiteTouristiqueJournee, idSociete));
            Assert.False(await ctx.SiteTouristiqueJournees
                .AnyAsync(j => j.IdSiteTouristiqueJournee == draft.IdSiteTouristiqueJournee));
        }

        [Fact]
        public async Task DeleteAsync_published_without_sales_removes_journee()
        {
            await using var ctx = BuildDb(nameof(DeleteAsync_published_without_sales_removes_journee));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 80,
                    PrixUnitaire = 2000m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            await svc.DeleteAsync(published.IdSiteTouristiqueJournee, idSociete);

            Assert.Null(await svc.GetByIdAsync(published.IdSiteTouristiqueJournee, idSociete));
        }

        [Fact]
        public async Task DeleteAsync_published_with_hold_throws_409()
        {
            await using var ctx = BuildDb(nameof(DeleteAsync_published_with_hold_throws_409));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P2");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 40,
                    PrixUnitaire = 15m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            await SiteTouristiqueTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            await Assert.ThrowsAsync<SiteTouristiqueJourneeConflictException>(() =>
                svc.DeleteAsync(published.IdSiteTouristiqueJournee, idSociete));

            Assert.NotNull(await svc.GetByIdAsync(published.IdSiteTouristiqueJournee, idSociete));
        }

        [Fact]
        public async Task DeleteAsync_wrong_societe_throws_404()
        {
            await using var ctx = BuildDb(nameof(DeleteAsync_wrong_societe_throws_404));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "T1");
            var (idOtherSociete, _) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Other Del");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2027, 2, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.DeleteAsync(draft.IdSiteTouristiqueJournee, idOtherSociete));

            Assert.NotNull(await svc.GetByIdAsync(draft.IdSiteTouristiqueJournee, idSociete));
        }
    }
}
