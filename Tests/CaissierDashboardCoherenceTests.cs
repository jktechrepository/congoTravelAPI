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
    /// <summary>Scénarios de l'audit cohérence GET /api/CaissierDashboard.</summary>
    public class CaissierDashboardCoherenceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockCaissier(int userId = 10) =>
            MockUser(UserRoles.CAISSIER, userId);

        private static Mock<ICurrentUserService> MockUser(string role, int userId)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.IsSuperAdmin).Returns(false);
            mock.SetupGet(x => x.SocieteId).Returns(1);
            mock.SetupGet(x => x.UserId).Returns(userId);
            return mock;
        }

        [Fact]
        public async Task Encaissement_uses_DatePaiement_not_DateCreation_for_daily_totals()
        {
            await using var ctx = BuildDb(nameof(Encaissement_uses_DatePaiement_not_DateCreation_for_daily_totals));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
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
                DateCreation = yesterday,
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(8000m, result.StatistiquesJournalieres.TotalRevenusTransport);
            Assert.Equal(2, result.StatistiquesJournalieres.NombreTransactions);
        }

        [Fact]
        public async Task Partial_payment_appears_in_en_cours_not_recents()
        {
            await using var ctx = BuildDb(nameof(Partial_payment_appears_in_en_cours_not_recents));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 2,
                IdSociete = 1,
                IdReservation = 1,
                IdUtilisateur = caissierId,
                MontantAPaye = 10000,
                MontantPaye = 2000,
                MontantPayeDevisePrincipale = 2000,
                CodeDevisePaiement = "CDF",
                Statut = false,
                IsDeleted = false,
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Single(result.PaiementsEnCours);
            Assert.Equal(2, result.PaiementsEnCours[0].IdPaiement);
            Assert.DoesNotContain(result.PaiementsRecents, p => p.IdPaiement == 2);
        }

        [Fact]
        public async Task Recettes_journalieres_expose_recetteAutre_and_balances_with_montantTotal()
        {
            await using var ctx = BuildDb(nameof(Recettes_journalieres_expose_recetteAutre_and_balances_with_montantTotal));
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
                MethodePaiement = "COUPON_KANSA",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                IsDeleted = false,
                DateCreation = DateTime.UtcNow,
                DatePaiement = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            var todayRecette = result.RecettesJournalieres.Last();
            Assert.Equal(1000m, todayRecette.RecetteAutre);
            Assert.Equal(6000m, todayRecette.MontantTotal);
            Assert.Equal(
                todayRecette.RecetteEspece + todayRecette.RecetteMobileMoney + todayRecette.RecetteVirement
                + todayRecette.RecetteCarte + todayRecette.RecetteAutre,
                todayRecette.MontantTotal);
        }

        [Fact]
        public void ComputeTauxRemplissage_uses_places_not_reservation_count()
        {
            var reservations = new List<Reservation>
            {
                new()
                {
                    IdReservation = 1,
                    IdVoyage = 1,
                    NombreDePlace = 4,
                    Voyage = new Voyage
                    {
                        Id = 1,
                        IdVehicule = 1,
                        Vehicule = new Vehicule { IdVehicule = 1, NombreSiege = 20 }
                    }
                },
                new()
                {
                    IdReservation = 2,
                    IdVoyage = 1,
                    NombreDePlace = 2,
                    Voyage = new Voyage
                    {
                        Id = 1,
                        IdVehicule = 1,
                        Vehicule = new Vehicule { IdVehicule = 1, NombreSiege = 20 }
                    }
                }
            };

            var taux = CaissierTransportMetricsHelper.ComputeTauxRemplissageCaissierJour(reservations);
            Assert.Equal(30m, taux);
        }

        [Fact]
        public async Task ReservationsConfirmeesJour_and_BilletsEmisJour_are_populated()
        {
            await using var ctx = BuildDb(nameof(ReservationsConfirmeesJour_and_BilletsEmisJour_are_populated));
            const int caissierId = 10;
            SeedMinimal(ctx, caissierId);

            ctx.Billets.Add(new Billet
            {
                IdBillet = 1,
                IdSociete = 1,
                IdReservation = 1,
                QrCode = "QR-1",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false
            });
            await ctx.SaveChangesAsync();

            var svc = new CaissierDashboardService(ctx, NullLogger<CaissierDashboardService>.Instance, MockCaissier(caissierId).Object);
            var result = await svc.GetDashboardDataAsync();

            Assert.Equal(1, result.StatistiquesJournalieres.ReservationsConfirmeesJour);
            Assert.Equal(1, result.StatistiquesJournalieres.BilletsEmisJour);
            Assert.Equal(1, result.ResumeCaisse.BilletsEmisJour);
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
