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
    public class SiteTouristiqueJourneeCancelTests
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
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, $"ST Can {suffix}");
            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = CreateService(ctx);

            var code = $"CAN-{suffix}";
            if (code.Length > 10)
                code = code[..10];

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = code,
                Nom = $"Lieu Cancel {suffix}",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            return (idSociete, lieu.IdSiteTouristique, journeeService);
        }

        [Fact]
        public async Task CancelAsync_published_becomes_cancelled_and_leaves_published_catalog()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_published_becomes_cancelled_and_leaves_published_catalog));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 1000m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            var cancelled = await svc.CancelAsync(published.IdSiteTouristiqueJournee, idSociete);

            Assert.Equal("Cancelled", cancelled.Status);
            Assert.Null(await svc.GetPublishedByIdAsync(published.IdSiteTouristiqueJournee));

            var catalog = await svc.ListPublishedGlobalAsync(
                new SiteTouristiqueJourneeListFilter { IdSociete = idSociete });
            Assert.DoesNotContain(catalog, j => j.IdSiteTouristiqueJournee == published.IdSiteTouristiqueJournee);

            var entity = await ctx.SiteTouristiqueJournees
                .AsNoTracking()
                .FirstAsync(j => j.IdSiteTouristiqueJournee == published.IdSiteTouristiqueJournee);
            Assert.Equal(SiteTouristiqueStatus.Cancelled, entity.Status);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                SiteTouristiqueJourneeSalesEligibilityHelper.EnsureCanSell(entity, DateTime.UtcNow));
            Assert.Contains("Published", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CancelAsync_draft_becomes_cancelled()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_draft_becomes_cancelled));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "D1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 20,
                    PrixUnitaire = 500m
                }
            }, idSociete);

            var cancelled = await svc.CancelAsync(draft.IdSiteTouristiqueJournee, idSociete);
            Assert.Equal("Cancelled", cancelled.Status);
        }

        [Fact]
        public async Task CancelAsync_already_cancelled_is_idempotent()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_already_cancelled_is_idempotent));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "I1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            var first = await svc.CancelAsync(draft.IdSiteTouristiqueJournee, idSociete);
            var second = await svc.CancelAsync(draft.IdSiteTouristiqueJournee, idSociete);

            Assert.Equal("Cancelled", first.Status);
            Assert.Equal("Cancelled", second.Status);
            Assert.Equal(first.IdSiteTouristiqueJournee, second.IdSiteTouristiqueJournee);
        }

        [Fact]
        public async Task CancelAsync_closed_throws_400()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_closed_throws_400));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "C1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 3),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            var entity = await ctx.SiteTouristiqueJournees
                .FirstAsync(j => j.IdSiteTouristiqueJournee == draft.IdSiteTouristiqueJournee);
            entity.Status = SiteTouristiqueStatus.Closed;
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CancelAsync(draft.IdSiteTouristiqueJournee, idSociete));
            Assert.Contains("Closed", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CancelAsync_wrong_societe_throws_404()
        {
            await using var ctx = BuildDb(nameof(CancelAsync_wrong_societe_throws_404));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "T1");
            var (idOther, _) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Other Can");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 4),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.CancelAsync(draft.IdSiteTouristiqueJournee, idOther));
        }
    }
}
