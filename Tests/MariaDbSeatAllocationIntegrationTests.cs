using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Tests d'intégration MariaDB — contraintes sièges et isolation Serializable.
    /// Exécutés uniquement si CONGOTRAVEL_TEST_MARIADB est défini (CI job integration-mariadb).
    /// </summary>
    [Trait("Category", "Integration")]
    public class MariaDbSeatAllocationIntegrationTests
    {
        private static string? MariaDbConnection =>
            Environment.GetEnvironmentVariable("CONGOTRAVEL_TEST_MARIADB");

        private static DbContextOptions<CongoTravelDbContext> MariaDbOptions() =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseMySql(
                    MariaDbConnection!,
                    ServerVersion.AutoDetect(MariaDbConnection!),
                    o => o.EnableRetryOnFailure(2))
                .Options;

        [Fact]
        public async Task SerializableAllocation_PreventsDoubleBooking_OnMariaDb()
        {
            if (string.IsNullOrWhiteSpace(MariaDbConnection))
            {
                return; // skip when env not set
            }

            var (voyageId, reservationId, p1, p2, catId) = await SeedScenarioAsync();

            var siegeMock = new Mock<ISiegeService>();
            siegeMock.Setup(s => s.EnsureSeatsForVehiculeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var alloc1 = CreateAllocationService(siegeMock.Object);
            var alloc2 = CreateAllocationService(siegeMock.Object);

            var requests = new List<(int IdReservationPassenger, int IdCategorieSiege)>
            {
                (p1, catId),
                (p2, catId)
            };

            await alloc1.AllocateSeatsForPassengersAsync(voyageId, reservationId, requests);

            await using var verify = new CongoTravelDbContext(MariaDbOptions());
            var count = await verify.VoyageSeatAllocations.CountAsync(a => a.IdVoyage == voyageId);
            Assert.Equal(2, count);

            // Second reservation should fail — only 2 seats total, both taken
            var reservation2 = new Reservation
            {
                IdVoyage = voyageId,
                IdClient = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                NombreDePlace = 1,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "EN_ATTENTE",
                Statut = true
            };
            verify.Reservations.Add(reservation2);
            await verify.SaveChangesAsync();

            var p3 = new ReservationPassenger
            {
                IdReservation = reservation2.IdReservation,
                IdSociete = 1,
                NomComplet = "Passager 3"
            };
            verify.ReservationPassengers.Add(p3);
            await verify.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                alloc2.AllocateSeatsForPassengersAsync(
                    voyageId,
                    reservation2.IdReservation,
                    new List<(int, int)> { (p3.IdReservationPassenger, catId) }));
        }

        private static VoyageSeatAllocationService CreateAllocationService(ISiegeService siegeService)
        {
            var ctx = new CongoTravelDbContext(MariaDbOptions());
            var dispo = new SiegeDisponibiliteService(
                ctx,
                siegeService,
                NullLogger<SiegeDisponibiliteService>.Instance);
            return new VoyageSeatAllocationService(
                ctx,
                siegeService,
                dispo,
                NullLogger<VoyageSeatAllocationService>.Instance);
        }

        private static async Task<(int voyageId, int reservationId, int p1, int p2, int catId)> SeedScenarioAsync()
        {
            await using var ctx = new CongoTravelDbContext(MariaDbOptions());

            var societe = new Societe { Nom = "MariaDbTest", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var cat = new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(cat);

            var type = new TypeVehicule { Libelle = "BUS", IdSociete = societe.IdSociete, Statut = true };
            ctx.TypeVehicules.Add(type);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "T1",
                Marques = "X",
                IdTypeVehicule = type.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "MD1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);

            var dest = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 100,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            for (var i = 1; i <= 2; i++)
            {
                ctx.Sieges.Add(new Siege
                {
                    IdVehicule = vehicule.IdVehicule,
                    IdCategorieSiege = cat.IdCategorieSiege,
                    CodeSiege = $"S{i}",
                    NumeroOrdre = i,
                    EstActif = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(1),
                HeureDepart = new TimeSpan(8, 0, 0),
                Prix = 100,
                IdVehicule = vehicule.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);

            var client = new Client
            {
                NomClient = "Test",
                Telephone = "000",
                DateCreation = DateTime.UtcNow,
                IsActif = true,
                Statut = true
            };
            ctx.Clients.Add(client);
            await ctx.SaveChangesAsync();

            var user = new Utilisateur
            {
                NomComplet = "Agent",
                Email = $"a-{Guid.NewGuid():N}@test.local",
                MotDePasseHash = "hash",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            var reservation = new Reservation
            {
                IdVoyage = voyage.Id,
                IdClient = client.IdClient,
                IdUtilisateur = user.IdUtilisateur,
                IdSociete = societe.IdSociete,
                NombreDePlace = 2,
                DateReservation = DateTime.UtcNow,
                StatutReservation = "EN_ATTENTE",
                Statut = true
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var p1 = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                IdSociete = societe.IdSociete,
                NomComplet = "P1"
            };
            var p2 = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                IdSociete = societe.IdSociete,
                NomComplet = "P2"
            };
            ctx.ReservationPassengers.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            return (voyage.Id, reservation.IdReservation, p1.IdReservationPassenger, p2.IdReservationPassenger, cat.IdCategorieSiege);
        }
    }
}
