using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class CollecteOrigineGroupeMetricsHelperTests
    {
        [Fact]
        public void BuildResult_always_returns_three_groups()
        {
            var (items, synthese) = CollecteOrigineGroupeMetricsHelper.BuildResult(
                new Dictionary<string, (decimal Montant, int Count)>(),
                new Dictionary<string, (decimal Montant, int Count)>());

            Assert.Equal(3, items.Count);
            Assert.Equal(OrigineOperationGroupe.All, items.Select(i => i.OrigineGroupe).ToArray());
            Assert.Equal(0m, synthese.MontantClassifie);
            Assert.Equal(0m, synthese.PartDigitalPourcentage);
        }

        [Fact]
        public void BuildResult_computes_parts_and_excludes_inconnu_from_digital_kpi()
        {
            var current = new Dictionary<string, (decimal Montant, int Count)>
            {
                [OrigineOperationGroupe.CLIENT] = (300m, 3),
                [OrigineOperationGroupe.AGENT] = (700m, 7),
                [OrigineOperationGroupe.INCONNU] = (100m, 1)
            };
            var previous = new Dictionary<string, (decimal Montant, int Count)>
            {
                [OrigineOperationGroupe.CLIENT] = (200m, 2),
                [OrigineOperationGroupe.AGENT] = (600m, 6),
                [OrigineOperationGroupe.INCONNU] = (50m, 1)
            };

            var (items, synthese) = CollecteOrigineGroupeMetricsHelper.BuildResult(current, previous);

            var client = items.Single(i => i.OrigineGroupe == OrigineOperationGroupe.CLIENT);
            Assert.Equal(300m, client.Montant);
            Assert.Equal(200m, client.MontantMoisPrecedent);
            Assert.Equal(50m, client.VariationPourcentage);
            Assert.Equal(27.27m, client.PartPourcentage);

            Assert.Equal(1000m, synthese.MontantClassifie);
            Assert.Equal(100m, synthese.MontantNonClassifie);
            Assert.Equal(30m, synthese.PartDigitalPourcentage);
            Assert.Equal(70m, synthese.PartGuichetPourcentage);
        }

        [Fact]
        public async Task GetCollecteParOrigineGroupeAsync_filters_by_societe_and_period()
        {
            await using var ctx = BuildDb(nameof(GetCollecteParOrigineGroupeAsync_filters_by_societe_and_period));
            SeedPaiements(ctx);
            await ctx.SaveChangesAsync();

            var monthStart = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonth = monthStart.AddMonths(1);
            var previousMonth = monthStart.AddMonths(-1);

            var (items, synthese) = await CollecteOrigineGroupeMetricsHelper.GetCollecteParOrigineGroupeAsync(
                ctx, societeId: 1, monthStart, nextMonth, previousMonth);

            var agent = items.Single(i => i.OrigineGroupe == OrigineOperationGroupe.AGENT);
            var client = items.Single(i => i.OrigineGroupe == OrigineOperationGroupe.CLIENT);

            Assert.Equal(5000m, agent.Montant);
            Assert.Equal(1, agent.NombrePaiements);
            Assert.Equal(3000m, client.Montant);
            Assert.Equal(1, client.NombrePaiements);
            Assert.Equal(8000m, synthese.MontantClassifie);
            Assert.Equal(37.5m, synthese.PartDigitalPourcentage);
            Assert.Equal(62.5m, synthese.PartGuichetPourcentage);
        }

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static void SeedPaiements(CongoTravelDbContext ctx)
        {
            var monthStart = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonth = monthStart.AddMonths(-1);

            ctx.Paiements.AddRange(
                new Paiement
                {
                    IdPaiement = 1,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    Origine = OrigineOperation.CAISSIER,
                    MontantPaye = 5000,
                    MontantPayeDevisePrincipale = 5000,
                    DatePaiement = monthStart.AddDays(5),
                    Statut = true,
                    IsDeleted = false
                },
                new Paiement
                {
                    IdPaiement = 2,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    Origine = OrigineOperation.CLIENT,
                    MontantPaye = 3000,
                    MontantPayeDevisePrincipale = 3000,
                    DatePaiement = monthStart.AddDays(10),
                    Statut = true,
                    IsDeleted = false
                },
                new Paiement
                {
                    IdPaiement = 3,
                    IdSociete = 2,
                    IdUtilisateur = 2,
                    Origine = OrigineOperation.CLIENT,
                    MontantPaye = 9999,
                    MontantPayeDevisePrincipale = 9999,
                    DatePaiement = monthStart.AddDays(10),
                    Statut = true,
                    IsDeleted = false
                },
                new Paiement
                {
                    IdPaiement = 4,
                    IdSociete = 1,
                    IdUtilisateur = 1,
                    Origine = OrigineOperation.CAISSIER,
                    MontantPaye = 1000,
                    MontantPayeDevisePrincipale = 1000,
                    DatePaiement = previousMonth.AddDays(15),
                    Statut = true,
                    IsDeleted = false
                });
        }
    }
}
