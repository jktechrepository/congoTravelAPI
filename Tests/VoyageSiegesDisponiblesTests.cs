using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageSiegesDisponiblesTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Fact]
        public async Task GetSiegesDisponiblesResponse_groups_by_category_and_excludes_allocated()
        {
            var db = nameof(GetSiegesDisponiblesResponse_groups_by_category_and_excludes_allocated);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var eco = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Economique",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var vip = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, vip);
            var tv = new TypeVehicule { Libelle = "T", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "B1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 4,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "PLQ",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            var dest = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 100,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 1000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);
            await ctx.SaveChangesAsync();

            var s1 = new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 1, CodeSiege = "ECO/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow };
            var s2 = new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 2, CodeSiege = "ECO/2", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = eco.IdCategorieSiege, DateCreation = DateTime.UtcNow };
            var s3 = new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 3, CodeSiege = "VIP/1", EstActif = true, IdSociete = s.IdSociete, IdCategorieSiege = vip.IdCategorieSiege, DateCreation = DateTime.UtcNow };
            var s4 = new Siege { IdVehicule = vh.IdVehicule, NumeroOrdre = 4, CodeSiege = "VIP/2", EstActif = false, IdSociete = s.IdSociete, IdCategorieSiege = vip.IdCategorieSiege, DateCreation = DateTime.UtcNow };
            ctx.Sieges.AddRange(s1, s2, s3, s4);
            await ctx.SaveChangesAsync();

            var client = new Client { NomClient = "C", AdresseClient = "A", Statut = true, DateCreation = DateTime.UtcNow, IsActif = true };
            ctx.Clients.Add(client);
            var user = new Utilisateur { NomComplet = "U", Email = "u@t.local", MotDePasseHash = "x", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            var reservation = new Reservation
            {
                IdClient = client.IdClient,
                IdUtilisateur = user.IdUtilisateur,
                IdVoyage = voyage.Id,
                IdSociete = s.IdSociete,
                StatutReservation = "CONFIRMEE",
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var passenger = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                NomComplet = "P1",
                DateCreation = DateTime.UtcNow
            };
            ctx.ReservationPassengers.Add(passenger);
            await ctx.SaveChangesAsync();

            ctx.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = voyage.Id,
                IdSiege = s2.IdSiege,
                IdReservationPassenger = passenger.IdReservationPassenger,
                Statut = "CONFIRME",
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var tarifMock = new Moq.Mock<IVoyageTarifService>();
            var svc = new VoyageService(ctx, NullLogger<VoyageService>.Instance, tarifMock.Object, SiegeDisponibiliteTestHelper.Create(ctx));
            var response = await svc.GetSiegesDisponiblesResponsePourVoyageAsync(voyage.Id);

            Assert.Equal(voyage.Id, response.IdVoyage);
            Assert.Equal(2, response.NombreSiegesDisponibles);
            Assert.Equal(2, response.RepartitionCategorieSieges.Count);

            var ecoGroup = response.RepartitionCategorieSieges.Single(r => r.CodeCategorieSiege == "ECO");
            Assert.Equal(eco.IdCategorieSiege, ecoGroup.IdCategorieSiege);
            Assert.Equal("Economique", ecoGroup.Libelle);
            Assert.Equal(1, ecoGroup.NombreSiege);
            Assert.Single(ecoGroup.Sieges);
            Assert.Equal(s1.IdSiege, ecoGroup.Sieges[0].IdSiege);

            var vipGroup = response.RepartitionCategorieSieges.Single(r => r.CodeCategorieSiege == "VIP");
            Assert.Equal(1, vipGroup.NombreSiege);
            Assert.Equal(s3.IdSiege, vipGroup.Sieges[0].IdSiege);
        }

        [Fact]
        public async Task GetSiegesDisponiblesResponse_returns_empty_when_no_vehicle()
        {
            var db = nameof(GetSiegesDisponiblesResponse_returns_empty_when_no_vehicle);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var response = await new VoyageService(
                    ctx,
                    NullLogger<VoyageService>.Instance,
                    new Moq.Mock<IVoyageTarifService>().Object,
                    SiegeDisponibiliteTestHelper.Create(ctx))
                .GetSiegesDisponiblesResponsePourVoyageAsync(999);

            Assert.Equal(999, response.IdVoyage);
            Assert.Equal(0, response.NombreSiegesDisponibles);
            Assert.Empty(response.RepartitionCategorieSieges);
        }

        [Fact]
        public async Task GetRepartitionSiegesDisponiblesParVoyagesAsync_returns_summary_counts_per_category()
        {
            var db = nameof(GetRepartitionSiegesDisponiblesParVoyagesAsync_returns_summary_counts_per_category);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var bus = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "BUS",
                Libelle = "Business",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(bus);
            var tv = new TypeVehicule { Libelle = "T", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "B1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 3,
                IdSociete = s.IdSociete,
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
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var voy = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(9),
                Prix = 1000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            var sieges = Enumerable.Range(1, 3).Select(i => new Siege
            {
                IdVehicule = vh.IdVehicule,
                NumeroOrdre = i,
                CodeSiege = $"B1/{i}",
                EstActif = true,
                IdSociete = s.IdSociete,
                IdCategorieSiege = bus.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            }).ToList();
            ctx.Sieges.AddRange(sieges);
            await ctx.SaveChangesAsync();

            var svc = new VoyageService(
                ctx,
                NullLogger<VoyageService>.Instance,
                new Moq.Mock<IVoyageTarifService>().Object,
                SiegeDisponibiliteTestHelper.Create(ctx));

            var result = await svc.GetRepartitionSiegesDisponiblesParVoyagesAsync(new[] { voy.Id });

            Assert.True(result.TryGetValue(voy.Id, out var repartition));
            var group = Assert.Single(repartition);
            Assert.Equal(bus.IdCategorieSiege, group.IdCategorieSiege);
            Assert.Equal("BUS", group.CodeCategorieSiege);
            Assert.Equal("Business", group.Libelle);
            Assert.Equal(3, group.NombreSiege);
        }
    }
}
