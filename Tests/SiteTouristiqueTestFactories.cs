using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.SiteTouristique.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Tests
{
    internal static class SiteTouristiqueTestFactories
    {
        public static SiteTouristiqueInventoryConfirmStrategyFactory CreateConfirmStrategyFactory(CongoTravelDbContext ctx) =>
            new(
                new SiteTouristiqueGlobalQuotaConfirmStrategy(ctx),
                new SiteTouristiqueClassQuotaConfirmStrategy(ctx));

        public static SiteTouristiqueReservationConfirmationService CreateConfirmationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmStrategyFactory(ctx),
                NullLogger<SiteTouristiqueReservationConfirmationService>.Instance);

        public static SiteTouristiquePaymentService CreatePaymentService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                CreateConfirmationService(ctx),
                NullLogger<SiteTouristiquePaymentService>.Instance);

        public static SiteTouristiqueHoldService CreateHoldService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new SiteTouristiqueInventoryHoldStrategyFactory(
                    new SiteTouristiqueGlobalQuotaHoldStrategy(ctx),
                    new SiteTouristiqueClassQuotaHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                NullLogger<SiteTouristiqueHoldService>.Instance);

        public static SiteTouristiqueReservationService CreateReservationService(CongoTravelDbContext ctx) =>
            new(
                ctx,
                new SiteTouristiqueInventoryCancelStrategyFactory(
                    new SiteTouristiqueGlobalQuotaCancelStrategy(ctx),
                    new SiteTouristiqueClassQuotaCancelStrategy(ctx)),
                Mock.Of<IFlexPayRealtimeNotifier>(),
                NullLogger<SiteTouristiqueReservationService>.Instance);

        public static SiteTouristiqueLieuPhotoService CreateLieuPhotoService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<SiteTouristiqueLieuPhotoService>.Instance);

        public static SiteTouristiqueLieuService CreateLieuService(CongoTravelDbContext ctx) =>
            new(ctx, CreateLieuPhotoService(ctx), NullLogger<SiteTouristiqueLieuService>.Instance);

        public static Site CreateSiteEntity(
            int idSociete,
            string nomSite = "Site Test",
            bool isPrincipal = true,
            string codeSite = "ST01") =>
            new()
            {
                IdSociete = idSociete,
                CodeSite = codeSite,
                NomSite = nomSite,
                NomResponsableSite = "Responsable",
                Genre = "Masculin",
                Statut = true,
                IsSitePrincipal = isPrincipal,
                DateCreation = DateTime.UtcNow
            };

        public static async Task<(int IdSociete, int IdSite)> SeedSocieteWithSiteAsync(
            CongoTravelDbContext ctx,
            string nomSociete = "ST Societe")
        {
            var societe = new Societe { Nom = nomSociete, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var site = CreateSiteEntity(societe.IdSociete);
            ctx.Sites.Add(site);
            await ctx.SaveChangesAsync();

            ctx.ConfigSocietes.Add(ConfigSocieteDefaults.CreateForSociete(societe.IdSociete));
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, site.IdSite);
        }

        public static async Task<(int IdSociete, int IdSite, int IdReservation)> SeedHoldWithFlexPayConfigAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, idSite) = await SeedSocieteWithSiteAsync(ctx, "ST FlexPay");

            ctx.InfoPaiementsSociete.Add(new InfoPaiementSociete
            {
                IdSociete = idSociete,
                IdSite = idSite,
                CodeMarchand = "MERCHANT-ST",
                ApiToken = "token-test",
                ActifMobileMoney = true,
                ActifCarteBancaire = true,
                Statut = true
            });

            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"FP-{Guid.NewGuid():N}"[..10],
                Nom = "Lieu FlexPay",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            var journee = await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 20m
                }
            }, idSociete);
            await journeeService.PublishAsync(journee.IdSiteTouristiqueJournee, idSociete);

            var hold = await CreateHoldService(ctx).CreateHoldAsync(
                journee.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            return (idSociete, idSite, hold.IdSiteTouristiqueReservation);
        }

        public static async Task<(int IdSociete, int IdReservation, string OrderNumber)> SeedPendingFlexPayPaymentAsync(
            CongoTravelDbContext ctx,
            int quantity,
            string orderNumber = "ST-CALLBACK-TEST-001",
            int? idUtilisateur = null)
        {
            var (idSociete, _, idReservation) = await SeedHoldWithFlexPayConfigAsync(ctx, quantity);

            if (idUtilisateur is > 0)
            {
                var reservation = await ctx.SiteTouristiqueReservations
                    .FirstAsync(r => r.IdSiteTouristiqueReservation == idReservation);
                reservation.IdUtilisateur = idUtilisateur;
            }

            ctx.SiteTouristiquePayments.Add(new SiteTouristiquePayment
            {
                IdSiteTouristiqueReservation = idReservation,
                ReferencePaiement = $"ST-PAY-{orderNumber}",
                Provider = SiteTouristiqueFlexPayConstants.Provider,
                ProviderTxRef = orderNumber,
                Status = SiteTouristiquePaymentStatus.PENDING,
                Montant = 20m * quantity,
                CodeDevise = "USD",
                MontantTarif = 20m * quantity,
                CodeDeviseTarif = "USD",
                TauxVersDevisePaiement = 1m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return (idSociete, idReservation, orderNumber);
        }
    }
}
