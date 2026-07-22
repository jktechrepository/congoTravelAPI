using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;


namespace CongoTravel.Tests
{
    public class SitePhase1Tests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        private static Mock<IEmailService> EmailMock()
        {
            var m = new Mock<IEmailService>();
            m.Setup(e => e.SendWelcomeEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            return m;
        }

        private static SiteService CreateSiteService(CongoTravelDbContext ctx) =>
            new SiteService(ctx, EmailMock().Object, NullLogger<SiteService>.Instance);

        [Fact]
        public async Task Site_Code_must_be_unique_per_societe()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(Site_Code_must_be_unique_per_societe)));
            var s = new Societe { Nom = "S", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var svc = CreateSiteService(ctx);
            await svc.CreateAsync(new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "A1",
                NomSite = "Ag1",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.CreateAsync(new Site
                {
                    IdSociete = s.IdSociete,
                    CodeSite = "A1",
                    NomSite = "Ag2",
                    NomResponsableSite = "R",
                    Genre = "Masculin",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                }));
        }

        [Fact]
        public async Task EnsureSiteBelongsToSociete_throws_when_wrong_societe()
        {
            await using var ctx = new CongoTravelDbContext(Options(nameof(EnsureSiteBelongsToSociete_throws_when_wrong_societe)));
            var s1 = new Societe { Nom = "S1", DateCreation = DateTime.UtcNow };
            var s2 = new Societe { Nom = "S2", DateCreation = DateTime.UtcNow };
            ctx.Societes.AddRange(s1, s2);
            await ctx.SaveChangesAsync();

            var ag = new Site
            {
                IdSociete = s1.IdSociete,
                CodeSite = "X",
                NomSite = "Ax",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(ag);
            await ctx.SaveChangesAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(ctx, ag.IdSite, s2.IdSociete));
        }

        private static ReservationWithPaiementService CreateSut(CongoTravelDbContext ctx)
        {
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
                CurrentUserTestHelper.MockCaissier());
        }

        /// <summary>Reuse seed shape from tarif tests (2 sièges, 2 places).</summary>
        private static async Task<(int IdSociete, int IdClient, int IdUtilisateur, int IdVoyage, int IdCategorieSiege)> SeedTwoPlacesAsync(
            CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "CoAg", DateCreation = DateTime.UtcNow };
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
                Email = "u@site.phase1.test",
                MotDePasseHash = "h",
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();

            return (s.IdSociete, client.IdClient, user.IdUtilisateur, voy.Id, eco.IdCategorieSiege);
        }

        [Fact]
        public async Task CreateReservationWithPaiement_succeeds_without_IdSite()
        {
            var db = nameof(CreateReservationWithPaiement_succeeds_without_IdSite);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPlacesAsync(ctx);

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
            var res = await ctx.Reservations.SingleAsync();
            var pay = await ctx.Paiements.SingleAsync();
            Assert.Null(res.IdSite);
            Assert.Null(pay.IdSite);
        }

        [Fact]
        public async Task CreateReservationWithPaiement_fails_when_IdSite_wrong_societe()
        {
            var db = nameof(CreateReservationWithPaiement_fails_when_IdSite_wrong_societe);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPlacesAsync(ctx);

            var otherSoc = new Societe { Nom = "Other", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(otherSoc);
            await ctx.SaveChangesAsync();
            var badSite = new Site
            {
                IdSociete = otherSoc.IdSociete,
                CodeSite = "BAD",
                NomSite = "Bad",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(badSite);
            await ctx.SaveChangesAsync();

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    IdSite = badSite.IdSite,
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
                    IdSociete = seed.IdSociete,
                    IdSite = badSite.IdSite
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Echec, result.Statut);
            Assert.Contains("site", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateReservationWithPaiement_succeeds_with_matching_IdSite()
        {
            var db = nameof(CreateReservationWithPaiement_succeeds_with_matching_IdSite);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var seed = await SeedTwoPlacesAsync(ctx);

            var ag = new Site
            {
                IdSociete = seed.IdSociete,
                CodeSite = "OK",
                NomSite = "Ok",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(ag);
            await ctx.SaveChangesAsync();

            var dto = new CreateReservationWithPaiementDto
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = seed.IdVoyage,
                    IdClient = seed.IdClient,
                    NombreDePlace = 2,
                    IdUtilisateur = seed.IdUtilisateur,
                    IdSociete = seed.IdSociete,
                    IdSite = ag.IdSite,
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
                    IdSociete = seed.IdSociete,
                    IdSite = ag.IdSite
                }
            };

            var sut = CreateSut(ctx);
            var result = await sut.CreateReservationWithPaiementAsync(dto);

            Assert.Equal(TransactionStatut.Succes, result.Statut);
            var res = await ctx.Reservations.SingleAsync();
            var pay = await ctx.Paiements.SingleAsync();
            Assert.Equal(ag.IdSite, res.IdSite);
            Assert.Equal(ag.IdSite, pay.IdSite);
        }

        [Fact]
        public async Task Bootstrap_site_is_principal_by_default()
        {
            var db = nameof(Bootstrap_site_is_principal_by_default);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = CreateSiteService(ctx);

            var s = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var site = await svc.CreateAsync(new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "MAIN",
                NomSite = "Principal",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            });

            Assert.True(site.IsSitePrincipal);
        }

        [Fact]
        public async Task Update_site_transfers_principal_uniqueness()
        {
            var db = nameof(Update_site_transfers_principal_uniqueness);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = CreateSiteService(ctx);

            var s = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var principal = await svc.CreateAsync(new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "A",
                NomSite = "A",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            });

            var satellite = await svc.CreateAsync(new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "B",
                NomSite = "B",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });

            var updated = await svc.UpdateAsync(new Site
            {
                IdSite = satellite.IdSite,
                CodeSite = satellite.CodeSite,
                NomSite = satellite.NomSite,
                NomResponsableSite = satellite.NomResponsableSite,
                Genre = satellite.Genre,
                Statut = true
            }, isSitePrincipal: true);

            Assert.NotNull(updated);
            Assert.True(updated!.IsSitePrincipal);

            var former = await ctx.Sites.AsNoTracking().SingleAsync(x => x.IdSite == principal.IdSite);
            Assert.False(former.IsSitePrincipal);
        }

        [Fact]
        public async Task ToggleStatut_rejects_deactivating_principal_site()
        {
            var db = nameof(ToggleStatut_rejects_deactivating_principal_site);
            await using var ctx = new CongoTravelDbContext(Options(db));
            var svc = CreateSiteService(ctx);

            var s = new Societe { Nom = "Soc", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var site = await svc.CreateAsync(new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "MAIN",
                NomSite = "Principal",
                NomResponsableSite = "R",
                Genre = "Masculin",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ToggleStatutAsync(site.IdSite));
        }
    }
}
