using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class OrigineOperationGroupeHelperTests
    {
        [Theory]
        [InlineData(OrigineOperation.CLIENT, OrigineOperationGroupe.CLIENT)]
        [InlineData(OrigineOperation.CAISSIER, OrigineOperationGroupe.AGENT)]
        [InlineData(OrigineOperation.GERANT, OrigineOperationGroupe.AGENT)]
        [InlineData(OrigineOperation.ADMIN, OrigineOperationGroupe.AGENT)]
        [InlineData(OrigineOperation.SUPER_ADMIN, OrigineOperationGroupe.AGENT)]
        [InlineData(OrigineOperation.INCONNU, OrigineOperationGroupe.INCONNU)]
        [InlineData(null, OrigineOperationGroupe.INCONNU)]
        [InlineData("", OrigineOperationGroupe.INCONNU)]
        public void ToGroupe_maps_granular_origine(string? origine, string expected)
        {
            Assert.Equal(expected, OrigineOperationGroupeHelper.ToGroupe(origine));
        }

        [Theory]
        [InlineData("client", true)]
        [InlineData("AGENT", true)]
        [InlineData("inconnu", true)]
        [InlineData("STAFF", false)]
        [InlineData(null, false)]
        public void IsValid_accepts_known_groups(string? value, bool expected)
        {
            Assert.Equal(expected, OrigineOperationGroupe.IsValid(value));
        }

        [Fact]
        public async Task ApplyOrigineGroupeFilter_CLIENT_returns_only_client_payments()
        {
            await using var ctx = BuildDb(nameof(ApplyOrigineGroupeFilter_CLIENT_returns_only_client_payments));
            SeedPaiements(ctx);
            await ctx.SaveChangesAsync();

            var filtered = await OrigineOperationGroupeHelper
                .ApplyOrigineGroupeFilter(ctx.Paiements.AsQueryable(), OrigineOperationGroupe.CLIENT)
                .Select(p => p.IdPaiement)
                .ToListAsync();

            Assert.Single(filtered);
            Assert.Equal(1, filtered[0]);
        }

        [Fact]
        public async Task ApplyOrigineGroupeFilter_AGENT_excludes_client_and_inconnu()
        {
            await using var ctx = BuildDb(nameof(ApplyOrigineGroupeFilter_AGENT_excludes_client_and_inconnu));
            SeedPaiements(ctx);
            await ctx.SaveChangesAsync();

            var filtered = await OrigineOperationGroupeHelper
                .ApplyOrigineGroupeFilter(ctx.Paiements.AsQueryable(), OrigineOperationGroupe.AGENT)
                .Select(p => p.IdPaiement)
                .OrderBy(id => id)
                .ToListAsync();

            Assert.Equal(new[] { 2, 3 }, filtered);
        }

        [Fact]
        public async Task ApplyOrigineGroupeFilter_INCONNU_returns_unclassified()
        {
            await using var ctx = BuildDb(nameof(ApplyOrigineGroupeFilter_INCONNU_returns_unclassified));
            SeedPaiements(ctx);
            await ctx.SaveChangesAsync();

            var filtered = await OrigineOperationGroupeHelper
                .ApplyOrigineGroupeFilter(ctx.Paiements.AsQueryable(), OrigineOperationGroupe.INCONNU)
                .Select(p => p.IdPaiement)
                .ToListAsync();

            Assert.Single(filtered);
            Assert.Equal(4, filtered[0]);
        }

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static void SeedPaiements(CongoTravelDbContext ctx)
        {
            ctx.Paiements.AddRange(
                new Paiement { IdPaiement = 1, IdSociete = 1, IdUtilisateur = 1, Origine = OrigineOperation.CLIENT, Statut = true, IsDeleted = false },
                new Paiement { IdPaiement = 2, IdSociete = 1, IdUtilisateur = 1, Origine = OrigineOperation.CAISSIER, Statut = true, IsDeleted = false },
                new Paiement { IdPaiement = 3, IdSociete = 1, IdUtilisateur = 1, Origine = OrigineOperation.GERANT, Statut = true, IsDeleted = false },
                new Paiement { IdPaiement = 4, IdSociete = 1, IdUtilisateur = 1, Origine = OrigineOperation.INCONNU, Statut = true, IsDeleted = false });
        }
    }
}
