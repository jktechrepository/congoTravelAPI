using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueJourneeCloseTests
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
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, $"ST Clo {suffix}");
            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = CreateService(ctx);

            var code = $"CLO-{suffix}";
            if (code.Length > 10)
                code = code[..10];

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = code,
                Nom = $"Lieu Close {suffix}",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            return (idSociete, lieu.IdSiteTouristique, journeeService);
        }

        [Fact]
        public async Task CloseAsync_published_becomes_closed_and_leaves_catalog()
        {
            await using var ctx = BuildDb(nameof(CloseAsync_published_becomes_closed_and_leaves_catalog));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 40,
                    PrixUnitaire = 800m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            var closed = await svc.CloseAsync(published.IdSiteTouristiqueJournee, idSociete);

            Assert.Equal("Closed", closed.Status);
            Assert.Null(await svc.GetPublishedByIdAsync(published.IdSiteTouristiqueJournee));

            var entity = await ctx.SiteTouristiqueJournees
                .AsNoTracking()
                .FirstAsync(j => j.IdSiteTouristiqueJournee == published.IdSiteTouristiqueJournee);
            Assert.Equal(SiteTouristiqueStatus.Closed, entity.Status);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SiteTouristiqueJourneeSalesEligibilityHelper.EnsureCanSell(entity, DateTime.UtcNow));
            Assert.Contains("Published", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CloseAsync_draft_becomes_closed()
        {
            await using var ctx = BuildDb(nameof(CloseAsync_draft_becomes_closed));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "D1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            var closed = await svc.CloseAsync(draft.IdSiteTouristiqueJournee, idSociete);
            Assert.Equal("Closed", closed.Status);
        }

        [Fact]
        public async Task CloseAsync_already_closed_is_idempotent()
        {
            await using var ctx = BuildDb(nameof(CloseAsync_already_closed_is_idempotent));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "I1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            var first = await svc.CloseAsync(draft.IdSiteTouristiqueJournee, idSociete);
            var second = await svc.CloseAsync(draft.IdSiteTouristiqueJournee, idSociete);

            Assert.Equal("Closed", first.Status);
            Assert.Equal("Closed", second.Status);
        }

        [Fact]
        public async Task CloseAsync_cancelled_throws_400()
        {
            await using var ctx = BuildDb(nameof(CloseAsync_cancelled_throws_400));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "C1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 3),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);
            await svc.CancelAsync(draft.IdSiteTouristiqueJournee, idSociete);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CloseAsync(draft.IdSiteTouristiqueJournee, idSociete));
            Assert.Contains("Cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CloseAsync_wrong_societe_throws_404()
        {
            await using var ctx = BuildDb(nameof(CloseAsync_wrong_societe_throws_404));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "T1");
            var (idOther, _) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Other Clo");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 4),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.CloseAsync(draft.IdSiteTouristiqueJournee, idOther));
        }
    }
}
