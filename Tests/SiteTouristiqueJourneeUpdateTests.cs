using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueJourneeUpdateTests
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
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, $"ST Update {suffix}");
            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = CreateService(ctx);

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"UPD-{suffix}"[..Math.Min(10, $"UPD-{suffix}".Length)],
                Nom = $"Lieu Update {suffix}",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            return (idSociete, lieu.IdSiteTouristique, journeeService);
        }

        [Fact]
        public async Task UpdateAsync_draft_updates_date_and_capacity()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_draft_updates_date_and_capacity));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "D1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 10, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 100,
                    PrixUnitaire = 5000m
                }
            }, idSociete);

            var updated = await svc.UpdateAsync(
                draft.IdSiteTouristiqueJournee,
                new SiteTouristiqueUpdateJourneeRequestDto
                {
                    DateVisite = new DateOnly(2026, 10, 15),
                    CodeDevise = "USD",
                    GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                    {
                        CapaciteTotale = 200,
                        PrixUnitaire = 10m
                    }
                },
                idSociete);

            Assert.Equal("Draft", updated.Status);
            Assert.Equal(new DateOnly(2026, 10, 15), updated.DateVisite);
            Assert.Equal("USD", updated.CodeDevise);
            Assert.NotNull(updated.GlobalQuota);
            Assert.Equal(200, updated.GlobalQuota!.CapaciteTotale);
            Assert.Equal(10m, updated.GlobalQuota.PrixUnitaire);
        }

        [Fact]
        public async Task UpdateAsync_draft_date_conflict_throws_409()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_draft_date_conflict_throws_409));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "D2");

            await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 1000m
                }
            }, idSociete);

            var draft2 = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 11, 2),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 1000m
                }
            }, idSociete);

            await Assert.ThrowsAsync<SiteTouristiqueJourneeConflictException>(() =>
                svc.UpdateAsync(
                    draft2.IdSiteTouristiqueJournee,
                    new SiteTouristiqueUpdateJourneeRequestDto
                    {
                        DateVisite = new DateOnly(2026, 11, 1)
                    },
                    idSociete));
        }

        [Fact]
        public async Task UpdateAsync_published_without_sales_updates_sales_and_capacity()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_published_without_sales_updates_sales_and_capacity));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 12, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 80,
                    PrixUnitaire = 2000m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            var open = DateTime.UtcNow.AddDays(1);
            var close = DateTime.UtcNow.AddDays(10);
            var updated = await svc.UpdateAsync(
                published.IdSiteTouristiqueJournee,
                new SiteTouristiqueUpdateJourneeRequestDto
                {
                    SalesOpenAtUtc = open,
                    SalesCloseAtUtc = close,
                    GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                    {
                        CapaciteTotale = 120,
                        PrixUnitaire = 2500m
                    }
                },
                idSociete);

            Assert.Equal("Published", updated.Status);
            Assert.Equal(120, updated.GlobalQuota!.CapaciteTotale);
            Assert.Equal(2500m, updated.GlobalQuota.PrixUnitaire);
            Assert.NotNull(updated.SalesOpenAtUtc);
            Assert.NotNull(updated.SalesCloseAtUtc);
        }

        [Fact]
        public async Task UpdateAsync_published_with_hold_rejects_capacity_but_allows_sales()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_published_with_hold_rejects_capacity_but_allows_sales));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P2");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 20m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            await SiteTouristiqueTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                published.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 2 } }
                });

            await Assert.ThrowsAsync<SiteTouristiqueJourneeConflictException>(() =>
                svc.UpdateAsync(
                    published.IdSiteTouristiqueJournee,
                    new SiteTouristiqueUpdateJourneeRequestDto
                    {
                        GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                        {
                            CapaciteTotale = 99,
                            PrixUnitaire = 30m
                        }
                    },
                    idSociete));

            var salesOnly = await svc.UpdateAsync(
                published.IdSiteTouristiqueJournee,
                new SiteTouristiqueUpdateJourneeRequestDto
                {
                    SalesOpenAtUtc = DateTime.UtcNow.AddHours(1),
                    SalesCloseAtUtc = DateTime.UtcNow.AddDays(3)
                },
                idSociete);

            Assert.Equal(50, salesOnly.GlobalQuota!.CapaciteTotale);
            Assert.NotNull(salesOnly.SalesOpenAtUtc);
            Assert.NotNull(salesOnly.SalesCloseAtUtc);
        }

        [Fact]
        public async Task UpdateAsync_published_rejects_date_change()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_published_rejects_date_change));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "P3");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 12, 10),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);
            var published = await svc.PublishAsync(draft.IdSiteTouristiqueJournee, idSociete);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.UpdateAsync(
                    published.IdSiteTouristiqueJournee,
                    new SiteTouristiqueUpdateJourneeRequestDto
                    {
                        DateVisite = new DateOnly(2026, 12, 20)
                    },
                    idSociete));

            Assert.Contains("DateVisite", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_closed_throws_400()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_closed_throws_400));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "C1");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2026, 12, 25),
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
                svc.UpdateAsync(
                    draft.IdSiteTouristiqueJournee,
                    new SiteTouristiqueUpdateJourneeRequestDto
                    {
                        SalesOpenAtUtc = DateTime.UtcNow.AddDays(1)
                    },
                    idSociete));

            Assert.Contains("Closed", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateAsync_wrong_societe_throws_404()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_wrong_societe_throws_404));
            var (idSociete, idLieu, svc) = await SeedPublishedLieuAsync(ctx, "T1");
            var (idOtherSociete, _) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "Other ST");

            var draft = await svc.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = idLieu,
                DateVisite = new DateOnly(2027, 1, 1),
                InventoryMode = "GlobalQuota",
                CodeDevise = "CDF",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 100m
                }
            }, idSociete);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                svc.UpdateAsync(
                    draft.IdSiteTouristiqueJournee,
                    new SiteTouristiqueUpdateJourneeRequestDto
                    {
                        GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                        {
                            CapaciteTotale = 20,
                            PrixUnitaire = 100m
                        }
                    },
                    idOtherSociete));
        }
    }
}
