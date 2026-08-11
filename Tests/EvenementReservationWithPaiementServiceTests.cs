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

        private static Mock<ICurrentUserService> MockClientUser(int jwtSocieteId = 1)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(u => u.IsStaff).Returns(false);
            mock.SetupGet(u => u.IsSuperAdmin).Returns(false);
            mock.SetupGet(u => u.SocieteId).Returns(jwtSocieteId);
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
            Assert.Equal("HOLD", result.Reservation.Status);
            Assert.Equal(idSociete, result.Reservation.IdSociete);
            Assert.Equal("PENDING", result.Payment!.Status);
            Assert.Equal("FP-FACADE-001", result.OrderNumber);
            Assert.True(result.FlexPayAccepted);
            Assert.Empty(result.Tickets);
            Assert.Equal(idSite, result.Reservation.IdSite);
            Assert.Equal(idSite, result.Payment!.IdSite);

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
        public async Task InitiateElectronicAsync_rolls_back_hold_when_flexpay_fails()
        {
            await using var ctx = BuildDb(nameof(InitiateElectronicAsync_rolls_back_hold_when_flexpay_fails));
            var (_, idSite, idSession) = await SeedPublishedSessionWithFlexPayAsync(ctx);
            // FlexPay événement désactivé → échec après création du hold
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

            var reservation = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(EvenementReservationStatus.CANCELLED, reservation.Status);

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
