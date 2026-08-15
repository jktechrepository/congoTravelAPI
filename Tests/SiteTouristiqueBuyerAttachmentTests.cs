using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.SiteTouristique.Strategies;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueBuyerAttachmentTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int userId, int jwtSocieteId = 999)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(userId);
            return mock;
        }

        private static SiteTouristiqueReservationWithPaiementService CreateWithPaiementService(
            CongoTravelDbContext ctx,
            ICurrentUserService currentUser)
        {
            var hold = new SiteTouristiqueHoldService(
                ctx,
                new SiteTouristiqueInventoryHoldStrategyFactory(
                    new SiteTouristiqueGlobalQuotaHoldStrategy(ctx),
                    new SiteTouristiqueClassQuotaHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<SiteTouristiqueHoldService>.Instance,
                currentUser);

            return new SiteTouristiqueReservationWithPaiementService(
                ctx,
                hold,
                SiteTouristiqueTestFactories.CreatePaymentService(ctx),
                Mock.Of<ISiteTouristiqueFlexPayInitiationService>(),
                SiteTouristiqueTestFactories.CreateReservationService(ctx),
                currentUser,
                NullLogger<SiteTouristiqueReservationWithPaiementService>.Instance);
        }

        [Fact]
        public async Task CreateCashAsync_attaches_IdUtilisateur_and_IdClient_from_jwt()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_attaches_IdUtilisateur_and_IdClient_from_jwt));
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "ST Buyer");

            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);

            var lieu = await lieuService.PublishAsync(
                (await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = "BUYER-ST",
                    Nom = "Lieu Buyer",
                    IdSite = idSite
                }, idSociete)).IdSiteTouristique,
                idSociete);

            var journee = await journeeService.PublishAsync(
                (await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
                {
                    IdSiteTouristique = lieu.IdSiteTouristique,
                    DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    InventoryMode = "GlobalQuota",
                    CodeDevise = "USD",
                    GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                    {
                        CapaciteTotale = 20,
                        PrixUnitaire = 15m
                    }
                }, idSociete)).IdSiteTouristiqueJournee,
                idSociete);

            ctx.Clients.Add(new Client
            {
                NomClient = "ST Acheteur",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            var idClient = await ctx.Clients.Select(c => c.IdClient).SingleAsync();

            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "ST JWT",
                MotDePasseHash = "x",
                IdClient = idClient,
                Statut = true
            });
            await ctx.SaveChangesAsync();
            var userId = await ctx.Utilisateurs.Select(u => u.IdUtilisateur).SingleAsync();

            var service = CreateWithPaiementService(ctx, MockClientUser(userId).Object);

            var result = await service.CreateCashAsync(new SiteTouristiqueReservationWithPaiementRequestDto
            {
                IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new SiteTouristiqueReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-ST-BUYER"
                }
            });

            Assert.Equal(userId, result.Reservation.IdUtilisateur);
            Assert.Equal(idClient, result.Reservation.IdClient);

            var stored = await ctx.SiteTouristiqueReservations.SingleAsync();
            Assert.Equal(userId, stored.IdUtilisateur);
            Assert.Equal(idClient, stored.IdClient);

            var listed = await SiteTouristiqueTestFactories.CreateReservationService(ctx).ListAsync(
                idSociete,
                new SiteTouristiqueReservationListFilter { IdClient = idClient });
            Assert.Single(listed);
            Assert.Equal(userId, listed[0].IdUtilisateur);
        }

        private static async Task<(int IdSociete, int IdJournee)> SeedPublishedGlobalJourneeAsync(
            CongoTravelDbContext ctx,
            string suffix)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, $"ST {suffix}");
            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);

            var lieu = await lieuService.PublishAsync(
                (await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
                {
                    CodeLieu = $"LIEU-{suffix}",
                    Nom = $"Lieu {suffix}",
                    IdSite = idSite
                }, idSociete)).IdSiteTouristique,
                idSociete);

            var journee = await journeeService.PublishAsync(
                (await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
                {
                    IdSiteTouristique = lieu.IdSiteTouristique,
                    DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                    InventoryMode = "GlobalQuota",
                    CodeDevise = "USD",
                    GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                    {
                        CapaciteTotale = 20,
                        PrixUnitaire = 15m
                    }
                }, idSociete)).IdSiteTouristiqueJournee,
                idSociete);

            return (idSociete, journee.IdSiteTouristiqueJournee);
        }

        [Fact]
        public async Task CreateCashAsync_uses_IdClient_from_body_over_jwt()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_uses_IdClient_from_body_over_jwt));
            var (_, idJournee) = await SeedPublishedGlobalJourneeAsync(ctx, "BODY");

            ctx.Clients.AddRange(
                new Client { NomClient = "JWT Client", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { NomClient = "Body Client", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
            var clients = await ctx.Clients.OrderBy(c => c.IdClient).Select(c => c.IdClient).ToListAsync();
            var jwtClientId = clients[0];
            var bodyClientId = clients[1];

            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "ST JWT",
                MotDePasseHash = "x",
                IdClient = jwtClientId,
                Statut = true
            });
            await ctx.SaveChangesAsync();
            var userId = await ctx.Utilisateurs.Select(u => u.IdUtilisateur).SingleAsync();

            var service = CreateWithPaiementService(ctx, MockClientUser(userId).Object);

            var result = await service.CreateCashAsync(new SiteTouristiqueReservationWithPaiementRequestDto
            {
                IdSiteTouristiqueJournee = idJournee,
                IdClient = bodyClientId,
                Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new SiteTouristiqueReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-ST-BODY"
                }
            });

            Assert.Equal(userId, result.Reservation.IdUtilisateur);
            Assert.Equal(bodyClientId, result.Reservation.IdClient);
            Assert.Equal(bodyClientId, (await ctx.SiteTouristiqueReservations.SingleAsync()).IdClient);
        }

        [Fact]
        public async Task CreateCashAsync_rejects_unknown_IdClient_in_body()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_rejects_unknown_IdClient_in_body));
            var (_, idJournee) = await SeedPublishedGlobalJourneeAsync(ctx, "BAD");
            var service = CreateWithPaiementService(ctx, MockClientUser(0).Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCashAsync(new SiteTouristiqueReservationWithPaiementRequestDto
                {
                    IdSiteTouristiqueJournee = idJournee,
                    IdClient = 999999,
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new SiteTouristiqueReservationPaiementBlockDto
                    {
                        MethodePaiement = "CASH",
                        ReferenceTransaction = "CAISSE-ST-BAD"
                    }
                }));

            Assert.Contains("999999", ex.Message);
            Assert.Equal(0, await ctx.SiteTouristiqueReservations.CountAsync());
        }
    }
}
