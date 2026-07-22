using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Prix affiché sur chaque billet d'une réservation multi-passagers (tarif par catégorie de siège).
    /// </summary>
    public class MultiPassengerBilletPrixVoyageTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(db)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        private static Models.DTOs.BilletResponseDto ToResponseDto(Billet billet) =>
            new()
            {
                IdBillet = billet.IdBillet,
                IdReservation = billet.IdReservation,
                IdReservationPassenger = billet.IdReservationPassenger,
                IdSiege = billet.IdSiege,
                CodeSiege = billet.CodeSiege,
                NomPassager = billet.ReservationPassenger?.NomComplet,
                QrCode = billet.QrCode,
                PrixVoyage = BilletResponseDtoPricing.ResolvePrixVoyage(billet)
            };

        private sealed class MultiPassengerSeed
        {
            public int IdSociete { get; init; }
            public int IdClient { get; init; }
            public int IdUtilisateur { get; init; }
            public int IdVoyage { get; init; }
            public int IdCategorieEco { get; init; }
            public int IdCategorieVip { get; init; }
            public int PrixEco { get; init; } = 1000;
            public int PrixVip { get; init; } = 15000;
            public int PrixVoyageGlobal { get; init; } = 50000;
        }

        /// <summary>2 passagers, 2 catégories (ECO + VIP), tarifs distincts sur le voyage.</summary>
        private static async Task<MultiPassengerSeed> SeedEcoAndVipTwoSeatsAsync(CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "MultiPax", DateCreation = DateTime.UtcNow };
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
            var vip = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.AddRange(eco, vip);
            await ctx.SaveChangesAsync();

            var tv = new TypeVehicule
            {
                Libelle = "Std",
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "BUS1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 2,
                IdSociete = s.IdSociete,
                NumeroDePlaque = "XY",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vh);

            var dest = new Destination
            {
                VilleDepart = "Kin",
                VilleArrivee = "Lub",
                Montant = 1,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            const int prixGlobal = 50000;
            const int prixEco = 1000;
            const int prixVip = 15000;

            var voy = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(2),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = prixGlobal,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            ctx.VoyageTarifsCategorieSiege.AddRange(
                new VoyageTarifCategorieSiege
                {
                    IdVoyage = voy.Id,
                    IdCategorieSiege = eco.IdCategorieSiege,
                    Prix = prixEco,
                    IdSociete = s.IdSociete,
                    DateCreation = DateTime.UtcNow
                },
                new VoyageTarifCategorieSiege
                {
                    IdVoyage = voy.Id,
                    IdCategorieSiege = vip.IdCategorieSiege,
                    Prix = prixVip,
                    IdSociete = s.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            ctx.Sieges.AddRange(
                new Siege
                {
                    IdVehicule = vh.IdVehicule,
                    NumeroOrdre = 1,
                    CodeSiege = "BUS1/1",
                    EstActif = true,
                    IdSociete = s.IdSociete,
                    IdCategorieSiege = eco.IdCategorieSiege,
                    DateCreation = DateTime.UtcNow
                },
                new Siege
                {
                    IdVehicule = vh.IdVehicule,
                    NumeroOrdre = 2,
                    CodeSiege = "BUS1/2",
                    EstActif = true,
                    IdSociete = s.IdSociete,
                    IdCategorieSiege = vip.IdCategorieSiege,
                    DateCreation = DateTime.UtcNow
                });

            var client = new Client
            {
                NomClient = "Famille Test",
                AdresseClient = "X",
                Statut = true,
                DateCreation = DateTime.UtcNow,
                IsActif = true
            };
            ctx.Clients.Add(client);
            var user = new Utilisateur
            {
                NomComplet = "Caissier",
                Email = "caissier@multipax.test",
                MotDePasseHash = "h",
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            return new MultiPassengerSeed
            {
                IdSociete = s.IdSociete,
                IdClient = client.IdClient,
                IdUtilisateur = user.IdUtilisateur,
                IdVoyage = voy.Id,
                IdCategorieEco = eco.IdCategorieSiege,
                IdCategorieVip = vip.IdCategorieSiege,
                PrixEco = prixEco,
                PrixVip = prixVip,
                PrixVoyageGlobal = prixGlobal
            };
        }

        private static async Task<List<Billet>> LoadBilletsLikeApiGetAsync(CongoTravelDbContext ctx, int idReservation)
        {
            return await ctx.Billets
                .Include(b => b.ReservationPassenger)
                .Include(b => b.Siege)
                .Include(b => b.Reservation)
                    .ThenInclude(r => r!.Voyage)
                        .ThenInclude(v => v!.VoyageTarifsCategorieSiege)
                .Where(b => b.IdReservation == idReservation)
                .OrderBy(b => b.IdReservationPassenger)
                .ToListAsync();
        }

        [Fact]
        public void ResolvePrixVoyage_multi_passenger_each_billet_gets_tarif_for_its_seat_category()
        {
            var voyage = new Voyage
            {
                Id = 10,
                Prix = 50000,
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 1, Prix = 1000 },
                    new() { IdCategorieSiege = 2, Prix = 15000 }
                }
            };
            var reservation = new Reservation { Voyage = voyage };

            var billetEco = new Billet
            {
                Reservation = reservation,
                Siege = new Siege { IdCategorieSiege = 1 },
                ReservationPassenger = new ReservationPassenger { NomComplet = "Passager Eco" }
            };
            var billetVip = new Billet
            {
                Reservation = reservation,
                Siege = new Siege { IdCategorieSiege = 2 },
                ReservationPassenger = new ReservationPassenger { NomComplet = "Passager VIP" }
            };

            Assert.Equal(1000, BilletResponseDtoPricing.ResolvePrixVoyage(billetEco));
            Assert.Equal(15000, BilletResponseDtoPricing.ResolvePrixVoyage(billetVip));
        }

        [Fact]
        public void ResolvePrixVoyage_without_siege_navigation_falls_back_to_voyage_global_prix()
        {
            var voyage = new Voyage
            {
                Id = 10,
                Prix = 50000,
                VoyageTarifsCategorieSiege = new List<VoyageTarifCategorieSiege>
                {
                    new() { IdCategorieSiege = 1, Prix = 1000 }
                }
            };

            var billet = new Billet
            {
                IdSiege = 99,
                Siege = null,
                Reservation = new Reservation { Voyage = voyage }
            };

            Assert.Equal(50000, BilletResponseDtoPricing.ResolvePrixVoyage(billet));
        }

        [Fact]
        public async Task ApiGetBillet_mapping_shows_per_passenger_tarif_when_graph_is_loaded()
        {
            var db = nameof(ApiGetBillet_mapping_shows_per_passenger_tarif_when_graph_is_loaded);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedEcoAndVipTwoSeatsAsync(ctx);

            var reservation = new Reservation
            {
                IdVoyage = seed.IdVoyage,
                IdClient = seed.IdClient,
                IdUtilisateur = seed.IdUtilisateur,
                IdSociete = seed.IdSociete,
                NombreDePlace = 2,
                StatutReservation = "CONFIRMEE",
                Statut = true,
                DateReservation = DateTime.UtcNow,
                DateCreation = DateTime.UtcNow
            };
            ctx.Reservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var pEco = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                IdSociete = seed.IdSociete,
                NomComplet = "Alice Eco",
                DateCreation = DateTime.UtcNow
            };
            var pVip = new ReservationPassenger
            {
                IdReservation = reservation.IdReservation,
                IdSociete = seed.IdSociete,
                NomComplet = "Bob VIP",
                DateCreation = DateTime.UtcNow
            };
            ctx.ReservationPassengers.AddRange(pEco, pVip);
            await ctx.SaveChangesAsync();

            var siegeEco = await ctx.Sieges.FirstAsync(s => s.IdCategorieSiege == seed.IdCategorieEco);
            var siegeVip = await ctx.Sieges.FirstAsync(s => s.IdCategorieSiege == seed.IdCategorieVip);

            ctx.VoyageSeatAllocations.AddRange(
                new VoyageSeatAllocation
                {
                    IdVoyage = seed.IdVoyage,
                    IdReservationPassenger = pEco.IdReservationPassenger,
                    IdSiege = siegeEco.IdSiege,
                    Statut = "CONFIRME",
                    DateCreation = DateTime.UtcNow
                },
                new VoyageSeatAllocation
                {
                    IdVoyage = seed.IdVoyage,
                    IdReservationPassenger = pVip.IdReservationPassenger,
                    IdSiege = siegeVip.IdSiege,
                    Statut = "CONFIRME",
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            ctx.Billets.AddRange(
                new Billet
                {
                    IdReservation = reservation.IdReservation,
                    IdReservationPassenger = pEco.IdReservationPassenger,
                    IdSiege = siegeEco.IdSiege,
                    CodeSiege = siegeEco.CodeSiege,
                    QrCode = "QR-ECO-1",
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = seed.IdSociete,
                    DateCreation = DateTime.UtcNow
                },
                new Billet
                {
                    IdReservation = reservation.IdReservation,
                    IdReservationPassenger = pVip.IdReservationPassenger,
                    IdSiege = siegeVip.IdSiege,
                    CodeSiege = siegeVip.CodeSiege,
                    QrCode = "QR-VIP-1",
                    DateGeneration = DateTime.UtcNow,
                    IdSociete = seed.IdSociete,
                    DateCreation = DateTime.UtcNow
                });
            await ctx.SaveChangesAsync();

            var loaded = await LoadBilletsLikeApiGetAsync(ctx, reservation.IdReservation);
            var dtos = loaded.Select(ToResponseDto).ToList();

            Assert.Equal(2, dtos.Count);
            Assert.Equal(seed.PrixEco, dtos[0].PrixVoyage);
            Assert.Equal(seed.PrixVip, dtos[1].PrixVoyage);
            Assert.Equal("Alice Eco", dtos[0].NomPassager);
            Assert.Equal("Bob VIP", dtos[1].NomPassager);
            Assert.NotEqual(dtos[0].PrixVoyage, dtos[1].PrixVoyage);
            Assert.NotEqual(seed.PrixVoyageGlobal, dtos[0].PrixVoyage);
            Assert.NotEqual(seed.PrixVoyageGlobal, dtos[1].PrixVoyage);
        }

        [Fact]
        public async Task ReservationWithPaiement_multi_passenger_billets_show_individual_tarif_not_total()
        {
            var db = nameof(ReservationWithPaiement_multi_passenger_billets_show_individual_tarif_not_total);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedEcoAndVipTwoSeatsAsync(ctx);

            var montantTotal = seed.PrixEco + seed.PrixVip;

            var mockRes = new Mock<IReservationRepository>();
            var mockPayRepo = new Mock<IPaiementRepository>();
            var mockQr = new Mock<IQrCodeService>();
            mockQr.Setup(q => q.GenerateUniqueQrCodeAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync(() => Guid.NewGuid().ToString("N"));

            var billetService = BilletServiceTestHelper.Create(ctx);
            var billetEmission = new BilletEmissionService(
                billetService,
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
                        new() { NomComplet = "Alice Eco", IdCategorieSiege = seed.IdCategorieEco },
                        new() { NomComplet = "Bob VIP", IdCategorieSiege = seed.IdCategorieVip }
                    }
                },
                Paiement = new PaiementDataDto
                {
                    MontantAPaye = montantTotal,
                    MontantPaye = montantTotal,
                    MethodePaiement = "ESPECES",
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete
                }
            };

            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.True(result.Statut == TransactionStatut.Succes, result.Message ?? "échec sans message");
            Assert.Equal(2, result.Billets.Count);

            var prixParBillet = result.Billets.Select(b => b.PrixVoyage!.Value).OrderBy(p => p).ToList();
            Assert.Equal(new[] { seed.PrixEco, seed.PrixVip }, prixParBillet);

            foreach (var billetDto in result.Billets)
            {
                Assert.NotNull(billetDto.PrixVoyage);
                Assert.NotEqual(seed.PrixVoyageGlobal, billetDto.PrixVoyage);
                Assert.NotEqual(montantTotal, billetDto.PrixVoyage);
            }

            var enBase = await LoadBilletsLikeApiGetAsync(ctx, result.Reservation!.IdReservation);
            var viaApi = enBase.Select(ToResponseDto).ToList();
            Assert.Equal(prixParBillet, viaApi.Select(b => b.PrixVoyage!.Value).OrderBy(p => p).ToList());
        }
    }
}
