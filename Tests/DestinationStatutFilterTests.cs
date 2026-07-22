using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class DestinationStatutFilterTests
    {
        private static CongoTravelDbContext BuildDb(string testName) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase($"DestinationStatutFilterTests_{testName}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static DestinationService BuildService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<DestinationService>.Instance);

        private static async Task<int> SeedSocieteWithActiveAndInactiveDestinationsAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe
            {
                Nom = "Soc",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            ctx.Destinations.AddRange(
                new Destination
                {
                    IdSociete = societe.IdSociete,
                    VilleDepart = "Kinshasa",
                    VilleArrivee = "Goma",
                    Montant = 50m,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new Destination
                {
                    IdSociete = societe.IdSociete,
                    VilleDepart = "Kinshasa",
                    VilleArrivee = "Lubumbashi",
                    Montant = 60m,
                    Statut = false,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            return societe.IdSociete;
        }

        [Fact]
        public async Task GetAllAsync_returns_only_destinations_with_statut_true()
        {
            await using var ctx = BuildDb(nameof(GetAllAsync_returns_only_destinations_with_statut_true));
            await SeedSocieteWithActiveAndInactiveDestinationsAsync(ctx);

            var svc = BuildService(ctx);
            var result = (await svc.GetAllAsync()).ToList();

            Assert.Single(result);
            Assert.Equal("Goma", result[0].VilleArrivee);
            Assert.True(result[0].Statut);
        }

        [Fact]
        public async Task GetBySocieteAsync_returns_only_destinations_with_statut_true()
        {
            await using var ctx = BuildDb(nameof(GetBySocieteAsync_returns_only_destinations_with_statut_true));
            var idSociete = await SeedSocieteWithActiveAndInactiveDestinationsAsync(ctx);

            var svc = BuildService(ctx);
            var result = (await svc.GetBySocieteAsync(idSociete)).ToList();

            Assert.Single(result);
            Assert.Equal(idSociete, result[0].IdSociete);
            Assert.Equal("Goma", result[0].VilleArrivee);
            Assert.True(result[0].Statut);
        }

        [Fact]
        public async Task GetBySocietePagedAsync_returns_only_destinations_with_statut_true()
        {
            await using var ctx = BuildDb(nameof(GetBySocietePagedAsync_returns_only_destinations_with_statut_true));
            var idSociete = await SeedSocieteWithActiveAndInactiveDestinationsAsync(ctx);

            var svc = BuildService(ctx);
            var result = await svc.GetBySocietePagedAsync(idSociete, new PagedRequest { PageNumber = 1, PageSize = 20 });
            var items = result.Data.ToList();

            Assert.Equal(1, result.TotalCount);
            Assert.Single(items);
            Assert.Equal(idSociete, items[0].IdSociete);
            Assert.Equal("Goma", items[0].VilleArrivee);
            Assert.True(items[0].Statut);
        }

        [Fact]
        public async Task GetByIdAsync_still_returns_inactive_destination_by_id()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_still_returns_inactive_destination_by_id));
            var societe = new Societe { Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var inactive = new Destination
            {
                IdSociete = societe.IdSociete,
                VilleDepart = "Kinshasa",
                VilleArrivee = "Bukavu",
                Montant = 40m,
                Statut = false,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(inactive);
            await ctx.SaveChangesAsync();

            var svc = BuildService(ctx);
            var result = await svc.GetByIdAsync(inactive.IdDestination);

            Assert.NotNull(result);
            Assert.False(result!.Statut);
        }
    }
}
