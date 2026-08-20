using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementReservationWithPaiementServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> MockClientUser(int jwtSocieteId = 1, int userId = 0)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            mock.SetupGet(u => u.UserId).Returns(userId);
            return mock;
        }

        private static Mock<ICurrentUserService> MockStaffUser(int jwtSocieteId)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(true);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
            return mock;
        }

        private static EvenementReservationWithPaiementService CreateService(
            CongoTravelDbContext ctx,
            IFlexPayService? flexPay = null,
            bool flexEnabled = true,
            ICurrentUserService? currentUser = null)
        {
            var hold = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<EvenementHoldService>.Instance);

            var payment = EvenementTestFactories.CreatePaymentService(ctx);
            var flexInit = EvenementTestFactories.CreateFlexPayInitiationService(
                ctx,
                flexPay ?? Mock.Of<IFlexPayService>(),
                enabled: flexEnabled);

            var reservation = new EvenementReservationService(
                ctx,
                new EvenementInventoryCancelStrategyFactory(
                    new EvenementGlobalQuotaCancelStrategy(ctx),
                    new EvenementClassQuotaCancelStrategy(ctx),
                    new EvenementSeatNumberedCancelStrategy(ctx)),
                Moq.Mock.Of<CongoTravel.Services.Repositories.IFlexPayRealtimeNotifier>(),
                NullLogger<EvenementReservationService>.Instance);

            return new EvenementReservationWithPaiementService(
                ctx,
                hold,
                payment,
                flexInit,
                EvenementTestFactories.CreateCommandeFlexPayService(
                    ctx,
                    flexPay ?? Mock.Of<IFlexPayService>(),
                    flexPayOptions: new CongoTravel.Configuration.FlexPayOptions
                    {
                        Enabled = flexEnabled,
                        EventEnabled = flexEnabled,
                        CallbackBaseUrl = "https://api.test.example/api/FlexPay/callback",
                        EventCallbackRelativePath = "/api/events/flexpay/callback"
                    }),
                reservation,
                currentUser ?? MockClientUser().Object,
                NullLogger<EvenementReservationWithPaiementService>.Instance);
        }

        [Fact]
        public async Task CreateCashAsync_hold_and_confirm_in_one_call()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_hold_and_confirm_in_one_call));
            var (idSociete, idSite, idSession) = await SeedPublishedGlobalSessionAsync(ctx, capacite: 20, prix: 15m);
            var service = CreateService(ctx, currentUser: MockClientUser(jwtSocieteId: 999).Object);

            var result = await service.CreateCashAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                CustomerRef = "GUICHET-1",
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-99"
                }
            });

            Assert.Equal("Succes", result.TransactionStatut);
            Assert.Equal("CONFIRMED", result.Reservation.Status);
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(idSite, result.Payment!.IdSite);
            Assert.Equal("SUCCEEDED", result.Payment!.Status);
            Assert.Equal(2, result.Tickets.Count);
            Assert.Equal(30m, result.Payment.Montant);

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(2, quota.QuantiteVendue);
            Assert.Equal(1, await ctx.EvenementReservations.CountAsync());
        }

        [Fact]
        public async Task CreateCashAsync_attaches_IdUtilisateur_and_IdClient_from_jwt()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_attaches_IdUtilisateur_and_IdClient_from_jwt));
            var (idSociete, _, idSession) = await SeedPublishedGlobalSessionAsync(ctx, capacite: 20, prix: 15m);

            ctx.Clients.Add(new Client
            {
                NomClient = "Acheteur Test",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            var idClient = await ctx.Clients.Select(c => c.IdClient).SingleAsync();

            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "Acheteur JWT",
                MotDePasseHash = "x",
                IdClient = idClient,
                Statut = true
            });
            await ctx.SaveChangesAsync();
            var userId = await ctx.Utilisateurs.Select(u => u.IdUtilisateur).SingleAsync();

            var service = CreateService(ctx, currentUser: MockClientUser(jwtSocieteId: 999, userId: userId).Object);

            var result = await service.CreateCashAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-BUYER"
                }
            });

            Assert.Equal(userId, result.Reservation.IdUtilisateur);
            Assert.Equal(idClient, result.Reservation.IdClient);

            var stored = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(userId, stored.IdUtilisateur);
            Assert.Equal(idClient, stored.IdClient);
        }

        [Fact]
        public async Task CreateCashAsync_uses_IdClient_from_body_over_jwt()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_uses_IdClient_from_body_over_jwt));
            var (_, _, idSession) = await SeedPublishedGlobalSessionAsync(ctx, capacite: 20, prix: 15m);

            ctx.Clients.AddRange(
                new Client { NomClient = "JWT Client", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow },
                new Client { NomClient = "Body Client", Statut = true, IsActif = true, DateCreation = DateTime.UtcNow });
            await ctx.SaveChangesAsync();
            var clients = await ctx.Clients.OrderBy(c => c.IdClient).Select(c => c.IdClient).ToListAsync();
            var jwtClientId = clients[0];
            var bodyClientId = clients[1];

            ctx.Utilisateurs.Add(new Utilisateur
            {
                NomComplet = "Acheteur JWT",
                MotDePasseHash = "x",
                IdClient = jwtClientId,
                Statut = true
            });
            await ctx.SaveChangesAsync();
            var userId = await ctx.Utilisateurs.Select(u => u.IdUtilisateur).SingleAsync();

            var service = CreateService(ctx, currentUser: MockClientUser(jwtSocieteId: 999, userId: userId).Object);

            var result = await service.CreateCashAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                IdClient = bodyClientId,
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-BODY-CLIENT"
                }
            });

            Assert.Equal(userId, result.Reservation.IdUtilisateur);
            Assert.Equal(bodyClientId, result.Reservation.IdClient);
            Assert.Equal(bodyClientId, (await ctx.EvenementReservations.SingleAsync()).IdClient);
        }

        [Fact]
        public async Task CreateCashAsync_rejects_unknown_IdClient_in_body()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_rejects_unknown_IdClient_in_body));
            var (_, _, idSession) = await SeedPublishedGlobalSessionAsync(ctx);
            var service = CreateService(ctx);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCashAsync(new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    IdClient = 999999,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "CASH",
                        ReferenceTransaction = "CAISSE-BAD-CLIENT"
                    }
                }));

            Assert.Contains("999999", ex.Message);
            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
        }

        [Fact]
        public async Task CreateCashAsync_rejects_electronic_method()
        {
            await using var ctx = BuildDb(nameof(CreateCashAsync_rejects_electronic_method));
            var (_, _, idSession) = await SeedPublishedGlobalSessionAsync(ctx);
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateCashAsync(new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000001",
                        IdSite = 1
                    }
                }));

            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
        }

        [Fact]
        public async Task InitiateElectronicAsync_hold_and_initiate_in_one_call()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_hold_and_initiate_in_one_call));
            var (idSociete, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock("FP-FACADE-001");
            var service = CreateService(ctx, flexApi.Object);

            var result = await service.InitiateElectronicAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 2 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001",
                    IdSite = idSite
                }
            });

            Assert.Equal("EnAttente", result.TransactionStatut);
            Assert.Equal("EN_ATTENTE_PAIEMENT", result.Reservation.Status);
            Assert.Equal(0, result.Reservation.IdEvenementReservation);
            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
            Assert.Equal(1, await ctx.EvenementCommandesEnAttente.CountAsync());
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal("PENDING", result.Payment!.Status);
            Assert.Equal("FP-FACADE-001", result.OrderNumber);
            Assert.True(result.FlexPayAccepted);
            Assert.Empty(result.Tickets);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(idSite, result.Payment!.IdSite);
            Assert.Null(await ctx.EvenementPayments.Select(p => p.IdEvenementReservation).SingleAsync());

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(2, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task InitiateElectronicAsync_defaults_idSite_from_session_when_omitted()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_defaults_idSite_from_session_when_omitted));
            var (_, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock("FP-FACADE-DEFAULT");
            var service = CreateService(ctx, flexApi.Object);

            var result = await service.InitiateElectronicAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "243900000001"
                    // IdSite omis → session.IdSite
                }
            });

            Assert.Equal("EnAttente", result.TransactionStatut);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(idSite, result.Payment!.IdSite);
        }

        [Fact]
        public async Task InitiateElectronicAsync_rolls_back_commande_when_flexpay_fails()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_rolls_back_commande_when_flexpay_fails));
            var (_, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            // FlexPay événement désactivé → échec après création commande + hold inventaire
            var service = CreateService(ctx, Mock.Of<IFlexPayService>(), flexEnabled: false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitiateElectronicAsync(new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000001",
                        IdSite = idSite
                    }
                }));

            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
            Assert.Equal(0, await ctx.EvenementCommandesEnAttente.CountAsync());

            var quota = await ctx.EvenementSessionGlobalQuotas.SingleAsync();
            Assert.Equal(0, quota.QuantiteHold);
            Assert.Equal(0, quota.QuantiteVendue);
        }

        [Fact]
        public async Task InitiateElectronicAsync_client_uses_session_societe_not_jwt()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_client_uses_session_societe_not_jwt));
            var (sessionSociete, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            var flexApi = EvenementTestFactories.CreateFlexPayApiMock("FP-CROSS-TENANT");
            // JWT Client sur une autre société (ex. 1) vs session organisateur
            var service = CreateService(
                ctx,
                flexApi.Object,
                currentUser: MockClientUser(jwtSocieteId: sessionSociete + 100).Object);

            var result = await service.InitiateElectronicAsync(new EvenementReservationWithPaiementRequestDto
            {
                IdEvenementSession = idSession,
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                Paiement = new EvenementReservationPaiementBlockDto
                {
                    MethodePaiement = "MOBILE_MONEY",
                    Phone = "0825099299",
                    IdSite = idSite
                }
            });

            Assert.Equal("EnAttente", result.TransactionStatut);
            Assert.Equal(sessionSociete, result.Reservation.IdSociete);
            Assert.Equal("FP-CROSS-TENANT", result.OrderNumber);
        }

        [Fact]
        public async Task InitiateElectronicAsync_staff_cannot_buy_other_societe_session()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_staff_cannot_buy_other_societe_session));
            var (sessionSociete, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            var staffJwtSociete = sessionSociete + 50;
            var service = CreateService(
                ctx,
                EvenementTestFactories.CreateFlexPayApiMock("FP-STAFF").Object,
                currentUser: MockStaffUser(staffJwtSociete).Object);

            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.InitiateElectronicAsync(new EvenementReservationWithPaiementRequestDto
                {
                    IdEvenementSession = idSession,
                    Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 1 } },
                    Paiement = new EvenementReservationPaiementBlockDto
                    {
                        MethodePaiement = "MOBILE_MONEY",
                        Phone = "243900000001",
                        IdSite = idSite
                    }
                }));

            Assert.Contains($"société {staffJwtSociete}", ex.Message);
            Assert.Equal(0, await ctx.EvenementReservations.CountAsync());
        }

        private static async Task<(int IdSociete, int IdSite, int IdSession)> SeedPublishedGlobalSessionAsync(
            CongoTravelDbContext ctx,
            int capacite = 50,
            decimal prix = 20m)
        {
            var (idSociete, idSite) = await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, "Facade Societe");

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeSession = $"FC-{Guid.NewGuid():N}"[..10],
                Libelle = "Facade session",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            ctx.EvenementSessionGlobalQuotas.Add(new EvenementSessionGlobalQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                CapaciteTotale = capacite,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = prix,
                CodeDevise = "USD"
            });
            await ctx.SaveChangesAsync();

            return (idSociete, idSite, session.IdEvenementSession);
        }

        private static async Task<(int IdSociete, int IdSite, int IdSession)> SeedPublishedSessionWithFlexPayAsync(
            CongoTravelDbContext ctx)
        {
            var (idSociete, idSite, idSession) = await SeedPublishedGlobalSessionAsync(ctx, capacite: 50, prix: 20m);

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeMarchand = "MERCHANT-FACADE",
                ApiToken = "token-test",
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                Statut = true
            });
            await ctx.SaveChangesAsync();

            return (idSociete, idSite, idSession);
        }
    }
}
