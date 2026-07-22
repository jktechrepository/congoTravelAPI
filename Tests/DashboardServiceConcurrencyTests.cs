using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Régression : requêtes séquentielles + pas de DTO vide silencieux.
    /// </summary>
    public class DashboardServiceConcurrencyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task GetDashboardDataAsync_returns_populated_dto_not_empty_fallback()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_returns_populated_dto_not_empty_fallback));

            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Soc",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Agents.Add(new Agent
            {
                IdAgent = 1,
                IdSociete = 1,
                NomComplet = "Agent",
                DateNaissance = new DateTime(1990, 1, 1),
                Statut = true
            });
            await ctx.SaveChangesAsync();

            var result = await DashboardEnrichmentTestHelper.CreateTransportDashboardService(ctx)
                .GetDashboardDataAsync(1);

            Assert.Equal(1, result.TotalAgents);
            Assert.False(string.IsNullOrWhiteSpace(result.CollecteMois.MoisLabel));
        }
    }
}
