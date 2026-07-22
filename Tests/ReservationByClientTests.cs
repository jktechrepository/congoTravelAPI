using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ReservationByClientTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static IMapper CreateMapper() =>
            new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<VehiculeMappingProfile>();
                    cfg.AddProfile<WorkflowReservationMappingProfile>();
                },
                NullLoggerFactory.Instance).CreateMapper();

        [Fact]
        public async Task GetByClientAsync_includes_client_and_passagers()
        {
            var db = nameof(GetByClientAsync_includes_client_and_passagers);
            await using var ctx = BuildDb(db);

            ctx.Societes.Add(new Societe { IdSociete = 1, Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow });
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
                AliasVehicule = "BUS",
                NombreSiege = 20,
                IdSociete = 1,
                IdTypeVehicule = 1,
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
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Clients.Add(new Client
            {
                IdClient = 3,
                NomClient = "Client Test",
                Telephone = "+243700",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 6,
                NomComplet = "Agent",
                Email = "agent@test.com",
                MotDePasseHash = "hash",
                DateCreation = DateTime.UtcNow
            });
            var reservation = new Reservation
            {
                IdClient = 3,
                IdUtilisateur = 6,
                IdVoyage = 1,
                IdSociete = 1,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow,
                NombreDePlace = 1
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();
            ctx.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                IdClient = 3,
                NomComplet = "Passager A",
                IdSociete = 1,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = new ReservationService(
                ctx,
                Mock.Of<IConfigSocieteRepository>(),
                NullLogger<ReservationService>.Instance);

            var list = (await svc.GetByClientAsync(3)).ToList();

            Assert.Single(list);
            Assert.NotNull(list[0].Client);
            Assert.Equal("Client Test", list[0].Client!.NomClient);
            Assert.NotNull(list[0].Passagers);
            Assert.Single(list[0].Passagers!);
            Assert.Equal("Passager A", list[0].Passagers!.First().NomComplet);
        }

        [Fact]
        public async Task GetByClient_returns_passagers_in_response_dto()
        {
            var db = nameof(GetByClient_returns_passagers_in_response_dto);
            await using var ctx = BuildDb(db);

            ctx.Societes.Add(new Societe { IdSociete = 1, Nom = "Soc", Statut = true, DateCreation = DateTime.UtcNow });
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
                AliasVehicule = "BUS",
                NombreSiege = 20,
                IdSociete = 1,
                IdTypeVehicule = 1,
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
                Prix = 100,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Clients.Add(new Client
            {
                IdClient = 3,
                NomClient = "Client Test",
                Telephone = "+243700",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            ctx.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 6,
                NomComplet = "Agent",
                Email = "agent@test.com",
                MotDePasseHash = "hash",
                DateCreation = DateTime.UtcNow
            });
            var reservation = new Reservation
            {
                IdClient = 3,
                IdUtilisateur = 6,
                IdVoyage = 1,
                IdSociete = 1,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow,
                NombreDePlace = 1
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();
            ctx.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                NomComplet = "Passager A",
                IdSociete = 1,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var mapper = CreateMapper();
            var controller = new ReservationController(
                new ReservationService(ctx, Mock.Of<IConfigSocieteRepository>(), NullLogger<ReservationService>.Instance),
                Mock.Of<IBilletRepository>(),
                mapper,
                NullLogger<ReservationController>.Instance,
                Mock.Of<ICashReservationWithPaiementService>(),
                Mock.Of<IFlexPayReservationService>(),
                Mock.Of<IReservationWithPaiementReadService>(),
                Mock.Of<IBilletPricingEnrichmentService>(),
                ctx,
                Mock.Of<ICurrentUserService>());

            var result = await controller.GetByClient(3);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<Models.DTOs.ReservationResponseDto>>(ok.Value).ToList();

            Assert.Single(dtos);
            Assert.Equal("Client Test", dtos[0].NomClient);
            Assert.NotNull(dtos[0].Passagers);
            Assert.Single(dtos[0].Passagers!);
            Assert.Equal("Passager A", dtos[0].Passagers![0].NomComplet);
        }
    }
}
