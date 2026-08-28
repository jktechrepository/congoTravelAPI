using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class SatelliteReversementTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static FlexPayOptions EnabledOptions() => new()
        {
            Enabled = true,
            AutoReversementEnabled = true,
            CallbackBaseUrl = "https://api.example.com/api/FlexPay/callback",
            PayOutUrl = "https://backend.flexpay.cd/api/rest/v1/merchantPayOutService",
            PayOutPendingMinutes = 15
        };

        [Fact]
        public async Task TryDeclencherAsync_evenement_applies_frais_plateforme_same_currency()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_evenement_applies_frais_plateforme_same_currency));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, "243911100001", frais: 500m);

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100001",
                    14500m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-REV-1" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new EvenementPayment
            {
                IdEvenementPayment = 11,
                IdEvenementReservation = 21,
                IdSite = idSite,
                Provider = EvenementFlexPayConstants.Provider,
                Status = EvenementPaymentStatus.SUCCEEDED,
                Montant = 15000m,
                CodeDevise = "CDF",
                MontantTarif = 15000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-EV"
            };
            var reservation = new EvenementReservation
            {
                IdEvenementReservation = 21,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 7
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.Evenement, rev.ModulePaiement);
            Assert.Equal(11, rev.IdPaiementSource);
            Assert.Null(rev.IdPaiement);
            Assert.Equal(14500m, rev.Montant);
            Assert.Equal("FP-EV-REV-1", rev.OrderNumber);
        }

        [Fact]
        public async Task TryDeclencherAsync_evenement_converts_frais_cdf_to_usd()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_evenement_converts_frais_cdf_to_usd));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(
                ctx, "243911100002", frais: 1500m, deviseFrais: "CDF");

            ctx.TauxChanges.Add(new TauxChange
            {
                IdSociete = idSociete,
                CodeDeviseSource = "CDF",
                CodeDeviseCible = "USD",
                Taux = 0.00033333m,
                Statut = true,
                DateEffet = DateTime.UtcNow.AddDays(-1),
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100002",
                    It.IsAny<decimal>(), "USD", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-USD" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new EvenementPayment
            {
                IdEvenementPayment = 12,
                IdEvenementReservation = 22,
                IdSite = idSite,
                Provider = EvenementFlexPayConstants.Provider,
                Status = EvenementPaymentStatus.SUCCEEDED,
                Montant = 25.50m,
                CodeDevise = "USD",
                MontantTarif = 25.50m,
                CodeDeviseTarif = "USD",
                ReferencePaiement = "REF-EV-USD"
            };
            var reservation = new EvenementReservation
            {
                IdEvenementReservation = 22,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 8
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal("USD", rev.CodeDevise);
            Assert.Equal(25.00m, rev.Montant);
        }

        [Fact]
        public async Task TryDeclencherAsync_evenement_payouts_to_organizer_mobile_money_when_set()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_evenement_payouts_to_organizer_mobile_money_when_set));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, "243911100001");

            const string organizerPhone = "243812345678";
            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeSession = "ORG-MM",
                Libelle = "Concert organisateur",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                NumeroMobileMoneyOrganisateur = organizerPhone,
                AutoReversementOrganisateur = true,
                VenteEnLigneActive = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), organizerPhone,
                    15000m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-ORG" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new EvenementPayment
            {
                IdEvenementPayment = 13,
                IdEvenementReservation = 23,
                IdSite = idSite,
                Provider = EvenementFlexPayConstants.Provider,
                Status = EvenementPaymentStatus.SUCCEEDED,
                Montant = 15000m,
                CodeDevise = "CDF",
                MontantTarif = 15000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-EV-ORG"
            };
            var reservation = new EvenementReservation
            {
                IdEvenementReservation = 23,
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 7
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation, session));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(organizerPhone, rev.NumeroMobileMoney);
            flexPay.Verify(f => f.InitierPayOutAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), organizerPhone,
                15000m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task TryDeclencherAsync_evenement_falls_back_to_site_mobile_money_when_organizer_empty()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_evenement_falls_back_to_site_mobile_money_when_organizer_empty));
            const string sitePhone = "243911100010";
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, sitePhone);

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeSession = "ORG-FB",
                Libelle = "Fallback site",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                NumeroMobileMoneyOrganisateur = null,
                AutoReversementOrganisateur = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), sitePhone,
                    8000m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-FB" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new EvenementPayment
            {
                IdEvenementPayment = 14,
                IdEvenementReservation = 24,
                IdSite = idSite,
                Provider = EvenementFlexPayConstants.Provider,
                Status = EvenementPaymentStatus.SUCCEEDED,
                Montant = 8000m,
                CodeDevise = "CDF",
                MontantTarif = 8000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-EV-FB"
            };
            var reservation = new EvenementReservation
            {
                IdEvenementReservation = 24,
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 7
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation, session));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(sitePhone, rev.NumeroMobileMoney);
        }

        [Fact]
        public async Task TryDeclencherAsync_evenement_skips_when_auto_reversement_organisateur_disabled()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_evenement_skips_when_auto_reversement_organisateur_disabled));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, "243911100011");

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeSession = "ORG-OFF",
                Libelle = "Pas de reversement session",
                StartAtUtc = DateTime.UtcNow.AddDays(1),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                NumeroMobileMoneyOrganisateur = "243812345678",
                AutoReversementOrganisateur = false,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var flexPay = new Mock<IFlexPayService>();
            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new EvenementPayment
            {
                IdEvenementPayment = 15,
                IdEvenementReservation = 25,
                IdSite = idSite,
                Provider = EvenementFlexPayConstants.Provider,
                Status = EvenementPaymentStatus.SUCCEEDED,
                Montant = 5000m,
                CodeDevise = "CDF",
                MontantTarif = 5000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-EV-OFF"
            };
            var reservation = new EvenementReservation
            {
                IdEvenementReservation = 25,
                IdEvenementSession = session.IdEvenementSession,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 7
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromEvenement(payment, reservation, session));

            Assert.False(ok);
            Assert.False(await ctx.ReversementsSite.AnyAsync());
            flexPay.Verify(
                f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Evenement_callback_payouts_to_organizer_mobile_money()
        {
            await using var ctx = BuildDb(nameof(Evenement_callback_payouts_to_organizer_mobile_money));
            var (idSociete, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            const string sitePhone = "243911100012";
            const string organizerPhone = "243899988877";
            await EnableSitePayOutAsync(ctx, idSociete, sitePhone);

            var reservation = await ctx.EvenementReservations
                .Include(r => r.Session)
                .FirstAsync(r => r.IdEvenementReservation == idReservation);
            reservation.Session!.NumeroMobileMoneyOrganisateur = organizerPhone;
            reservation.Session.AutoReversementOrganisateur = true;
            await ctx.SaveChangesAsync();

            var payment = await ctx.EvenementPayments.SingleAsync();
            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), organizerPhone,
                    payment.Montant, payment.CodeDevise, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-CB-ORG" });

            var auto = CreateAutoService(ctx, flexPay.Object);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                reversementAutomatiqueService: auto);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = payment.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Currency = payment.CodeDevise
            });

            Assert.True(result.Success);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(organizerPhone, rev.NumeroMobileMoney);
        }

        [Fact]
        public async Task TryDeclencherAsync_restaurant_initiates_payout()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_restaurant_initiates_payout));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, "243911100003");

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100003",
                    8000m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-RST-REV" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new RestaurantPayment
            {
                IdRestaurantPayment = 31,
                IdRestaurantReservation = 41,
                IdSite = idSite,
                Provider = RestaurantFlexPayConstants.Provider,
                Status = RestaurantPaymentStatus.SUCCEEDED,
                Montant = 8000m,
                CodeDevise = "CDF",
                MontantTarif = 8000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-RST"
            };
            var reservation = new RestaurantReservation
            {
                IdRestaurantReservation = 41,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 3
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromRestaurant(payment, reservation));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.Restaurant, rev.ModulePaiement);
            Assert.Equal(31, rev.IdPaiementSource);
        }

        [Fact]
        public async Task TryDeclencherAsync_site_touristique_initiates_payout()
        {
            await using var ctx = BuildDb(nameof(TryDeclencherAsync_site_touristique_initiates_payout));
            var (idSociete, idSite) = await SeedSiteWithAutoReversementAsync(ctx, "243911100004");

            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100004",
                    5000m, "CDF", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-ST-REV" });

            var svc = CreateAutoService(ctx, flexPay.Object);
            var payment = new SiteTouristiquePayment
            {
                IdSiteTouristiquePayment = 51,
                IdSiteTouristiqueReservation = 61,
                IdSite = idSite,
                Provider = SiteTouristiqueFlexPayConstants.Provider,
                Status = SiteTouristiquePaymentStatus.SUCCEEDED,
                Montant = 5000m,
                CodeDevise = "CDF",
                MontantTarif = 5000m,
                CodeDeviseTarif = "CDF",
                ReferencePaiement = "REF-ST"
            };
            var reservation = new SiteTouristiqueReservation
            {
                IdSiteTouristiqueReservation = 61,
                IdSociete = idSociete,
                IdSite = idSite,
                IdUtilisateur = 4
            };

            var ok = await svc.TryDeclencherAsync(
                ReversementAutomatiqueContext.FromSiteTouristique(payment, reservation));

            Assert.True(ok);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.SiteTouristique, rev.ModulePaiement);
            Assert.Equal(51, rev.IdPaiementSource);
        }

        [Fact]
        public async Task Evenement_callback_triggers_reversement_after_confirm()
        {
            await using var ctx = BuildDb(nameof(Evenement_callback_triggers_reversement_after_confirm));
            var (idSociete, idReservation, orderNumber) =
                await EvenementTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            await EnableSitePayOutAsync(ctx, idSociete, "243911100005");

            var payment = await ctx.EvenementPayments.SingleAsync();
            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100005",
                    payment.Montant, payment.CodeDevise, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-EV-CB-REV" });

            var auto = CreateAutoService(ctx, flexPay.Object);
            var service = EvenementTestFactories.CreateCallbackService(
                ctx,
                reversementAutomatiqueService: auto);

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = payment.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Currency = payment.CodeDevise
            });

            Assert.True(result.Success);
            Assert.Equal(EvenementReservationStatus.CONFIRMED,
                await ctx.EvenementReservations.Select(r => r.Status).SingleAsync());
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.Evenement, rev.ModulePaiement);
            Assert.Equal(payment.IdEvenementPayment, rev.IdPaiementSource);
            Assert.Equal("FP-EV-CB-REV", rev.OrderNumber);
        }

        [Fact]
        public async Task Restaurant_callback_triggers_reversement_after_confirm()
        {
            await using var ctx = BuildDb(nameof(Restaurant_callback_triggers_reversement_after_confirm));
            var (idSociete, _, orderNumber) =
                await RestaurantTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            await EnableSitePayOutAsync(ctx, idSociete, "243911100006");

            var payment = await ctx.RestaurantPayments.SingleAsync();
            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100006",
                    payment.Montant, payment.CodeDevise, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-RST-CB-REV" });

            var service = RestaurantTestFactories.CreateCallbackService(
                ctx,
                reversementAutomatiqueService: CreateAutoService(ctx, flexPay.Object));

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = payment.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Currency = payment.CodeDevise
            });

            Assert.True(result.Success);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.Restaurant, rev.ModulePaiement);
            Assert.Equal(payment.IdRestaurantPayment, rev.IdPaiementSource);
        }

        [Fact]
        public async Task SiteTouristique_callback_triggers_reversement_after_confirm()
        {
            await using var ctx = BuildDb(nameof(SiteTouristique_callback_triggers_reversement_after_confirm));
            var (idSociete, _, orderNumber) =
                await SiteTouristiqueTestFactories.SeedPendingFlexPayPaymentAsync(ctx, quantity: 1);

            await EnableSitePayOutAsync(ctx, idSociete, "243911100007");

            var payment = await ctx.SiteTouristiquePayments.SingleAsync();
            var flexPay = new Mock<IFlexPayService>();
            flexPay.Setup(f => f.InitierPayOutAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), "243911100007",
                    payment.Montant, payment.CodeDevise, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FlexPayPaymentResponseDto { Code = "0", OrderNumber = "FP-ST-CB-REV" });

            var service = SiteTouristiqueTestFactories.CreateCallbackService(
                ctx,
                reversementAutomatiqueService: CreateAutoService(ctx, flexPay.Object));

            var result = await service.ProcessCallbackAsync(new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = orderNumber,
                Amount = payment.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Currency = payment.CodeDevise
            });

            Assert.True(result.Success);
            var rev = await ctx.ReversementsSite.SingleAsync();
            Assert.Equal(ReversementModulePaiement.SiteTouristique, rev.ModulePaiement);
            Assert.Equal(payment.IdSiteTouristiquePayment, rev.IdPaiementSource);
        }

        private static ReversementAutomatiqueService CreateAutoService(
            CongoTravelDbContext ctx,
            IFlexPayService flexPayService)
        {
            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

            var siteService = new ReversementSiteService(
                ctx,
                flexPayService,
                new InfoPaiementResolutionService(ctx, NullLogger<InfoPaiementResolutionService>.Instance),
                httpContextAccessor.Object,
                Options.Create(EnabledOptions()),
                NullLogger<ReversementSiteService>.Instance);

            return new ReversementAutomatiqueService(
                new ConfigSocieteService(ctx),
                new DeviseMontantConverter(ctx),
                siteService,
                Options.Create(EnabledOptions()),
                NullLogger<ReversementAutomatiqueService>.Instance);
        }

        private static async Task<(int IdSociete, int IdSite)> SeedSiteWithAutoReversementAsync(
            CongoTravelDbContext ctx,
            string phone,
            decimal frais = 0m,
            string? deviseFrais = null)
        {
            var societe = new Societe { Nom = "Soc Satellite", Statut = true, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var site = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "SAT1",
                NomSite = "Site Satellite",
                Statut = true,
                IsSitePrincipal = true,
                NumeroMobileMoney = phone,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
                CodeMarchand = "MERCH-SAT",
                ApiToken = "token-sat",
                Statut = true,
                ActifMobileMoney = true,
                DateCreation = DateTime.UtcNow
            });

            ctx.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = societe.IdSociete,
                AutoReversementPaiementElectronique = true,
                PourcentageReversementSite = 100m,
                FraisPlateforme = frais,
                CodeDeviseFraisPlateforme = deviseFrais,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, site.IdSite);
        }

        private static async Task EnableSitePayOutAsync(
            CongoTravelDbContext ctx,
            int idSociete,
            string phone)
        {
            var site = await ctx.Sites.FirstAsync(s => s.IdSociete == idSociete);
            site.NumeroMobileMoney = phone;

            var config = await ctx.ConfigSocietes.FirstOrDefaultAsync(c => c.IdSociete == idSociete);
            if (config == null)
            {
                ctx.ConfigSocietes.Add(new ConfigSociete
                {
                    IdSociete = idSociete,
                    AutoReversementPaiementElectronique = true,
                    PourcentageReversementSite = 100m,
                    DateCreation = DateTime.UtcNow
                });
            }
            else
            {
                config.AutoReversementPaiementElectronique = true;
                config.PourcentageReversementSite = 100m;
            }

            if (!await ctx.InfoPaiementsSociete.AnyAsync(i => i.IdSociete == idSociete && i.Statut))
            {
                ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
                {
                    IdSociete = idSociete,
                    IdSite = site.IdSite,
                    CodeMarchand = "MERCH-SAT-CB",
                    ApiToken = "token-sat-cb",
                    Statut = true,
                    ActifMobileMoney = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            await ctx.SaveChangesAsync();
        }
    }
}
