using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.ReversementSite;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class ReversementSiteTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static FlexPayOptions EnabledFlexPayOptions() => new()
        {
            Enabled = true,
            AutoReversementEnabled = true,
            CallbackBaseUrl = "https://api.example.com/api/FlexPay/callback",
            PayOutUrl = "https://backend.flexpay.cd/api/rest/v1/merchantPayOutService",
            PayOutPendingMinutes = 15
        };

        [Theory]
        [InlineData("243900000000", true)]
        [InlineData("243 900 000 000", true)]
        [InlineData("+243900000000", true)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        public void MobileMoneyPhoneHelper_validates_digits(string input, bool expectedValid)
        {
            var valid = MobileMoneyPhoneHelper.TryNormalize(input, out var normalized, out _);
            Assert.Equal(expectedValid, valid);
            if (expectedValid)
                Assert.Equal("243900000000", normalized);
        }

        [Fact]
        public void ResolvePayOutCallbackUrl_derives_payout_callback_from_base()
        {
            var url = FlexPayUrlHelper.ResolvePayOutCallbackUrl(
                null,
                "https://api.example.com/api/FlexPay/callback",
                forceProductionCallbackInDev: true);

            Assert.Equal("https://api.example.com/api/FlexPay/payout/callback", url);
        }

        [Fact]
        public async Task InitierAsync_rejects_site_without_NumeroMobileMoney()
        {
            await using var ctx = BuildDb(nameof(InitierAsync_rejects_site_without_NumeroMobileMoney));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, numeroMobileMoney: null);

            var service = CreateReversementSiteService(ctx, Mock.Of<IFlexPayService>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitierAsync(new InitierReversementSiteDto
                {
                    IdSite = idSite,
                    IdSociete = idSociete,
                    Montant = 1000,
                    CodeDevise = "CDF"
                }, idUtilisateur: 1));

            Assert.Contains("Mobile Money", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task InitierAsync_creates_pending_reversement_on_flexpay_success()
        {
            await using var ctx = BuildDb(nameof(InitierAsync_creates_pending_reversement_on_flexpay_success));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000001");

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    "243900000001",
                    5000m,
                    "CDF",
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto
                {
                    Code = "0",
                    Message = "Transaction envoyée avec succès.",
                    OrderNumber = "FP-PAYOUT-001"
                });

            var service = CreateReversementSiteService(ctx, flexPay.Object);
            var result = await service.InitierAsync(new InitierReversementSiteDto
            {
                IdSite = idSite,
                IdSociete = idSociete,
                Montant = 5000,
                CodeDevise = "CDF",
                Motif = "Test reversement"
            }, idUtilisateur: 42);

            Assert.Equal(StatutReversementSite.EnAttente, result.Statut);
            Assert.Equal("FP-PAYOUT-001", result.OrderNumber);
            Assert.Equal("243900000001", result.NumeroMobileMoney);
            Assert.Equal(42, result.IdUtilisateur);
            Assert.Single(await ctx.ReversementsSite.ToListAsync());
        }

        [Fact]
        public async Task InitierAsync_rejects_second_pending_within_window()
        {
            await using var ctx = BuildDb(nameof(InitierAsync_rejects_second_pending_within_window));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000002");

            ctx.ReversementsSite.Add(new ReversementSite
            {
                IdSite = idSite,
                IdSociete = idSociete,
                IdUtilisateur = 1,
                NumeroMobileMoney = "243900000002",
                Montant = 100,
                CodeDevise = "CDF",
                Reference = "REV-PENDING",
                OrderNumber = "FP-OLD",
                Statut = StatutReversementSite.EnAttente,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var service = CreateReversementSiteService(ctx, Mock.Of<IFlexPayService>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.InitierAsync(new InitierReversementSiteDto
                {
                    IdSite = idSite,
                    IdSociete = idSociete,
                    Montant = 200,
                    CodeDevise = "CDF"
                }, idUtilisateur: 1));

            Assert.Contains("déjà en attente", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PayOut_callback_success_updates_reversement_status()
        {
            await using var ctx = BuildDb(nameof(PayOut_callback_success_updates_reversement_status));
            var reversement = new ReversementSite
            {
                IdSite = 1,
                IdSociete = 1,
                IdUtilisateur = 1,
                NumeroMobileMoney = "243900000003",
                Montant = 100,
                CodeDevise = "CDF",
                Reference = "REV1ABC123",
                OrderNumber = "FP-CB-001",
                Statut = StatutReversementSite.EnAttente,
                DateCreation = DateTime.UtcNow
            };
            ctx.ReversementsSite.Add(reversement);
            await ctx.SaveChangesAsync();

            var callbackService = new FlexPayPayOutCallbackService(ctx, NullLogger<FlexPayPayOutCallbackService>.Instance);
            var result = await callbackService.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                Reference = "REV1ABC123",
                OrderNumber = "FP-CB-001",
                ProviderReference = "OP-123",
                Channel = "mpesa"
            }, "{}", null, null);

            Assert.True(result.Success);
            Assert.Equal(StatutReversementSite.Succes, result.Statut);

            var updated = await ctx.ReversementsSite.FirstAsync();
            Assert.Equal(StatutReversementSite.Succes, updated.Statut);
            Assert.Equal("OP-123", updated.ProviderReference);
            Assert.Equal("mpesa", updated.Channel);
            Assert.NotNull(updated.DateCallback);
            Assert.Single(await ctx.CallbacksFlexPay.ToListAsync());
        }

        [Fact]
        public async Task PayOut_callback_failure_does_not_touch_reservations()
        {
            await using var ctx = BuildDb(nameof(PayOut_callback_failure_does_not_touch_reservations));
            ctx.ReversementsSite.Add(new ReversementSite
            {
                IdSite = 1,
                IdSociete = 1,
                IdUtilisateur = 1,
                NumeroMobileMoney = "243900000004",
                Montant = 50,
                CodeDevise = "USD",
                Reference = "REV2FAIL",
                OrderNumber = "FP-CB-FAIL",
                Statut = StatutReversementSite.EnAttente,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var callbackService = new FlexPayPayOutCallbackService(ctx, NullLogger<FlexPayPayOutCallbackService>.Instance);
            var result = await callbackService.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "1",
                Reference = "REV2FAIL",
                OrderNumber = "FP-CB-FAIL"
            }, "{}", null, null);

            Assert.False(result.Success);
            Assert.Equal(StatutReversementSite.Echec, result.Statut);
            Assert.Equal(0, await ctx.Reservations.CountAsync());
        }

        [Fact]
        public async Task PayOut_callback_is_idempotent_when_already_finalized()
        {
            await using var ctx = BuildDb(nameof(PayOut_callback_is_idempotent_when_already_finalized));
            ctx.ReversementsSite.Add(new ReversementSite
            {
                IdSite = 1,
                IdSociete = 1,
                IdUtilisateur = 1,
                NumeroMobileMoney = "243900000005",
                Montant = 10,
                CodeDevise = "CDF",
                Reference = "REV3DONE",
                OrderNumber = "FP-CB-DONE",
                Statut = StatutReversementSite.Succes,
                DateCreation = DateTime.UtcNow,
                DateCallback = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var callbackService = new FlexPayPayOutCallbackService(ctx, NullLogger<FlexPayPayOutCallbackService>.Instance);
            var result = await callbackService.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                Reference = "REV3DONE",
                OrderNumber = "FP-CB-DONE"
            }, "{}", null, null);

            Assert.True(result.AlreadyProcessed);
            Assert.Equal(StatutReversementSite.Succes, result.Statut);
        }

        [Fact]
        public async Task InitierPourPaiementAsync_is_idempotent_by_IdPaiement()
        {
            await using var ctx = BuildDb(nameof(InitierPourPaiementAsync_is_idempotent_by_IdPaiement));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000010");

            ctx.ReversementsSite.Add(new ReversementSite
            {
                IdPaiement = 99,
                IdReservation = 50,
                IdSite = idSite,
                IdSociete = idSociete,
                IdUtilisateur = 1,
                Origine = ReversementSiteOrigines.PaiementElectronique,
                NumeroMobileMoney = "243900000010",
                Montant = 100,
                CodeDevise = "CDF",
                Reference = "REV-EXIST",
                OrderNumber = "FP-EXIST",
                Statut = StatutReversementSite.EnAttente,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var service = CreateReversementSiteService(ctx, Mock.Of<IFlexPayService>());
            var result = await service.InitierPourPaiementAsync(
                99, 50, idSite, idSociete, 1, 200, "CDF", "test", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("FP-EXIST", result!.OrderNumber);
            Assert.Equal(1, await ctx.ReversementsSite.CountAsync());
        }

        [Fact]
        public async Task ReversementAutomatiqueService_skips_when_societe_flag_off()
        {
            await using var ctx = BuildDb(nameof(ReversementAutomatiqueService_skips_when_societe_flag_off));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000011");

            var paiement = new Paiement
            {
                IdPaiement = 1,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 1,
                MontantPaye = 50,
                CodeDevisePaiement = "CDF",
                Statut = true
            };
            var reservation = new Reservation
            {
                IdReservation = 10,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 1,
                IdVoyage = 1,
                IdClient = 1,
                NombreDePlace = 1,
                Statut = true
            };

            var svc = CreateReversementAutomatiqueService(ctx, Mock.Of<IFlexPayService>());

            var triggered = await svc.TryDeclencherApresPaiementElectroniqueAsync(paiement, reservation);
            Assert.False(triggered);
            Assert.Empty(await ctx.ReversementsSite.ToListAsync());
        }

        [Fact]
        public async Task ReversementAutomatiqueService_skips_when_montant_brut_zero()
        {
            await using var ctx = BuildDb(nameof(ReversementAutomatiqueService_skips_when_montant_brut_zero));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000012");
            await EnableAutoReversementAsync(ctx, idSociete);

            var paiement = new Paiement
            {
                IdPaiement = 2,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 1,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 0
            };
            var reservation = new Reservation { IdReservation = 11, IdSociete = idSociete, IdSite = idSite, IdUtilisateur = 1 };

            var svc = CreateReversementAutomatiqueService(ctx, Mock.Of<IFlexPayService>());
            var triggered = await svc.TryDeclencherApresPaiementElectroniqueAsync(paiement, reservation);

            Assert.False(triggered);
            Assert.Empty(await ctx.ReversementsSite.ToListAsync());
        }

        [Fact]
        public async Task ReversementAutomatiqueService_initiates_payout_when_enabled_and_amount_resolves()
        {
            await using var ctx = BuildDb(nameof(ReversementAutomatiqueService_initiates_payout_when_enabled_and_amount_resolves));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000013");
            await EnableAutoReversementAsync(ctx, idSociete);

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243900000013",
                    75m, "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-AUTO-1" });

            var paiement = new Paiement
            {
                IdPaiement = 3,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 5,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 75,
                CodeDevisePaiement = "USD",
                Statut = true
            };
            var reservation = new Reservation
            {
                IdReservation = 12,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 5
            };

            var svc = CreateReversementAutomatiqueService(ctx, flexPay.Object);
            var triggered = await svc.TryDeclencherApresPaiementElectroniqueAsync(paiement, reservation);

            Assert.True(triggered);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(3, rev.IdPaiement);
            Assert.Equal(ReversementModulePaiement.Transport, rev.ModulePaiement);
            Assert.Equal(3, rev.IdPaiementSource);
            Assert.Equal(ReversementSiteOrigines.PaiementElectronique, rev.Origine);
            Assert.Equal("FP-AUTO-1", rev.OrderNumber);
        }

        [Fact]
        public async Task InitierPourPaiementAsync_allows_same_source_id_across_modules()
        {
            await using var ctx = BuildDb(nameof(InitierPourPaiementAsync_allows_same_source_id_across_modules));
            var (idSociete, idSite) = await SeedSiteAsync(ctx, "243900000020");

            var flexPay = new Mock<IFlexPayService>();
            flexPay.SetupSequence(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-MULTI-TRANSPORT" })
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-MULTI-EVENEMENT" });

            var service = CreateReversementSiteService(ctx, flexPay.Object);

            var transport = await service.InitierPourPaiementAsync(
                ReversementModulePaiement.Transport, 5, 10,
                idSite, idSociete, 1, 1000m, "CDF", "transport",
                idPaiementTransport: 5, idReservationTransport: 10);

            var evenement = await service.InitierPourPaiementAsync(
                ReversementModulePaiement.Evenement, 5, 20,
                idSite, idSociete, 1, 2000m, "CDF", "evenement");

            Assert.NotNull(transport);
            Assert.NotNull(evenement);
            Assert.NotEqual(transport!.IdReversementSite, evenement!.IdReversementSite);
            Assert.Equal(2, await ctx.ReversementsSite.CountAsync());
            Assert.Equal(ReversementModulePaiement.Transport,
                await ctx.ReversementsSite.Where(r => r.IdReversementSite == transport.IdReversementSite)
                    .Select(r => r.ModulePaiement).SingleAsync());
            Assert.Equal(ReversementModulePaiement.Evenement,
                await ctx.ReversementsSite.Where(r => r.IdReversementSite == evenement.IdReversementSite)
                    .Select(r => r.ModulePaiement).SingleAsync());
        }

        [Fact]
        public async Task Calculator_null_CodeDeviseFraisPlateforme_uses_payment_currency()
        {
            var converter = new Mock<IDeviseMontantConverter>();
            var result = await ReversementMontantCalculator.ComputeAsync(
                10000m,
                "CDF",
                DateTime.UtcNow,
                1,
                new ConfigSociete
                {
                    PourcentageReversementSite = 100m,
                    FraisPlateforme = 500m,
                    CodeDeviseFraisPlateforme = null
                },
                converter.Object);

            Assert.NotNull(result);
            Assert.Equal(9500m, result!.Montant);
            converter.Verify(
                c => c.ConvertAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Calculator_converts_fee_usd_to_cdf()
        {
            var converter = new Mock<IDeviseMontantConverter>();
            converter
                .Setup(c => c.ConvertAsync(1, 1m, "USD", "CDF", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((2850m, 2850m));

            var result = await ReversementMontantCalculator.ComputeAsync(
                10000m,
                "CDF",
                DateTime.UtcNow,
                1,
                new ConfigSociete
                {
                    PourcentageReversementSite = 100m,
                    FraisPlateforme = 1m,
                    CodeDeviseFraisPlateforme = "USD"
                },
                converter.Object);

            Assert.NotNull(result);
            Assert.Equal(7150m, result!.Montant);
            Assert.Equal("CDF", result.CodeDevise);
        }

        private static async Task EnableAutoReversementAsync(CongoTravelDbContext ctx, int idSociete)
        {
            ctx.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = idSociete,
                AutoReversementPaiementElectronique = true,
                PourcentageReversementSite = 100m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        private static ReversementAutomatiqueService CreateReversementAutomatiqueService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService) =>
            new(
                new ConfigSocieteService(ctx),
                new DeviseMontantConverter(ctx),
                CreateReversementSiteService(ctx, flexPayService),
                Options.Create(EnabledFlexPayOptions()),
                NullLogger<ReversementAutomatiqueService>.Instance);

        private static PaiementElectroniqueReversementMontantResolver CreateResolver(
            IDeviseMontantConverter? converter = null) =>
            new(
                converter ?? new DeviseMontantConverter(BuildDb(nameof(CreateResolver))),
                NullLogger<PaiementElectroniqueReversementMontantResolver>.Instance);

        [Fact]
        public async Task Resolver_returns_null_for_cash_payment()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete { PourcentageReversementSite = 100m };
            var paiement = new Paiement
            {
                IdPaiement = 1,
                MethodePaiement = "CASH",
                MontantPaye = 1000,
                CodeDevisePaiement = "CDF"
            };

            Assert.Null(await resolver.ResolveAsync(paiement, new Reservation(), config));
        }

        [Fact]
        public async Task Resolver_applies_100_percent()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete { PourcentageReversementSite = 100m };
            var paiement = new Paiement
            {
                IdPaiement = 2,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 25.50m,
                CodeDevisePaiement = "USD"
            };

            var result = await resolver.ResolveAsync(paiement, new Reservation(), config);
            Assert.NotNull(result);
            Assert.Equal(25.50m, result!.Montant);
            Assert.Equal("USD", result.CodeDevise);
        }

        [Fact]
        public async Task Resolver_applies_configured_percent_cdf_rounds_integer()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete { PourcentageReversementSite = 95m };
            var paiement = new Paiement
            {
                IdPaiement = 3,
                MethodePaiement = "CARTE_BANCAIRE",
                MontantPaye = 150000m,
                CodeDevisePaiement = "CDF"
            };

            var result = await resolver.ResolveAsync(paiement, new Reservation(), config);
            Assert.NotNull(result);
            Assert.Equal(142500m, result!.Montant);
            Assert.Equal("CDF", result.CodeDevise);
        }

        [Fact]
        public async Task Resolver_returns_null_when_percent_zero()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete { PourcentageReversementSite = 0m };
            var paiement = new Paiement
            {
                IdPaiement = 4,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 1000m,
                CodeDevisePaiement = "CDF"
            };

            Assert.Null(await resolver.ResolveAsync(paiement, new Reservation(), config));
        }

        [Fact]
        public async Task ResolveAsync_subtracts_fixed_fee_same_currency()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete
            {
                PourcentageReversementSite = 100m,
                FraisPlateforme = 500m,
                CodeDeviseFraisPlateforme = "CDF"
            };
            var paiement = new Paiement
            {
                IdPaiement = 5,
                IdSociete = 1,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 150000m,
                CodeDevisePaiement = "CDF"
            };

            var result = await resolver.ResolveAsync(paiement, new Reservation(), config);
            Assert.NotNull(result);
            Assert.Equal(149500m, result!.Montant);
            Assert.Contains("frais plateforme", result.Motif);
        }

        [Fact]
        public async Task ResolveAsync_ignores_fee_when_zero()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete
            {
                PourcentageReversementSite = 100m,
                FraisPlateforme = 0m
            };
            var paiement = new Paiement
            {
                IdPaiement = 6,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 150000m,
                CodeDevisePaiement = "CDF"
            };

            var result = await resolver.ResolveAsync(paiement, new Reservation(), config);
            Assert.NotNull(result);
            Assert.Equal(150000m, result!.Montant);
            Assert.DoesNotContain("frais plateforme", result.Motif ?? string.Empty);
        }

        [Fact]
        public async Task ResolveAsync_returns_null_when_fee_exceeds_percent_part()
        {
            var resolver = CreateResolver();
            var config = new ConfigSociete
            {
                PourcentageReversementSite = 100m,
                FraisPlateforme = 600m,
                CodeDeviseFraisPlateforme = "CDF"
            };
            var paiement = new Paiement
            {
                IdPaiement = 7,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 500m,
                CodeDevisePaiement = "CDF"
            };

            Assert.Null(await resolver.ResolveAsync(paiement, new Reservation(), config));
        }

        [Fact]
        public async Task ResolveAsync_returns_null_when_conversion_fails()
        {
            var converter = new Mock<IDeviseMontantConverter>();
            converter
                .Setup(c => c.ConvertAsync(
                    It.IsAny<int>(), It.IsAny<decimal>(), "CDF", "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Aucun taux actif"));

            var resolver = CreateResolver(converter.Object);
            var config = new ConfigSociete
            {
                PourcentageReversementSite = 100m,
                FraisPlateforme = 1500m,
                CodeDeviseFraisPlateforme = "CDF"
            };
            var paiement = new Paiement
            {
                IdPaiement = 8,
                IdSociete = 1,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 25.50m,
                CodeDevisePaiement = "USD"
            };

            Assert.Null(await resolver.ResolveAsync(paiement, new Reservation(), config));
        }

        [Fact]
        public async Task ResolveAsync_subtracts_converted_fee_cdf_to_usd()
        {
            var converter = new Mock<IDeviseMontantConverter>();
            converter
                .Setup(c => c.ConvertAsync(1, 1500m, "CDF", "USD", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((0.50m, 0.00033333m));

            var resolver = CreateResolver(converter.Object);
            var config = new ConfigSociete
            {
                PourcentageReversementSite = 100m,
                FraisPlateforme = 1500m,
                CodeDeviseFraisPlateforme = "CDF"
            };
            var paiement = new Paiement
            {
                IdPaiement = 9,
                IdSociete = 1,
                MethodePaiement = "MOBILE_MONEY",
                MontantPaye = 25.50m,
                CodeDevisePaiement = "USD"
            };

            var result = await resolver.ResolveAsync(paiement, new Reservation(), config);
            Assert.NotNull(result);
            Assert.Equal(25.00m, result!.Montant);
            Assert.Equal("USD", result.CodeDevise);
        }

        private static ReversementSiteService CreateReversementSiteService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService)
        {
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

            return new ReversementSiteService(
                ctx,
                flexPayService,
                new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance),
                httpContextAccessor.Object,
                Options.Create(EnabledFlexPayOptions()),
                NullLogger<ReversementSiteService>.Instance);
        }

        private static async Task<(int IdSociete, int IdSite)> SeedSiteAsync(
            CongoTravelDbContext ctx,
            string? numeroMobileMoney)
        {
            var societe = new Societe { Nom = "Soc PayOut", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var site = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "PO1",
                NomSite = "Guichet",
                Statut = true,
                IsSitePrincipal = true,
                NumeroMobileMoney = numeroMobileMoney,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
                CodeMarchand = "MERCH-PO",
                ApiToken = "token-po",
                Statut = true,
                ActifMobileMoney = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, site.IdSite);
        }
    }
}
