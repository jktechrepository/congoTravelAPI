using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class CaissierDashboardNombrePassagersTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(db)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        [Fact]
        public void CountPassagersFromPaidReservations_multi_passenger_single_reservation_counts_two()
        {
            var reservationId = 100;
            var passengers = new List<ReservationPassenger>
            {
                new() { IdReservation = reservationId, NomComplet = "Alice", Statut = true },
                new() { IdReservation = reservationId, NomComplet = "Bob", Statut = true }
            };

            var count = CaissierDashboardMetrics.CountPassagersFromPaidReservations(
                new[] { reservationId },
                passengers,
                Array.Empty<Reservation>());

            Assert.Equal(2, count);
        }

        [Fact]
        public void CountPassagersFromPaidReservations_distinct_clients_old_logic_would_be_one_new_is_two()
        {
            var res1 = 1;
            var res2 = 2;
            var passengers = new List<ReservationPassenger>
            {
                new() { IdReservation = res1, NomComplet = "P1", Statut = true },
                new() { IdReservation = res2, NomComplet = "P2", Statut = true },
                new() { IdReservation = res2, NomComplet = "P3", Statut = true }
            };

            var count = CaissierDashboardMetrics.CountPassagersFromPaidReservations(
                new[] { res1, res2 },
                passengers,
                Array.Empty<Reservation>());

            Assert.Equal(3, count);
        }

        [Fact]
        public void CountPassagersFromPaidReservations_legacy_reservation_uses_nombre_de_place()
        {
            var reservationId = 50;
            var legacy = new Reservation
            {
                IdReservation = reservationId,
                NombreDePlace = 3
            };

            var count = CaissierDashboardMetrics.CountPassagersFromPaidReservations(
                new[] { reservationId },
                Array.Empty<ReservationPassenger>(),
                new[] { legacy });

            Assert.Equal(3, count);
        }

        [Fact]
        public void CountPassagersFromPaidReservations_inactive_passengers_excluded()
        {
            var reservationId = 10;
            var passengers = new List<ReservationPassenger>
            {
                new() { IdReservation = reservationId, NomComplet = "Actif", Statut = true },
                new() { IdReservation = reservationId, NomComplet = "Inactif", Statut = false }
            };

            var count = CaissierDashboardMetrics.CountPassagersFromPaidReservations(
                new[] { reservationId },
                passengers,
                Array.Empty<Reservation>());

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetStatistiquesJournalieresAsync_counts_passengers_not_distinct_clients()
        {
            var db = nameof(GetStatistiquesJournalieresAsync_counts_passengers_not_distinct_clients);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var societe = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var caissier = new Utilisateur
            {
                NomComplet = "Caisse",
                Email = "c@test.local",
                MotDePasseHash = "x",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(caissier);

            var client = new Client
            {
                NomClient = "Acheteur unique",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Clients.Add(client);

            var tv = new TypeVehicule { Libelle = "T", IdSociete = societe.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "B1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 4,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "X",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            var dest = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 1,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(9),
                Prix = 10000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);
            await ctx.SaveChangesAsync();

            var reservation = new Reservation
            {
                IdVoyage = voyage.Id,
                IdClient = client.IdClient,
                IdUtilisateur = caissier.IdUtilisateur,
                IdSociete = societe.IdSociete,
                NombreDePlace = 2,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();

            ctx.ReservationPassengers.AddRange(
                new ReservationPassenger
                {
                    IdReservation = reservation.IdReservation,
                    IdSociete = societe.IdSociete,
                    NomComplet = "Passager 1",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new ReservationPassenger
                {
                    IdReservation = reservation.IdReservation,
                    IdSociete = societe.IdSociete,
                    NomComplet = "Passager 2",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            ctx.Paiements.Add(new Paiement
            {
                MontantAPaye = 2000,
                MontantPaye = 2000,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                MethodePaiement = "ESPECES",
                Statut = true,
                IdUtilisateur = caissier.IdUtilisateur,
                IdReservation = reservation.IdReservation,
                IdSociete = societe.IdSociete,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(x => x.UserId).Returns(caissier.IdUtilisateur);
            currentUser.SetupGet(x => x.SocieteId).Returns(societe.IdSociete);
            currentUser.Setup(x => x.GetSocieteId()).Returns(societe.IdSociete);

            var sut = new CaissierDashboardService(
                ctx,
                NullLogger<CaissierDashboardService>.Instance,
                currentUser.Object);

            var dashboard = await sut.GetDashboardDataAsync();
            var stats = dashboard.StatistiquesJournalieres;

            Assert.Equal(2, stats.NombrePassagers);
            Assert.Equal(1, stats.NombreTransactions);
            Assert.NotEqual(1, stats.NombrePassagers);
        }
    }
}
