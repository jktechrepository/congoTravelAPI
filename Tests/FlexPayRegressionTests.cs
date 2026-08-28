using Microsoft.AspNetCore.Http;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Mapping;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.DTOs.Sync;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Bloc A (CASH) et Bloc B (FlexPay) — garde-fous non-régression (plan FlexPay).
    /// </summary>
    public class FlexPayRegressionTests
    {
        private static DbContextOptions<CongoTravelDbContext> CreateDbOptions(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options;

        [Theory]
        [InlineData("MOBILE_MONEY")]
        [InlineData("CARTE_BANCAIRE")]
        public void Cash_endpoint_rejects_electronic_methods(string methode)
        {
            Assert.Throws<InvalidOperationException>(() =>
                MethodePaiementHelper.EnsureCashOnlyForGuichetEndpoint(methode));
        }

        [Theory]
        [InlineData("CASH")]
        [InlineData("ESPECES")]
        [InlineData("Espèces")]
        public void Cash_endpoint_allows_cash_methods(string methode)
        {
            var ex = Record.Exception(() => MethodePaiementHelper.EnsureCashOnlyForGuichetEndpoint(methode));
            Assert.Null(ex);
        }

        [Fact]
        public void Normalize_maps_especes_to_CASH()
        {
            Assert.Equal(MethodePaiementHelper.Cash, MethodePaiementHelper.NormalizeForStorage("ESPECES"));
            Assert.Equal(MethodePaiementHelper.MobileMoney, MethodePaiementHelper.NormalizeForStorage("mobile_money"));
        }

        [Fact]
        public void GetRecetteBucket_classifies_CASH_and_MOBILE_MONEY()
        {
            Assert.Equal(MethodePaiementHelper.RecetteBucket.Espece,
                MethodePaiementHelper.GetRecetteBucket("CASH"));
            Assert.Equal(MethodePaiementHelper.RecetteBucket.Espece,
                MethodePaiementHelper.GetRecetteBucket("ESPECES"));
            Assert.Equal(MethodePaiementHelper.RecetteBucket.MobileMoney,
                MethodePaiementHelper.GetRecetteBucket("MOBILE_MONEY"));
        }

        [Fact]
        public async Task Sync_batch_rejects_MOBILE_MONEY()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(Sync_batch_rejects_MOBILE_MONEY)));
            var sync = CreateSyncService(ctx);

            var result = await sync.ProcessPaymentsBatchAsync(1, new PaymentBatchRequestDto
            {
                Items = new List<PaymentRequestDto>
                {
                    new()
                    {
                        ClientRequestId = "c1",
                        IdClient = 1,
                        MontantPaye = 10,
                        DatePaiementUtc = DateTime.UtcNow,
                        MethodePaiement = "MOBILE_MONEY"
                    }
                }
            });

            Assert.Equal(1, result.Summary.Errors);
            Assert.Equal("rejected", result.Results[0].Status);
            Assert.Equal(0, await ctx.Paiements.CountAsync());
        }

        [Fact]
        public async Task Siege_hold_reduces_disponibles_count()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(Siege_hold_reduces_disponibles_count)));
            var (idVoyage, idSiege, idCat, _) = await SeedMinimalVoyageAsync(ctx);
            var dispo = SiegeDisponibiliteTestHelper.Create(ctx);

            var before = await dispo.GetIndisponibleSiegeIdsAsync(idVoyage);
            Assert.Empty(before);

            var commandeId = Guid.NewGuid();
            await dispo.CreateHoldsForCategoriesAsync(idVoyage, commandeId, new[] { idCat }, 15);

            var after = await dispo.GetIndisponibleSiegeIdsAsync(idVoyage);
            Assert.Contains(idSiege, after);

            await dispo.ReleaseHoldsForCommandeAsync(commandeId);
            var released = await dispo.GetIndisponibleSiegeIdsAsync(idVoyage);
            Assert.Empty(released);
        }

        [Fact]
        public async Task FlexPay_initiate_creates_hold_and_pending_paiement_without_reservation()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions("FlexPay_initiate_creates_hold"));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);

            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);

            var response = await flexPay.InitiateAsync(dto);

            Assert.Equal(TransactionStatut.EnAttente, response.Statut);
            Assert.NotEqual(Guid.Empty, response.IdCommandeReservationEnAttente);
            Assert.False(string.IsNullOrWhiteSpace(response.OrderNumberFlexPay));
            Assert.Equal(response.OrderNumberFlexPay, response.TransactionId);
            Assert.True(response.FlexPayAccepted);
            Assert.NotNull(response.HoldExpireAt);
            Assert.Equal(0, response.Reservation.IdReservation);
            Assert.Equal("EN_ATTENTE_PAIEMENT", response.Reservation.StatutReservation);
            Assert.False(response.Paiement.Statut);
            Assert.Empty(response.Billets);
            Assert.NotNull(response.Reservation.Passagers);
            Assert.Single(response.Reservation.Passagers!);
            Assert.Equal(0, await ctx.Reservations.CountAsync());
            Assert.Equal(1, await ctx.SiegeHoldsEnAttente.CountAsync());
            Assert.Equal(1, await ctx.Paiements.CountAsync());
            var commande = await ctx.CommandesReservationEnAttente.SingleAsync();
            var paiement = await ctx.Paiements.SingleAsync();
            Assert.Equal(Models.Enums.OrigineOperation.CLIENT, commande.Origine);
            Assert.Equal(Models.Enums.OrigineOperation.CLIENT, paiement.Origine);
            Assert.Equal(Models.Enums.OrigineOperation.CLIENT, response.Paiement.Origine);
            Assert.False(paiement.Statut);
            Assert.Equal((int)StatutPaiementMetier.EnAttente, paiement.StatutPaiementMetier);
            Assert.Null(paiement.IdReservation);
        }

        [Fact]
        public async Task FlexPay_initiate_fails_when_disabled()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions("FlexPay_initiate_fails_when_disabled"));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: false);
            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);

            await Assert.ThrowsAsync<InvalidOperationException>(() => flexPay.InitiateAsync(dto));
        }

        [Fact]
        public async Task FlexPay_callback_success_creates_reservation_idempotent()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_callback_success_creates_reservation_idempotent)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);
            var initiated = await flexPay.InitiateAsync(dto);

            var callbackSvc = CreateCallbackService(ctx);
            var cb = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = initiated.OrderNumberFlexPay,
                Reference = initiated.ReferenceFlexPay,
                Amount = "50000",
                Currency = "CDF"
            };

            var first = await callbackSvc.ProcessCallbackAsync(cb, "{}", null, null);
            Assert.True(first.Success);
            Assert.Equal(1, await ctx.Reservations.CountAsync());
            Assert.Equal(0, await ctx.SiegeHoldsEnAttente.CountAsync());
            Assert.Equal(0, await ctx.CommandesReservationEnAttente.CountAsync());

            var second = await callbackSvc.ProcessCallbackAsync(cb, "{}", null, null);
            Assert.True(second.AlreadyProcessed);
            Assert.Equal(1, await ctx.Reservations.CountAsync());
        }

        [Fact]
        public async Task FlexPay_callback_success_persists_audit_and_links_paiement_to_reservation()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_callback_success_persists_audit_and_links_paiement_to_reservation)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            var realtimeMock = new Mock<IFlexPayRealtimeNotifier>();
            var callbackSvc = CreateCallbackService(ctx, realtimeMock);
            var cb = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = initiated.OrderNumberFlexPay,
                Reference = initiated.ReferenceFlexPay,
                Amount = "50000",
                Currency = "CDF"
            };

            var result = await callbackSvc.ProcessCallbackAsync(cb, "{}", "test-headers", "127.0.0.1");

            Assert.True(result.Success);
            Assert.NotNull(result.IdReservation);
            Assert.NotNull(result.IdPaiement);

            var paiement = await ctx.Paiements.SingleAsync();
            Assert.True(paiement.Statut);
            Assert.Equal(result.IdReservation, paiement.IdReservation);
            Assert.Equal((int)StatutPaiementMetier.Reussi, paiement.StatutPaiementMetier);

            Assert.Equal(1, await ctx.CallbacksFlexPay.CountAsync());
            var audit = await ctx.CallbacksFlexPay.SingleAsync();
            Assert.Equal("0", audit.Code);
            Assert.True(audit.TraiteAvecSucces);
            Assert.Equal(0, await ctx.CommandesReservationEnAttente.CountAsync());
            Assert.Equal(0, await ctx.SiegeHoldsEnAttente.CountAsync());

            realtimeMock.Verify(
                n => n.NotifyPaymentConfirmedAsync(
                    user.IdUtilisateur,
                    initiated.OrderNumberFlexPay!,
                    result.IdReservation!.Value,
                    result.IdPaiement!.Value,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task FlexPay_callback_failure_releases_holds_and_notifies()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_callback_failure_releases_holds_and_notifies)));
            var (idVoyage, idSiege, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            Assert.Equal(1, await ctx.SiegeHoldsEnAttente.CountAsync());

            var realtimeMock = new Mock<IFlexPayRealtimeNotifier>();
            var callbackSvc = CreateCallbackService(ctx, realtimeMock);
            var cb = new FlexPayCallbackDto
            {
                Code = "1",
                OrderNumber = initiated.OrderNumberFlexPay,
                Reference = initiated.ReferenceFlexPay,
                Amount = "50000",
                Currency = "CDF"
            };

            var result = await callbackSvc.ProcessCallbackAsync(cb, "{}", null, null);

            Assert.True(result.Success);
            Assert.Equal(0, await ctx.Reservations.CountAsync());
            Assert.Equal(0, await ctx.SiegeHoldsEnAttente.CountAsync());
            Assert.Equal(0, await ctx.CommandesReservationEnAttente.CountAsync());

            var paiement = await ctx.Paiements.SingleAsync();
            Assert.False(paiement.Statut);
            Assert.Equal((int)StatutPaiementMetier.Echec, paiement.StatutPaiementMetier);
            Assert.Null(paiement.IdReservation);

            var dispo = SiegeDisponibiliteTestHelper.Create(ctx);
            var indispo = await dispo.GetIndisponibleSiegeIdsAsync(idVoyage);
            Assert.DoesNotContain(idSiege, indispo);

            realtimeMock.Verify(
                n => n.NotifyPaymentFailedAsync(
                    user.IdUtilisateur,
                    initiated.OrderNumberFlexPay!,
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task FlexPay_verifier_pending_does_not_remove_commande()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_verifier_pending_does_not_remove_commande)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            Assert.Equal(1, await ctx.CommandesReservationEnAttente.CountAsync());
            Assert.Equal(1, await ctx.SiegeHoldsEnAttente.CountAsync());

            var callbackSvc = CreateCallbackService(ctx, flexPayCheckStatus: "2");
            var result = await callbackSvc.VerifyAndFinalizeAsync(initiated.OrderNumberFlexPay!);

            Assert.False(result.IsUnifiedSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.True(result.StatusOnly!.Success);
            Assert.True(result.StatusOnly.PaymentPending);
            Assert.False(result.StatusOnly.AlreadyProcessed);
            Assert.Null(result.StatusOnly.IdReservation);
            Assert.Equal(1, await ctx.CommandesReservationEnAttente.CountAsync());
            Assert.Equal(1, await ctx.SiegeHoldsEnAttente.CountAsync());

            var paiement = await ctx.Paiements.SingleAsync();
            Assert.False(paiement.Statut);
            Assert.Equal((int)StatutPaiementMetier.EnAttente, paiement.StatutPaiementMetier);
        }

        [Fact]
        public async Task FlexPay_verifier_success_finalizes_reservation()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_verifier_success_finalizes_reservation)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            var callbackSvc = CreateCallbackService(ctx, flexPayCheckStatus: "0");
            var result = await callbackSvc.VerifyAndFinalizeAsync(initiated.OrderNumberFlexPay!);

            Assert.True(result.IsUnifiedSuccess);
            Assert.NotNull(result.ReservationWithPaiement);
            Assert.Equal(TransactionStatut.Succes, result.ReservationWithPaiement!.Statut);
            Assert.True(result.ReservationWithPaiement.Reservation.IdReservation > 0);
            Assert.NotEmpty(result.ReservationWithPaiement.Billets);
            Assert.Equal(0, await ctx.CommandesReservationEnAttente.CountAsync());
            Assert.Equal(1, await ctx.Reservations.CountAsync());

            var paiement = await ctx.Paiements.SingleAsync();
            Assert.True(paiement.Statut);
            Assert.Equal(result.ReservationWithPaiement.Reservation.IdReservation, paiement.IdReservation);
        }

        [Fact]
        public async Task FlexPay_verifier_already_processed_returns_unified_with_billets()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_verifier_already_processed_returns_unified_with_billets)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            var callbackSvc = CreateCallbackService(ctx, flexPayCheckStatus: "0");
            var first = await callbackSvc.VerifyAndFinalizeAsync(initiated.OrderNumberFlexPay!);
            Assert.True(first.IsUnifiedSuccess);

            var second = await callbackSvc.VerifyAndFinalizeAsync(initiated.OrderNumberFlexPay!);
            Assert.True(second.IsUnifiedSuccess);
            Assert.True(second.ReservationWithPaiement!.Statut == TransactionStatut.Succes);
            Assert.NotEmpty(second.ReservationWithPaiement.Billets);
            Assert.Equal(first.ReservationWithPaiement!.Reservation.IdReservation,
                second.ReservationWithPaiement.Reservation.IdReservation);
        }

        [Fact]
        public async Task FlexPay_verifier_failure_marks_echec_and_removes_commande()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_verifier_failure_marks_echec_and_removes_commande)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            var callbackSvc = CreateCallbackService(ctx, flexPayCheckStatus: "1");
            var result = await callbackSvc.VerifyAndFinalizeAsync(initiated.OrderNumberFlexPay!);

            Assert.False(result.IsUnifiedSuccess);
            Assert.NotNull(result.StatusOnly);
            Assert.True(result.StatusOnly!.Success);
            Assert.False(result.StatusOnly.PaymentPending);
            Assert.Contains("refusé", result.StatusOnly.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await ctx.CommandesReservationEnAttente.CountAsync());
            Assert.Equal(0, await ctx.Reservations.CountAsync());

            var paiement = await ctx.Paiements.SingleAsync();
            Assert.False(paiement.Statut);
            Assert.Equal((int)StatutPaiementMetier.Echec, paiement.StatutPaiementMetier);
        }

        private static InitiateFlexPayReservationDto BuildFlexPayDto(
            int idVoyage, int idSociete, int idSite, int idClient, int idUtilisateur, int idCat, decimal montant) =>
            new()
            {
                Reservation = new ReservationDataDto
                {
                    IdVoyage = idVoyage,
                    IdClient = idClient,
                    NombreDePlace = 1,
                    IdUtilisateur = idUtilisateur,
                    IdSociete = idSociete,
                    IdSite = idSite,
                    Passagers = new List<ReservationPassengerInputDto>
                    {
                        new()
                        {
                            IdCategorieSiege = idCat,
                            NomComplet = "Test Passager"
                        }
                    }
                },
                Paiement = new FlexPayPaiementDataDto
                {
                    MontantAPaye = montant,
                    MethodePaiement = MethodePaiementHelper.MobileMoney,
                    CodeDevisePaiement = "CDF",
                    Phone = "243900000000",
                    IdUtilisateur = idUtilisateur,
                    IdSociete = idSociete,
                    IdSite = idSite
                }
            };

        private static FlexPayReservationService CreateFlexPayService(CongoTravelDbContext ctx, bool enabled)
        {
            var tarifMock = new Mock<IVoyageTarifService>();
            tarifMock
                .Setup(t => t.ComputeTotalForSiegesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, IReadOnlyList<int> seats, int _, CancellationToken _) => seats.Count * 50000m);

            var flexApi = new Mock<IFlexPayService>();
            flexApi
                .Setup(f => f.InitierPaiementMobileMoneyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-TEST-001", Message = "OK" });

            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var resolution = new InfoPaiementResolutionService(
                ctx, NullLogger<InfoPaiementResolutionService>.Instance);
            var configSociete = ConfigSocieteTestHelper.Create(ctx);

            return new FlexPayReservationService(
                ctx,
                SiegeDisponibiliteTestHelper.Create(ctx),
                tarifMock.Object,
                flexApi.Object,
                httpAccessor.Object,
                Microsoft.Extensions.Options.Options.Create(new FlexPayOptions
                {
                    Enabled = enabled,
                    SeatHoldMinutes = 15,
                    CallbackBaseUrl = "https://test.example/api/FlexPay/callback"
                }),
                resolution,
                configSociete,
                new DeviseMontantConverter(ctx),
                CurrentUserTestHelper.MockClient(),
                NullLogger<FlexPayReservationService>.Instance);
        }

        private static FlexPayCallbackService CreateCallbackService(
            CongoTravelDbContext ctx,
            Mock<IFlexPayRealtimeNotifier>? realtimeMock = null,
            string? flexPayCheckStatus = null)
        {
            var flexApi = new Mock<IFlexPayService>();
            if (flexPayCheckStatus != null)
            {
                flexApi
                    .Setup(f => f.VerifierStatutTransactionAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new FlexPayCheckResponseDto
                    {
                        Code = flexPayCheckStatus,
                        Transaction = new FlexPayTransactionDto { Status = flexPayCheckStatus }
                    });
            }

            var qrMock = new Mock<IQrCodeService>();
            qrMock
                .Setup(q => q.GenerateUniqueQrCodeAsync(It.IsAny<int>(), It.IsAny<int?>()))
                .ReturnsAsync("QR-FLEXPAY-TEST");

            var billetRepo = BilletServiceTestHelper.Create(ctx);
            var billetEmission = new BilletEmissionService(
                billetRepo,
                qrMock.Object,
                ctx,
                ConfigSocieteTestHelper.Create(ctx),
                NullLogger<BilletEmissionService>.Instance);

            var tarifMock = new Mock<IVoyageTarifService>();
            tarifMock
                .Setup(t => t.ComputeTotalForSiegesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, IReadOnlyList<int> seats, int _, CancellationToken _) => seats.Count * 50000m);
            tarifMock
                .Setup(t => t.ResolvePrixAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(50000);

            var mapper = new MapperConfiguration(
                cfg =>
                {
                    cfg.AddProfile<WorkflowReservationMappingProfile>();
                    cfg.AddProfile<VehiculeMappingProfile>();
                },
                NullLoggerFactory.Instance).CreateMapper();

            var readService = new ReservationWithPaiementReadService(
                new ReservationService(ctx, ConfigSocieteTestHelper.Create(ctx), NullLogger<ReservationService>.Instance),
                billetRepo,
                new BilletPricingEnrichmentService(ctx, tarifMock.Object),
                ctx,
                mapper);

            realtimeMock ??= new Mock<IFlexPayRealtimeNotifier>();

            var resolution = new InfoPaiementResolutionService(
                ctx, NullLogger<InfoPaiementResolutionService>.Instance);

            return new FlexPayCallbackService(
                ctx,
                SiegeDisponibiliteTestHelper.Create(ctx),
                billetEmission,
                flexApi.Object,
                realtimeMock.Object,
                readService,
                Microsoft.Extensions.Options.Options.Create(new FlexPayOptions()),
                resolution,
                Mock.Of<IReversementAutomatiqueService>(),
                Mock.Of<IAllerRetourReservationService>(),
                NullLogger<FlexPayCallbackService>.Instance);
        }

        private static SyncService CreateSyncService(CongoTravelDbContext ctx)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.Setup(c => c.UserId).Returns(1);
            return new SyncService(
                ctx,
                new Mock<IWatermarkService>().Object,
                new Mock<ICursorService>().Object,
                currentUser.Object,
                NullLogger<SyncService>.Instance);
        }

        private static async Task<(int IdVoyage, int IdSiege, int IdCategorie, int IdSite)> SeedMinimalVoyageAsync(CongoTravelDbContext ctx)
        {
            var s = new Societe { Nom = "T", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var site = new Site
            {
                IdSociete = s.IdSociete,
                CodeSite = "GARE1",
                NomSite = "Gare test",
                Statut = true,
                IsSitePrincipal = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = s.IdSociete,
                IdSite = site.IdSite,
                CodeMarchand = "TEST_MERCHANT",
                ApiToken = "Bearer test-token",
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var cat = new CategorieSiege
            {
                IdSociete = s.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.CategorieSieges.Add(cat);

            var tv = new TypeVehicule { Libelle = "Std", IdSociete = s.IdSociete, Statut = true, DateCreation = DateTime.UtcNow };
            ctx.TypeVehicules.Add(tv);
            await ctx.SaveChangesAsync();

            var vh = new Vehicule
            {
                AliasVehicule = "V1",
                Marques = "M",
                IdTypeVehicule = tv.IdTypeVehicule,
                NombreSiege = 1,
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
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 50000,
                IdVehicule = vh.IdVehicule,
                IdDestination = dest.IdDestination,
                IdSociete = s.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow,
                CodeDevisePrix = "CDF"
            };
            ctx.Voyages.Add(voy);
            await ctx.SaveChangesAsync();

            ctx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = voy.Id,
                IdCategorieSiege = cat.IdCategorieSiege,
                Prix = 50000,
                IdSociete = s.IdSociete,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var siege = new Siege
            {
                IdVehicule = vh.IdVehicule,
                IdCategorieSiege = cat.IdCategorieSiege,
                CodeSiege = "V1/1",
                NumeroOrdre = 1,
                IdSociete = s.IdSociete,
                EstActif = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sieges.Add(siege);
            await ctx.SaveChangesAsync();

            return (voy.Id, siege.IdSiege, cat.IdCategorieSiege, site.IdSite);
        }

        [Fact]
        public async Task FlexPay_initiate_satellite_uses_principal_InfoPaiement_fallback()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_initiate_satellite_uses_principal_InfoPaiement_fallback)));
            var (idVoyage, _, idCat, idSitePrincipal) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var satellite = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "SAT1",
                NomSite = "Guichet satellite",
                Statut = true,
                IsSitePrincipal = false,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(satellite);
            await ctx.SaveChangesAsync();

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var dto = BuildFlexPayDto(
                idVoyage, societe.IdSociete, satellite.IdSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);

            var response = await flexPay.InitiateAsync(dto);

            Assert.Equal(TransactionStatut.EnAttente, response.Statut);
            Assert.True(response.FlexPayAccepted);
            Assert.Equal(satellite.IdSite, response.Paiement.IdSite);
        }

        [Fact]
        public async Task FlexPay_initiate_satellite_fails_when_no_principal_InfoPaiement()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_initiate_satellite_fails_when_no_principal_InfoPaiement)));
            var (idVoyage, _, idCat, _) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            ctx.InfoPaiementsSociete.RemoveRange(await ctx.InfoPaiementsSociete.ToListAsync());
            await ctx.SaveChangesAsync();

            var satellite = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "SAT",
                NomSite = "Satellite",
                Statut = true,
                IsSitePrincipal = false,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(satellite);
            await ctx.SaveChangesAsync();

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var dto = BuildFlexPayDto(
                idVoyage, societe.IdSociete, satellite.IdSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => flexPay.InitiateAsync(dto));
            Assert.Contains("Paiement electronique non configurer", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<Client> SeedClientAsync(CongoTravelDbContext ctx, int idSociete)
        {
            var c = new Client
            {
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                Statut = true,
                DateCreation = DateTime.UtcNow,
                IsActif = true
            };
            ctx.Clients.Add(c);
            await ctx.SaveChangesAsync();
            return c;
        }

        private static async Task<Utilisateur> SeedUserAsync(CongoTravelDbContext ctx, int idSociete)
        {
            var u = new Utilisateur
            {
                NomComplet = "Agent Test",
                Email = "t@test.local",
                MotDePasseHash = "hash",
                IdSociete = idSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Utilisateurs.Add(u);
            await ctx.SaveChangesAsync();
            return u;
        }

        [Fact]
        public async Task Initiate_includes_supplement_per_place_same_currency()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(Initiate_includes_supplement_per_place_same_currency)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            await ConfigSocieteTestHelper.SeedAsync(ctx, societe.IdSociete, c =>
            {
                c.MontAddPaieElectronique = 500m;
                c.CodeDeviseMontAddPaieElectronique = "CDF";
            });

            decimal? capturedAmount = null;
            var tarifMock = new Mock<IVoyageTarifService>();
            tarifMock
                .Setup(t => t.ComputeTotalForSiegesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(50000m);

            var flexApi = new Mock<IFlexPayService>();
            flexApi
                .Setup(f => f.InitierPaiementMobileMoneyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, decimal, string, string, CancellationToken>(
                    (_, _, _, _, amount, _, _, _) => capturedAmount = amount)
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-SUPP-1", Message = "OK" });

            var flexPay = CreateFlexPayServiceWithMocks(ctx, enabled: true, tarifMock, flexApi);
            var result = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50500m));

            Assert.NotNull(result.OrderNumberFlexPay);
            Assert.Equal(50500m, capturedAmount);
        }

        [Fact]
        public async Task Initiate_rejects_montantAPaye_missing_supplement()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(Initiate_rejects_montantAPaye_missing_supplement)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            await ConfigSocieteTestHelper.SeedAsync(ctx, societe.IdSociete, c =>
            {
                c.MontAddPaieElectronique = 500m;
                c.CodeDeviseMontAddPaieElectronique = "CDF";
            });

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                flexPay.InitiateAsync(
                    BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m)));

            Assert.Contains("supplément électronique", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Initiate_ignores_supplement_when_zero()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(Initiate_ignores_supplement_when_zero)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var result = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            Assert.NotNull(result.OrderNumberFlexPay);
        }

        [Fact]
        public async Task FlexPay_initiate_cross_currency_converts_tarif_amount()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_initiate_cross_currency_converts_tarif_amount)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);
            await SeedTauxAsync(ctx, societe.IdSociete, "CDF", "USD", 0.0004m);

            decimal? capturedAmount = null;
            var tarifMock = new Mock<IVoyageTarifService>();
            tarifMock
                .Setup(t => t.ComputeTotalForSiegesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(50000m);

            var flexApi = new Mock<IFlexPayService>();
            flexApi
                .Setup(f => f.InitierPaiementMobileMoneyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, string, decimal, string, string, CancellationToken>(
                    (_, _, _, _, amount, _, _, _) => capturedAmount = amount)
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-CROSS-1", Message = "OK" });

            var service = CreateFlexPayServiceWithMocks(ctx, enabled: true, tarifMock, flexApi);
            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);
            dto.Paiement.CodeDevisePaiement = "USD";

            var result = await service.InitiateAsync(dto);

            Assert.NotNull(result.OrderNumberFlexPay);
            Assert.Equal("USD", result.CodeDevisePaiement);
            Assert.Equal(20m, capturedAmount);
            Assert.Equal(50000m, result.MontantVoyage);
            Assert.Equal(20m, result.Paiement.MontantAPaye);
        }

        [Fact]
        public async Task FlexPay_initiate_cross_currency_fails_without_active_rate()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_initiate_cross_currency_fails_without_active_rate)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var service = CreateFlexPayService(ctx, enabled: true);
            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);
            dto.Paiement.CodeDevisePaiement = "USD";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitiateAsync(dto));
            Assert.Contains("Aucun taux actif", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FlexPay_initiate_rejects_currency_not_supported_by_mobile_money_channel()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_initiate_rejects_currency_not_supported_by_mobile_money_channel)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var tarifMock = new Mock<IVoyageTarifService>();
            tarifMock
                .Setup(t => t.ComputeTotalForSiegesAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, IReadOnlyList<int> seats, int _, CancellationToken _) => seats.Count * 50000m);

            var flexApi = new Mock<IFlexPayService>();
            flexApi
                .Setup(f => f.InitierPaiementMobileMoneyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-CHAN-1", Message = "OK" });

            var service = CreateFlexPayServiceWithMocks(
                ctx,
                enabled: true,
                tarifMock,
                flexApi,
                mobileCurrencies: new[] { "CDF" },
                cardCurrencies: new[] { "CDF", "USD" });

            var dto = BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m);
            dto.Paiement.CodeDevisePaiement = "USD";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.InitiateAsync(dto));
            Assert.Contains("n'autorise pas la devise USD", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task FlexPay_callback_rejects_when_currency_mismatches_expected()
        {
            await using var ctx = new CongoTravelDbContext(CreateDbOptions(nameof(FlexPay_callback_rejects_when_currency_mismatches_expected)));
            var (idVoyage, _, idCat, idSite) = await SeedMinimalVoyageAsync(ctx);
            var societe = await ctx.Societes.FirstAsync();
            var client = await SeedClientAsync(ctx, societe.IdSociete);
            var user = await SeedUserAsync(ctx, societe.IdSociete);

            var flexPay = CreateFlexPayService(ctx, enabled: true);
            var initiated = await flexPay.InitiateAsync(
                BuildFlexPayDto(idVoyage, societe.IdSociete, idSite, client.IdClient, user.IdUtilisateur, idCat, 50000m));

            var callbackSvc = CreateCallbackService(ctx);
            var cb = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = initiated.OrderNumberFlexPay,
                Reference = initiated.ReferenceFlexPay,
                Amount = "50000",
                Currency = "USD"
            };

            var result = await callbackSvc.ProcessCallbackAsync(cb, "{}", null, null);

            Assert.False(result.Success);
            Assert.Contains("devise callback", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await ctx.Reservations.CountAsync());
        }

        private static FlexPayReservationService CreateFlexPayServiceWithMocks(
            CongoTravelDbContext ctx,
            bool enabled,
            Mock<IVoyageTarifService> tarifMock,
            Mock<IFlexPayService> flexApi,
            IEnumerable<string>? mobileCurrencies = null,
            IEnumerable<string>? cardCurrencies = null)
        {
            var httpAccessor = new Mock<IHttpContextAccessor>();
            httpAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var resolution = new InfoPaiementResolutionService(
                ctx, NullLogger<InfoPaiementResolutionService>.Instance);

            return new FlexPayReservationService(
                ctx,
                SiegeDisponibiliteTestHelper.Create(ctx),
                tarifMock.Object,
                flexApi.Object,
                httpAccessor.Object,
                Microsoft.Extensions.Options.Options.Create(new FlexPayOptions
                {
                    Enabled = enabled,
                    SeatHoldMinutes = 15,
                    CallbackBaseUrl = "https://test.example/api/FlexPay/callback",
                    MobileMoneySupportedCurrencies = (mobileCurrencies ?? new[] { "CDF", "USD" }).ToList(),
                    CardSupportedCurrencies = (cardCurrencies ?? new[] { "CDF", "USD" }).ToList()
                }),
                resolution,
                ConfigSocieteTestHelper.Create(ctx),
                new DeviseMontantConverter(ctx),
                CurrentUserTestHelper.MockClient(),
                NullLogger<FlexPayReservationService>.Instance);
        }

        private static async Task SeedTauxAsync(
            CongoTravelDbContext ctx,
            int idSociete,
            string source,
            string cible,
            decimal taux)
        {
            ctx.TauxChanges.Add(new TauxChange
            {
                IdSociete = idSociete,
                CodeDeviseSource = source,
                CodeDeviseCible = cible,
                Taux = taux,
                DateEffet = DateTime.UtcNow.AddDays(-1),
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }
    }
}
