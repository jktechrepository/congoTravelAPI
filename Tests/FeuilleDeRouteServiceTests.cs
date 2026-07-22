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
    public class FeuilleDeRouteServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static FeuilleDeRouteService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<FeuilleDeRouteService>.Instance);

        [Fact]
        public async Task GenererAsync_snapshots_societe_voyage_and_boarded_passengers()
        {
            await using var ctx = BuildDb(nameof(GenererAsync_snapshots_societe_voyage_and_boarded_passengers));
            var voyageId = await SeedVoyageWithEmbarquementAsync(ctx);
            var service = CreateService(ctx);

            var detail = await service.GenererAsync(voyageId, idUtilisateurGeneration: 42);

            Assert.True(detail.IdFeuilleDeRoute > 0);
            Assert.Equal(1, detail.IdSociete);
            Assert.Equal(voyageId, detail.IdVoyage);
            Assert.Equal("Congo Express", detail.SocieteNom);
            Assert.Equal("+243800", detail.SocieteTelephone);
            Assert.Equal("Kinshasa → Lubumbashi", detail.DestinationLibelle);
            Assert.Equal("ABC-123", detail.VehiculeImmatriculation);
            Assert.Equal(1, detail.NombrePassagers);
            Assert.Single(detail.Passagers);
            Assert.Equal("Jean Passager", detail.Passagers[0].NomComplet);
            Assert.Equal("A1", detail.Passagers[0].CodeSiege);
            Assert.Equal(42, detail.IdUtilisateurGeneration);

            var persisted = await ctx.FeuilleDeRoutes
                .Include(f => f.Passagers)
                .SingleAsync(f => f.IdFeuilleDeRoute == detail.IdFeuilleDeRoute);
            Assert.Equal(1, persisted.NombrePassagers);
            Assert.Single(persisted.Passagers);
        }

        [Fact]
        public async Task GenererAsync_allows_empty_passenger_list()
        {
            await using var ctx = BuildDb(nameof(GenererAsync_allows_empty_passenger_list));
            var voyageId = await SeedVoyageAsync(ctx);
            var service = CreateService(ctx);

            var detail = await service.GenererAsync(voyageId, null);

            Assert.Equal(0, detail.NombrePassagers);
            Assert.Empty(detail.Passagers);
        }

        [Fact]
        public async Task GenererAsync_throws_when_voyage_missing()
        {
            await using var ctx = BuildDb(nameof(GenererAsync_throws_when_voyage_missing));
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GenererAsync(999, null));
        }

        [Fact]
        public async Task GetBySocieteAsync_filters_by_voyage_and_date()
        {
            await using var ctx = BuildDb(nameof(GetBySocieteAsync_filters_by_voyage_and_date));
            var voyageId = await SeedVoyageWithEmbarquementAsync(ctx);
            var service = CreateService(ctx);

            var feuille1 = await service.GenererAsync(voyageId, null);
            await service.GenererAsync(voyageId, null);

            var otherVoyage = await SeedVoyageAsync(ctx, idVoyage: 2, dateDepart: DateTime.UtcNow.Date.AddDays(1));
            await service.GenererAsync(otherVoyage, null);

            var byVoyage = await service.GetBySocieteAsync(
                1, voyageId, null, new PagedRequest { PageNumber = 1, PageSize = 20 });
            Assert.Equal(2, byVoyage.TotalCount);
            Assert.All(byVoyage.Data, x => Assert.Equal(voyageId, x.IdVoyage));

            var byDate = await service.GetBySocieteAsync(
                1, null, feuille1.DateEmbarquement, new PagedRequest { PageNumber = 1, PageSize = 20 });
            Assert.Equal(2, byDate.TotalCount);
            Assert.All(byDate.Data, x => Assert.Equal(feuille1.DateEmbarquement.Date, x.DateEmbarquement.Date));
        }

        [Fact]
        public async Task GetByVoyageAsync_returns_history_newest_first()
        {
            await using var ctx = BuildDb(nameof(GetByVoyageAsync_returns_history_newest_first));
            var voyageId = await SeedVoyageAsync(ctx);
            var service = CreateService(ctx);

            var first = await service.GenererAsync(voyageId, null);
            await Task.Delay(5);
            var second = await service.GenererAsync(voyageId, null);

            var history = await service.GetByVoyageAsync(voyageId);

            Assert.Equal(2, history.Count);
            Assert.Equal(second.IdFeuilleDeRoute, history[0].IdFeuilleDeRoute);
            Assert.Equal(first.IdFeuilleDeRoute, history[1].IdFeuilleDeRoute);
        }

        [Fact]
        public async Task GetByIdAsync_returns_full_detail()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_full_detail));
            var voyageId = await SeedVoyageWithEmbarquementAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.GenererAsync(voyageId, 7);
            var detail = await service.GetByIdAsync(created.IdFeuilleDeRoute);

            Assert.NotNull(detail);
            Assert.Equal(created.IdFeuilleDeRoute, detail!.IdFeuilleDeRoute);
            Assert.Single(detail.Passagers);
            Assert.Equal("Jean Passager", detail.Passagers[0].NomComplet);
        }

        private static async Task<int> SeedVoyageAsync(
            CongoTravelDbContext ctx,
            int idVoyage = 1,
            DateTime? dateDepart = null)
        {
            if (!await ctx.Societes.AnyAsync(s => s.IdSociete == 1))
            {
                ctx.Societes.Add(new Societe
                {
                    IdSociete = 1,
                    Nom = "Congo Express",
                    Telephone = "+243800",
                    EmailContact = "contact@congo.cd",
                    AdresseResidence = "Av. 30 Juin",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            if (!await ctx.Destinations.AnyAsync(d => d.IdDestination == 1))
            {
                ctx.Destinations.Add(new Destination
                {
                    IdDestination = 1,
                    IdSociete = 1,
                    VilleDepart = "Kinshasa",
                    VilleArrivee = "Lubumbashi",
                    Montant = 50,
                    Statut = true
                });
            }

            if (!await ctx.Vehicules.AnyAsync(v => v.IdVehicule == 1))
            {
                ctx.Vehicules.Add(new Vehicule
                {
                    IdVehicule = 1,
                    AliasVehicule = "Bus Principal",
                    NumeroDePlaque = "ABC-123",
                    NombreSiege = 40,
                    IdSociete = 1,
                    IdTypeVehicule = 1,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            var depart = dateDepart ?? DateTime.UtcNow.Date;
            ctx.Voyages.Add(new Voyage
            {
                Id = idVoyage,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = depart,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 25000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 25000,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
            return idVoyage;
        }

        private static async Task<int> SeedVoyageWithEmbarquementAsync(CongoTravelDbContext ctx)
        {
            var voyageId = await SeedVoyageAsync(ctx);

            ctx.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Acheteur",
                Telephone = "+243700",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = 1,
                IdUtilisateur = 1,
                IdClient = 1,
                IdVoyage = voyageId,
                IdSociete = 1,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1,
                DateReservation = DateTime.UtcNow.Date,
                DateCreation = DateTime.UtcNow
            });

            ctx.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = 1,
                IdReservation = 1,
                IdSociete = 1,
                NomComplet = "Jean Passager",
                Telephone = "+243711",
                Email = "jean@test.cd",
                DocumentType = "CNI",
                DocumentNumero = "12345",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.Billets.Add(new Billet
            {
                IdBillet = 1,
                IdSociete = 1,
                IdReservation = 1,
                IdReservationPassenger = 1,
                IdClient = 1,
                QrCode = "QR-FDR-001",
                CodeSiege = "A1",
                IsUsed = true,
                DateGeneration = DateTime.UtcNow
            });

            ctx.BilletEmbarquements.Add(new BilletEmbarquement
            {
                IdEmbarquement = 1,
                IdSociete = 1,
                IdBillet = 1,
                IdReservationPassenger = 1,
                DateEmbarquementUtc = DateTime.UtcNow.AddMinutes(-10),
                IdUtilisateurEnregistrement = 5
            });

            await ctx.SaveChangesAsync();
            return voyageId;
        }
    }
}
