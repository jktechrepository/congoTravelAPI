using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class DashboardServiceTransportTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task GetDashboardDataAsync_includes_transport_metrics_and_collecte()
        {
            await using var ctx = BuildDb(nameof(GetDashboardDataAsync_includes_transport_metrics_and_collecte));
            SeedData(ctx);
            await ctx.SaveChangesAsync();

            var svc = DashboardEnrichmentTestHelper.CreateTransportDashboardService(ctx);
            var result = await svc.GetDashboardDataAsync(1);

            Assert.Equal("CDF", result.CodeDevisePrincipale);
            Assert.Equal(1, result.TotalAgents);
            Assert.Equal(1, result.TotalClientsActifs);
            Assert.Equal(1, result.TransportStatistiques.VoyagesActifs);
            Assert.Equal(1, result.TransportStatistiques.VoyagesAujourdhui);
            Assert.Equal(1, result.TransportStatistiques.VoyagesMois);
            Assert.Equal(1, result.TransportStatistiques.ReservationsConfirmeesMois);
            Assert.Equal(1, result.TransportStatistiques.ReservationsConfirmeesJour);
            Assert.Equal(1, result.TransportStatistiques.BilletsEmisMois);
            Assert.True(result.CollecteMois.Montant > 0);
            Assert.Equal(1, result.CollecteMois.NombrePaiements);
            Assert.Equal(3, result.CollecteParOrigineGroupe.Count);
            var agentGroup = result.CollecteParOrigineGroupe.Single(x => x.OrigineGroupe == Models.Enums.OrigineOperationGroupe.AGENT);
            Assert.Equal(5000m, agentGroup.Montant);
            Assert.Equal(1, agentGroup.NombrePaiements);
            Assert.Equal(100m, agentGroup.VariationPourcentage);
            Assert.Equal(100m, result.CollecteOrigineGroupeSynthese.PartGuichetPourcentage);
            Assert.Equal(0m, result.CollecteOrigineGroupeSynthese.PartDigitalPourcentage);
            Assert.Single(result.Top5AgentsCollecteurs);
        }

        private static void SeedData(CongoTravelDbContext ctx)
        {
            ctx.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Agents.Add(new Agent
            {
                IdAgent = 1,
                IdSociete = 1,
                NomComplet = "Agent A",
                Matricule = "AG-001",
                DateNaissance = new DateTime(1990, 1, 1),
                Statut = true
            });

            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 10,
                IdSociete = 1,
                IdAgent = 1,
                NomComplet = "User Agent A",
                MotDePasseHash = "hash",
                Statut = true
            });

            ctx.Clients.Add(new Client
            {
                IdClient = 100,
                NomClient = "Client A",
                AdresseClient = "Kin",
                Statut = true,
                IsActif = true,
                IsDeleted = false
            });

            ctx.Destinations.Add(new Destination
            {
                IdDestination = 1,
                IdSociete = 1,
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Statut = true
            });

            ctx.Vehicules.Add(new Vehicule
            {
                IdVehicule = 1,
                AliasVehicule = "BUS-1",
                NombreSiege = 20,
                IdSociete = 1,
                IdTypeVehicule = 1,
                Statut = true
            });

            ctx.Voyages.Add(new Voyage
            {
                Id = 1000,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                PrixDevisePrincipale = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                Statut = true
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 500,
                IdSociete = 1,
                IdClient = 100,
                IdUtilisateur = 10,
                IdVoyage = 1000,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                NombreDePlace = 1
            });

            ctx.Billets.Add(new Billet
            {
                IdBillet = 1,
                IdSociete = 1,
                IdReservation = 500,
                QrCode = "QR-1",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false
            });

            ctx.Paiements.Add(new Paiement
            {
                IdPaiement = 900,
                IdSociete = 1,
                IdReservation = 500,
                IdUtilisateur = 10,
                MontantAPaye = 5000,
                MontantPaye = 5000,
                MontantAPayeDevisePrincipale = 5000,
                MontantPayeDevisePrincipale = 5000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                DatePaiement = DateTime.UtcNow,
                Statut = true,
                IsDeleted = false,
                Origine = Models.Enums.OrigineOperation.CAISSIER
            });
        }
    }
}
