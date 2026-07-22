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
    /// Phase E — attribution sièges : capacité, unicité, concurrence minimale (InMemory EF).
    /// </summary>
    public class WorkflowSeatAllocationTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string dbName) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

        /// <summary>Réinitialise le store InMemory (un par test).</summary>
        private static CongoTravelDbContext CreateFresh(string dbName)
        {
            var ctx = new CongoTravelDbContext(Options(dbName));
            ctx.Database.EnsureDeleted();
            ctx.Database.EnsureCreated();
            return ctx;
        }

        /// <summary>Ouvre le même store sans le détruire (concurrence / relectures).</summary>
        private static CongoTravelDbContext Open(string dbName) => new(Options(dbName));

        private static Mock<ISiegeService> CreateSiegeServiceMock()
        {
            var m = new Mock<ISiegeService>();
            m.Setup(s => s.EnsureSeatsForVehiculeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return m;
        }

        private static (int voyageId, int reservationId, int p1, int p2, int idCategorieSiege) SeedTwoSeatScenario(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "TestCo", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            ctx.SaveChanges();

            var eco = new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(eco);
            ctx.SaveChanges();

            var typeVehicule = new TypeVehicule { Libelle = "STD", IdSociete = societe.IdSociete, Statut = true };
            ctx.TypeVehicules.Add(typeVehicule);
            ctx.SaveChanges();

            var vehicule = new Vehicule
            {
                AliasVehicule = "T1",
                Marques = "X",
                IdTypeVehicule = typeVehicule.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "ABC",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);

            var destination = new Destination
            {
                VilleDepart = "A",
                VilleArrivee = "B",
                Montant = 100,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(destination);
            ctx.SaveChanges();

            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                IdVehicule = vehicule.IdVehicule,
                IdDestination = destination.IdDestination,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);

            var client = new Client
            {
                NomClient = "C1",
                AdresseClient = "Addr",
                Statut = true,
                DateCreation = DateTime.UtcNow,
                IsActif = true
            };
            ctx.Clients.Add(client);

            var utilisateur = new Utilisateur
            {
                NomComplet = "U1",
                Email = "u@test.local",
                MotDePasseHash = "x",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(utilisateur);
            ctx.SaveChanges();

            var reservation = new Reservation
            {
                IdClient = client.IdClient,
                IdUtilisateur = utilisateur.IdUtilisateur,
                IdVoyage = voyage.Id,
                IdSociete = societe.IdSociete,
                NombreDePlace = 2,
                StatutReservation = "EN_ATTENTE",
                Statut = true,
                DateReservation = DateTime.UtcNow.Date,
                DateCreation = DateTime.UtcNow
            };
            ctx.Reservations.Add(reservation);
            ctx.SaveChanges();

            var rp1 = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                NomComplet = "P1",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var rp2 = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                NomComplet = "P2",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.ReservationPassengers.AddRange(rp1, rp2);

            ctx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = voyage.Id,
                IdCategorieSiege = eco.IdCategorieSiege,
                Prix = 5000,
                IdSociete = societe.IdSociete,
                DateCreation = DateTime.UtcNow
            });

            ctx.Sieges.Add(new Siege
            {
                IdVehicule = vehicule.IdVehicule,
                NumeroOrdre = 1,
                CodeSiege = "T1/1",
                EstActif = true,
                IdSociete = societe.IdSociete,
                IdCategorieSiege = eco.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            });
            ctx.Sieges.Add(new Siege
            {
                IdVehicule = vehicule.IdVehicule,
                NumeroOrdre = 2,
                CodeSiege = "T1/2",
                EstActif = true,
                IdSociete = societe.IdSociete,
                IdCategorieSiege = eco.IdCategorieSiege,
                DateCreation = DateTime.UtcNow
            });

            ctx.SaveChanges();
            return (voyage.Id, reservation.IdReservation, rp1.IdReservationPassenger, rp2.IdReservationPassenger, eco.IdCategorieSiege);
        }

        [Fact]
        public async Task AllocateSeats_AssignsDistinctSeats_WhenCapacitySufficient()
        {
            var db = nameof(AllocateSeats_AssignsDistinctSeats_WhenCapacitySufficient);
            await using var ctx = CreateFresh(db);
            var (voyageId, resId, p1, p2, idCategorieSiege) = SeedTwoSeatScenario(ctx);

            var svc = new VoyageSeatAllocationService(ctx, CreateSiegeServiceMock().Object, SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);

            var result = await svc.AllocateSeatsForPassengersAsync(voyageId, resId, new[] { (p1, idCategorieSiege), (p2, idCategorieSiege) });

            Assert.Equal(2, result.Count);
            Assert.NotEqual(result[0].IdSiege, result[1].IdSiege);
            await using var verify = Open(db);
            Assert.Equal(2, await verify.VoyageSeatAllocations.CountAsync(a => a.IdVoyage == voyageId));
        }

        [Fact]
        public async Task AllocateSeats_Throws_WhenInsufficientFreeSeats()
        {
            var db = nameof(AllocateSeats_Throws_WhenInsufficientFreeSeats);
            await using var ctx = CreateFresh(db);
            var (voyageId, resId, p1, p2, idCategorieSiege) = SeedTwoSeatScenario(ctx);

            var vehicule = await ctx.Vehicules.FirstAsync();
            vehicule.NombreSiege = 1;
            var siege2 = await ctx.Sieges.FirstAsync(s => s.NumeroOrdre == 2);
            ctx.Sieges.Remove(siege2);
            await ctx.SaveChangesAsync();

            var svc = new VoyageSeatAllocationService(ctx, CreateSiegeServiceMock().Object, SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AllocateSeatsForPassengersAsync(voyageId, resId, new[] { (p1, idCategorieSiege), (p2, idCategorieSiege) }));
        }

        [Fact]
        public async Task AllocateSeats_Throws_WhenPassengerNotInReservation()
        {
            var db = nameof(AllocateSeats_Throws_WhenPassengerNotInReservation);
            await using var ctx = CreateFresh(db);
            var (voyageId, resId, p1, _, idCategorieSiege) = SeedTwoSeatScenario(ctx);

            var autreRes = new Reservation
            {
                IdClient = (await ctx.Clients.FirstAsync()).IdClient,
                IdUtilisateur = (await ctx.Utilisateurs.FirstAsync()).IdUtilisateur,
                IdVoyage = voyageId,
                IdSociete = (await ctx.Societes.FirstAsync()).IdSociete,
                NombreDePlace = 1,
                StatutReservation = "EN_ATTENTE",
                Statut = true,
                DateReservation = DateTime.UtcNow.Date,
                DateCreation = DateTime.UtcNow
            };
            ctx.Reservations.Add(autreRes);
            await ctx.SaveChangesAsync();

            var orphan = new ReservationPassenger
            {
                IdReservation = autreRes.IdReservation,
                NomComplet = "OrphelinTest",
                IdSociete = autreRes.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.ReservationPassengers.Add(orphan);
            await ctx.SaveChangesAsync();

            var svc = new VoyageSeatAllocationService(ctx, CreateSiegeServiceMock().Object, SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AllocateSeatsForPassengersAsync(voyageId, resId, new[] { (p1, idCategorieSiege), (orphan.IdReservationPassenger, idCategorieSiege) }));
        }

        [Fact]
        public async Task AllocateSeats_Throws_WhenRequestedCategoryHasNoSeat()
        {
            var db = nameof(AllocateSeats_Throws_WhenRequestedCategoryHasNoSeat);
            await using var ctx = CreateFresh(db);
            var (voyageId, resId, p1, _, _) = SeedTwoSeatScenario(ctx);

            var premium = new CategorieSiege
            {
                IdSociete = (await ctx.Societes.FirstAsync()).IdSociete,
                CodeCategorieSiege = "PREM",
                Libelle = "Premiere",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(premium);
            await ctx.SaveChangesAsync();

            var svc = new VoyageSeatAllocationService(ctx, CreateSiegeServiceMock().Object, SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.AllocateSeatsForPassengersAsync(voyageId, resId, new[] { (p1, premium.IdCategorieSiege) }));
            Assert.Contains("Aucun siège disponible", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Phase E2 — deux réservations sur un siège unique : une seule ligne d’allocation persistée (comportement relationnel).</summary>
        [Fact(Skip = "EF Core InMemory n’applique pas les index uniques comme MySQL/Pomelo ; le scénario peut persister 2 lignes. À valider en intégration sur SGBD réel.")]
        public async Task Concurrent_allocations_OnSingleSeat_AtMostOneRowPersisted()
        {
            var db = nameof(Concurrent_allocations_OnSingleSeat_AtMostOneRowPersisted);

            await using (var seedCtx = CreateFresh(db))
            {
                var societe = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
                seedCtx.Societes.Add(societe);
                seedCtx.SaveChanges();

                var ecoCat = new CategorieSiege
                {
                    IdSociete = societe.IdSociete,
                    CodeCategorieSiege = "ECO",
                    Libelle = "Eco",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                };
                seedCtx.CategorieSieges.Add(ecoCat);
                seedCtx.SaveChanges();

                var typeVehicule = new TypeVehicule { Libelle = "T", IdSociete = societe.IdSociete, Statut = true };
                seedCtx.TypeVehicules.Add(typeVehicule);
                seedCtx.SaveChanges();

                var vehicule = new Vehicule
                {
                    AliasVehicule = "S1",
                    Marques = "Y",
                    IdTypeVehicule = typeVehicule.IdTypeVehicule,
                    NombreSiege = 1,
                    IdSociete = societe.IdSociete,
                    NumeroDePlaque = "ZZ",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                };
                seedCtx.Vehicules.Add(vehicule);

                var destination = new Destination
                {
                    VilleDepart = "X",
                    VilleArrivee = "Y",
                    Montant = 50,
                    IdSociete = societe.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                };
                seedCtx.Destinations.Add(destination);
                seedCtx.SaveChanges();

                var voyage = new Voyage
                {
                    DateDepart = DateTime.UtcNow.Date,
                    HeureDepart = TimeSpan.Zero,
                    Prix = 100,
                    IdVehicule = vehicule.IdVehicule,
                    IdDestination = destination.IdDestination,
                    IdSociete = societe.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                };
                seedCtx.Voyages.Add(voyage);

                var client = new Client
                {
                    NomClient = "Cl",
                    AdresseClient = "A",
                    Statut = true,
                    DateCreation = DateTime.UtcNow,
                    IsActif = true
                };
                seedCtx.Clients.Add(client);
                var user = new Utilisateur
                {
                    NomComplet = "Us",
                    Email = "z@test",
                    MotDePasseHash = "h",
                    IdSociete = societe.IdSociete,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                };
                seedCtx.Utilisateurs.Add(user);
                seedCtx.SaveChanges();

                seedCtx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
                {
                    IdVoyage = voyage.Id,
                    IdCategorieSiege = ecoCat.IdCategorieSiege,
                    Prix = 100,
                    IdSociete = societe.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
                seedCtx.SaveChanges();

                void AddReservationWithPassenger()
                {
                    var r = new Reservation
                    {
                        IdClient = client.IdClient,
                        IdUtilisateur = user.IdUtilisateur,
                        IdVoyage = voyage.Id,
                        IdSociete = societe.IdSociete,
                        NombreDePlace = 1,
                        StatutReservation = "EN_ATTENTE",
                        Statut = true,
                        DateReservation = DateTime.UtcNow.Date,
                        DateCreation = DateTime.UtcNow
                    };
                    seedCtx.Reservations.Add(r);
                    seedCtx.SaveChanges();
                    seedCtx.ReservationPassengers.Add(new ReservationPassenger
                    {
                        IdReservation = r.IdReservation,
                        NomComplet = "Px",
                        IdSociete = societe.IdSociete,
                        Statut = true,
                        DateCreation = DateTime.UtcNow
                    });
                    seedCtx.SaveChanges();
                }

                AddReservationWithPassenger();
                AddReservationWithPassenger();

                seedCtx.Sieges.Add(new Siege
                {
                    IdVehicule = vehicule.IdVehicule,
                    NumeroOrdre = 1,
                    CodeSiege = "S1/1",
                    EstActif = true,
                    IdSociete = societe.IdSociete,
                    IdCategorieSiege = ecoCat.IdCategorieSiege,
                    DateCreation = DateTime.UtcNow
                });
                seedCtx.SaveChanges();
            }

            int voyageId;
            int rA, rB, pA, pB, catId;
            await using (var read = Open(db))
            {
                voyageId = await read.Voyages.Select(v => v.Id).FirstAsync();
                var ids = await read.Reservations.OrderBy(r => r.IdReservation).Select(r => r.IdReservation).ToListAsync();
                rA = ids[0];
                rB = ids[1];
                pA = await read.ReservationPassengers.Where(x => x.IdReservation == rA).Select(x => x.IdReservationPassenger).FirstAsync();
                pB = await read.ReservationPassengers.Where(x => x.IdReservation == rB).Select(x => x.IdReservationPassenger).FirstAsync();
                catId = await read.CategorieSieges.Select(c => c.IdCategorieSiege).FirstAsync();
            }

            var t1 = Task.Run(async () =>
            {
                await using var c = Open(db);
                var svc = new VoyageSeatAllocationService(c, CreateSiegeServiceMock().Object,
                    SiegeDisponibiliteTestHelper.Create(c),
                    NullLogger<VoyageSeatAllocationService>.Instance);
                try
                {
                    await svc.AllocateSeatsForPassengersAsync(voyageId, rA, new[] { (pA, catId) });
                }
                catch (InvalidOperationException) { }
                catch (DbUpdateException) { }
            });
            var t2 = Task.Run(async () =>
            {
                await using var c = Open(db);
                var svc = new VoyageSeatAllocationService(c, CreateSiegeServiceMock().Object,
                    SiegeDisponibiliteTestHelper.Create(c),
                    NullLogger<VoyageSeatAllocationService>.Instance);
                try
                {
                    await svc.AllocateSeatsForPassengersAsync(voyageId, rB, new[] { (pB, catId) });
                }
                catch (InvalidOperationException) { }
                catch (DbUpdateException) { }
            });

            await Task.WhenAll(t1, t2);

            await using var verify = Open(db);
            var allocations = await verify.VoyageSeatAllocations.CountAsync(a => a.IdVoyage == voyageId);
            Assert.Equal(1, allocations);
        }
    }
}
