using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class CaissierDashboardMonthlyTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockCaissier(int userId = 10)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(UserRoles.CAISSIER);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(false);
            mock.SetupGet(x => x.SocieteId).Returns(1);
            mock.SetupGet(x => x.UserId).Returns(userId);
            return mock;
        }

        [Fact]
        public async Task PerformancesMensuelles_splits_current_and_previous_month_encaissements()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_splits_current_and_previous_month_encaissements));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            var (_, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 3000,
                MontantPaye = 3000,
                MontantAPayeDevisePrincipale = 3000,
                MontantPayeDevisePrincipale = 3000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                DateCreation = previousMonthStartUtc.AddDays(5),
                DatePaiement = previousMonthStartUtc.AddDays(5),
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.PerformancesMensuelles.MoisEnCours.TotalEncaissements);
            Assert.Equal(3000m, result.PerformancesMensuelles.MoisPrecedent.TotalEncaissements);
            Assert.Equal(1, result.PerformancesMensuelles.MoisEnCours.NombreTransactions);
            Assert.Equal(1, result.PerformancesMensuelles.MoisPrecedent.NombreTransactions);
        }

        [Fact]
        public async Task PerformancesMensuelles_uses_DatePaiement_for_month_bucket()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_uses_DatePaiement_for_month_bucket));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            var (_, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 2000,
                MontantPaye = 2000,
                MontantAPayeDevisePrincipale = 2000,
                MontantPayeDevisePrincipale = 2000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                DateCreation = previousMonthStartUtc.AddDays(10),
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(7000m, result.PerformancesMensuelles.MoisEnCours.TotalEncaissements);
            Assert.Equal(0m, result.PerformancesMensuelles.MoisPrecedent.TotalEncaissements);
        }

        [Fact]
        public async Task PerformancesMensuelles_excludes_other_caissier()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_excludes_other_caissier));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            var (_, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = 99,
                MontantAPaye = 9000,
                MontantPaye = 9000,
                MontantAPayeDevisePrincipale = 9000,
                MontantPayeDevisePrincipale = 9000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                DateCreation = monthStartUtc.AddDays(1),
                DatePaiement = monthStartUtc.AddDays(1),
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(5000m, result.PerformancesMensuelles.MoisEnCours.TotalEncaissements);
            Assert.Equal(0m, result.PerformancesMensuelles.MoisPrecedent.TotalEncaissements);
        }

        [Fact]
        public async Task PerformancesMensuelles_recette_buckets_balance_with_total()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_recette_buckets_balance_with_total));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 1000,
                MontantPaye = 1000,
                MontantAPayeDevisePrincipale = 1000,
                MontantPayeDevisePrincipale = 1000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "COUPON_KANSA",
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var mois = (await svc.GetDashboardDataAsync()).PerformancesMensuelles.MoisEnCours;

            Assert.Equal(1000m, mois.RecetteAutre);
            Assert.Equal(6000m, mois.TotalEncaissements);
            Assert.Equal(
                mois.RecetteEspece + mois.RecetteMobileMoney + mois.RecetteVirement
                + mois.RecetteCarte + mois.RecetteAutre,
                mois.TotalEncaissements);
        }

        [Fact]
        public async Task PerformancesMensuelles_synthese_variation_matches_helper()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_synthese_variation_matches_helper));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            var (_, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var previousMonthStartUtc = monthStartUtc.AddMonths(-1);

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 2500,
                MontantPaye = 2500,
                MontantAPayeDevisePrincipale = 2500,
                MontantPayeDevisePrincipale = 2500,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                DateCreation = previousMonthStartUtc.AddDays(2),
                DatePaiement = previousMonthStartUtc.AddDays(2),
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var perf = (await svc.GetDashboardDataAsync()).PerformancesMensuelles;

            Assert.Equal(
                SocieteTransportMetricsHelper.ComputeVariationPercent(
                    perf.MoisEnCours.TotalEncaissements, perf.MoisPrecedent.TotalEncaissements),
                perf.Synthese.VariationEncaissementsPourcentage);
        }

        [Fact]
        public async Task PerformancesMensuelles_jours_ecoules_only_on_current_month()
        {
            await using var ctx = BuildDb(nameof(PerformancesMensuelles_jours_ecoules_only_on_current_month));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);
            await ctx.SaveChangesAsync();

            var (todayUtc, monthStartUtc, _) = CaissierTransportMetricsHelper.GetUtcBoundaries();
            var expectedJours = Math.Max(1, (todayUtc - monthStartUtc).Days + 1);

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var perf = (await svc.GetDashboardDataAsync()).PerformancesMensuelles;

            Assert.Equal(expectedJours, perf.MoisEnCours.JoursEcoules);
            Assert.NotNull(perf.MoisEnCours.MoyenneEncaissementsJournaliers);
            Assert.Null(perf.MoisPrecedent.JoursEcoules);
            Assert.Null(perf.MoisPrecedent.MoyenneEncaissementsJournaliers);
        }

        private static void SeedMinimal(CongoTravelDbContext ctx, int caissierId)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Rusa",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = caissierId,
                NomComplet = "Caissier Test",
                Email = "caissier@test.local",
                MotDePasseHash = "x",
                IdSociete = 1,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Destinations.Add(new Destination
            {
                IdDestination = 1,
                IdSociete = 1,
                VilleDepart = "Kin",
                VilleArrivee = "Goma",
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                IdSociete = 1,
                IdTypeVehicule = 1,
                AliasVehicule = "BUS",
                NombreSiege = 20,
                Statut = true
            });

            ctx.Voyages.Add(new Voyage
            {
                Id = 1,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                PrixDevisePrincipale = 5000,
                CodeDevisePrix = "CDF",
                Statut = true
            });

            ctx.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client",
                AdresseClient = "A",
                Statut = true,
                IsActif = true
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                IdSociete = 1,
                IdClient = 1,
                IdUtilisateur = caissierId,
                IdVoyage = 1,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 1,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 5000,
                MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000,
                MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                Statut = true,
                IsDeleted = false,
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow
            });
        }
    }
}
