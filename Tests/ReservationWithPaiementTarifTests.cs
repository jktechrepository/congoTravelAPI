using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Réservation + paiement : cohérence <see cref="PaiementDataDto.MontantAPaye"/> avec tarifs sièges après allocation.
    /// </summary>
    public class ReservationWithPaiementTarifTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private sealed class Seed
        {
            public int IdSociete { get; init; }
            public int IdClient { get; init; }
            public int IdUtilisateur { get; init; }
            public int IdVoyage { get; init; }
            public int IdCategorieSiege { get; init; }
        }

        private static async Task<Seed> SeedTwoPassengersOneVoyageAsync(CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "CoTarif", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var eco = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(eco);
            await ctx.SaveChangesAsync();

            var tv = new TypeVehicule { Libelle = "Std", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "VT1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "AB",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);
            var dest = new Destination
            {
                VilleDepart = "D1",
                VilleArrivee = "D2",
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
                HeureDepart = TimeSpan.FromHours(7),
                Prix = 50000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            ctx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = voy.Id,
                IdCategorieSiege = eco.IdCategorieSiege,
                Prix = 1000,
                IdSociete = s.IdSociete,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            ctx.Sieges.AddRange(
                new Siege
                {
                    IdVehicule = vh.IdVehicule,
                    NumeroOrdre = 1,
                    CodeSiege = "VT1/1",
                    EstActif = true,
                    IdSociete = s.IdSociete,
                    IdCategorieSiege = eco.IdCategorieSiege,
                    DateCreation = DateTime.UtcNow
                },
                new Siege
                {
                    IdVehicule = vh.IdVehicule,
                    NumeroOrdre = 2,
                    CodeSiege = "VT1/2",
                    EstActif = true,
                    IdSociete = s.IdSociete,
                    IdCategorieSiege = eco.IdCategorieSiege,
                    DateCreation = DateTime.UtcNow
                });

            var client = new Client
            {
                NomClient = "Acheteur",
                AdresseClient = "X",
                Statut = true,
                DateCreation = DateTime.UtcNow,
                IsActif = true
            };
            ctx.Clients.Add(client);
            var user = new Utilisateur
            {
                NomComplet = "U",
                Email = "u@tarif.test",
                MotDePasseHash = "h",
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            return new Seed
            {
                IdSociete = s.IdSociete,
                IdClient = client.IdClient,
                IdUtilisateur = user.IdUtilisateur,
                IdVoyage = voy.Id,
                IdCategorieSiege = eco.IdCategorieSiege
            };
        }

        private static ReservationWithPaiementService CreateSut(
            CongoTravelDbContext ctx,
            Services.Repositories.ICurrentUserService? currentUser = null)
        {
            currentUser ??= CurrentUserTestHelper.MockCaissier();
            var mockRes = new Mock<IReservationRepository>();
            var mockPayRepo = new Mock<IPaiementRepository>();
            var mockBilletRepo = new Mock<IBilletRepository>();
            var mockQr = new Mock<IQrCodeService>();

            var billetEmission = new BilletEmissionService(
                mockBilletRepo.Object,
                mockQr.Object,
                ctx,
                ConfigSocieteTestHelper.Create(ctx),
                NullLogger<BilletEmissionService>.Instance);

            var siegeService = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            var seatAlloc = new VoyageSeatAllocationService(
                ctx,
                siegeService,
                SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);
            var voyageTarif = new VoyageTarifService(ctx);
            var billetPricing = new BilletPricingEnrichmentService(ctx, voyageTarif);

            return new ReservationWithPaiementService(
                ctx,
                NullLogger<ReservationWithPaiementService>.Instance,
                mockRes.Object,
                mockPayRepo.Object,
                billetEmission,
                seatAlloc,
                voyageTarif,
                billetPricing,
                ConfigSocieteTestHelper.Create(ctx),
                currentUser);
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_sets_origine_from_current_user_role()
        {
            var db = nameof(CreateReservationWithPaiementAsync_sets_origine_from_current_user_role);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    NombreDePlace = 1,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    IdSociete = seed.IdSociete,
                    IdUtilisateur = seed.IdUtilisateur,
                    MontantAPaye = 1000m,
                    MontantPaye = 1000m,
                    MethodePaiement = "ESPECES"
                }
            };

            var result = await CreateSut(ctx, CurrentUserTestHelper.MockClient()).CreateReservationWithPaiementAsync(dto);

            Assert.Equal(Models.Enums.OrigineOperation.CLIENT, result.Reservation.Origine);
            Assert.Equal(Models.Enums.OrigineOperation.CLIENT, result.Paiement.Origine);
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_succeeds_when_montant_matches_tarif_total_partial_payment()
        {
            var db = nameof(CreateReservationWithPaiementAsync_succeeds_when_montant_matches_tarif_total_partial_payment);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 2000m,
                    MontantPaye = 500m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Succes, result.Statut);
            Assert.NotNull(result.Reservation);
            Assert.Equal(1, await ctx.Reservations.CountAsync());
            Assert.Equal(1, await ctx.Paiements.CountAsync());
            Assert.Equal(2, await ctx.VoyageSeatAllocations.CountAsync(a => a.Statut == "CONFIRME"));
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_fails_when_montant_mismatch_after_allocation()
        {
            var db = nameof(CreateReservationWithPaiementAsync_fails_when_montant_mismatch_after_allocation);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 1500m,
                    MontantPaye = 1500m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Echec, result.Statut);
            Assert.Contains("Montant à payer incohérent", result.Message, StringComparison.Ordinal);
            Assert.Contains("2000", result.Message, StringComparison.Ordinal);
            Assert.Equal(0, await ctx.Paiements.CountAsync());
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_accepts_montant_within_tolerance()
        {
            var db = nameof(CreateReservationWithPaiementAsync_accepts_montant_within_tolerance);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 2000.04m,
                    MontantPaye = 1m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Succes, result.Statut);
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_fails_when_montant_outside_tolerance()
        {
            var db = nameof(CreateReservationWithPaiementAsync_fails_when_montant_outside_tolerance);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 2000.10m,
                    MontantPaye = 1m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Echec, result.Statut);
            Assert.Contains("Montant à payer incohérent", result.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task CreateReservationWithPaiementAsync_fails_when_passenger_category_is_invalid()
        {
            var db = nameof(CreateReservationWithPaiementAsync_fails_when_passenger_category_is_invalid);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege + 999 }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 2000m,
                    MontantPaye = 2000m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Echec, result.Statut);
            Assert.Contains("catégories de siège", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Cash_reservation_unaffected_by_electronic_supplement_config()
        {
            var db = nameof(Cash_reservation_unaffected_by_electronic_supplement_config);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPassengersOneVoyageAsync(ctx);

            await ConfigSocieteTestHelper.SeedAsync(ctx, seed.IdSociete, c =>
            {
                c.MontAddPaieElectronique = 500m;
                c.CodeDeviseMontAddPaieElectronique = "CDF";
            });

            var mockRes = new Mock<IReservationRepository>();
            mockRes
                .Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
                .ReturnsAsync((Reservation r) =>
                {
                    r.IdReservation = 99;
                    ctx.Reservations.Add(r);
                    ctx.SaveChanges();
                    return r;
                });

            var mockPayRepo = new Mock<IPaiementRepository>();
            mockPayRepo
                .Setup(p => p.CreateAsync(It.IsAny<Paiement>()))
                .ReturnsAsync((Paiement p) =>
                {
                    p.IdPaiement = 1;
                    ctx.Paiements.Add(p);
                    ctx.SaveChanges();
                    return p;
                });

            var mockBilletRepo = new Mock<IBilletRepository>();
            var mockQr = new Mock<IQrCodeService>();
            mockQr.Setup(q => q.GenerateUniqueQrCodeAsync(It.IsAny<int>(), It.IsAny<int?>())).ReturnsAsync("QR-CASH");

            var billetEmission = new BilletEmissionService(
                mockBilletRepo.Object,
                mockQr.Object,
                ctx,
                ConfigSocieteTestHelper.Create(ctx),
                NullLogger<BilletEmissionService>.Instance);

            var siegeService = new SiegeService(ctx, NullLogger<SiegeService>.Instance);
            var seatAlloc = new VoyageSeatAllocationService(
                ctx,
                siegeService,
                SiegeDisponibiliteTestHelper.Create(ctx),
                NullLogger<VoyageSeatAllocationService>.Instance);
            var voyageTarif = new VoyageTarifService(ctx);
            var billetPricing = new BilletPricingEnrichmentService(ctx, voyageTarif);

            var sut = new ReservationWithPaiementService(
                ctx,
                NullLogger<ReservationWithPaiementService>.Instance,
                mockRes.Object,
                mockPayRepo.Object,
                billetEmission,
                seatAlloc,
                voyageTarif,
                billetPricing,
                ConfigSocieteTestHelper.Create(ctx),
                CurrentUserTestHelper.MockCaissier());

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new() { NomComplet = "P1", IdCategorieSiege = seed.IdCategorieSiege },
                        new() { NomComplet = "P2", IdCategorieSiege = seed.IdCategorieSiege }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = 2000m,
                    MontantPaye = 2000m,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Succes, result.Statut);
        }
    }
}
