using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Attributes;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using Xunit;

namespace CongoTravel.Tests
{
    public class RapportCaisseMetricsHelperTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void ResolvePeriode_uses_today_when_no_params()
        {
            var (from, to, mode, isValid, error) = RapportCaisseMetricsHelper.ResolvePeriode(null, null, null);

            Assert.True(isValid);
            Assert.Null(error);
            Assert.Equal("jour", mode);
            Assert.Equal(DateTime.UtcNow.Date, from.Date);
            Assert.Equal(from.Date.AddDays(1).AddTicks(-1), to);
        }

        [Fact]
        public void ResolvePeriode_prioritizes_interval_over_date_precise()
        {
            var precise = new DateTime(2026, 6, 10);
            var debut = new DateTime(2026, 6, 1);
            var fin = new DateTime(2026, 6, 30);
            var (from, to, mode, isValid, error) = RapportCaisseMetricsHelper.ResolvePeriode(precise, debut, fin);

            Assert.True(isValid);
            Assert.Null(error);
            Assert.Equal("intervalle", mode);
            Assert.Equal(debut.Date, from);
            Assert.Equal(fin.Date.AddDays(1).AddTicks(-1), to);
        }

        [Fact]
        public void ResolvePeriode_rejects_half_interval()
        {
            var (_, _, _, isValid, error) = RapportCaisseMetricsHelper.ResolvePeriode(null, DateTime.UtcNow.Date, null);

            Assert.False(isValid);
            Assert.NotNull(error);
        }

        [Fact]
        public void BuildRapportCaisse_splits_cash_and_electronic_with_details()
        {
            var paiements = new List<Paiement>
            {
                NewPaiement(1, "CASH", 100m, 100m, "CDF", 10),
                NewPaiement(2, "MOBILE_MONEY", 50m, 50m, "CDF", 10),
                NewPaiement(3, "CARTE_BANCAIRE", 30m, 30m, "CDF", 10),
                NewPaiement(4, "VIREMENT BANCAIRE", 20m, 20m, "CDF", 10),
                NewPaiement(5, "CHEQUE", 10m, 10m, "USD", 10)
            };

            var result = RapportCaisseMetricsHelper.BuildRapportCaisse(
                paiements,
                idSociete: 1,
                idUtilisateur: 10,
                periodeDebutUtc: new DateTime(2026, 6, 1),
                periodeFinUtc: new DateTime(2026, 6, 1).AddDays(1).AddTicks(-1),
                modePeriode: "jour",
                codeDevisePrincipale: "CDF");

            Assert.Equal(100m, result.Especes.MontantDevisePrincipale);
            Assert.Equal(4, result.Electronique.NombreTransactions);
            Assert.Equal(50m, result.Electronique.Detail.MobileMoney.MontantDevisePrincipale);
            Assert.Equal(30m, result.Electronique.Detail.Carte.MontantDevisePrincipale);
            Assert.Equal(20m, result.Electronique.Detail.Virement.MontantDevisePrincipale);
            Assert.Equal(10m, result.Electronique.Detail.Autre.MontantDevisePrincipale);
            Assert.Equal(210m, result.Synthese.TotalEncaisse);
            Assert.Equal(5, result.Synthese.NombreTransactions);
            Assert.Equal(47.62m, result.Synthese.PartEspecesPourcentage);
            Assert.Equal(52.38m, result.Synthese.PartElectroniquePourcentage);
            Assert.Equal(2, result.ParDevise.Count);
        }

        [Fact]
        public async Task GetRapportCaisse_filters_by_idUtilisateur()
        {
            await using var ctx = BuildDb(nameof(GetRapportCaisse_filters_by_idUtilisateur));
            ctx.Paiements.AddRange(
                NewPaiement(100, "CASH", 100m, 100m, "CDF", 42),
                NewPaiement(101, "MOBILE_MONEY", 200m, 200m, "CDF", 99)
            );
            await ctx.SaveChangesAsync();

            var controller = new FinanceReportingController(ctx);
            var action = await controller.GetRapportCaisse(
                idSociete: 1,
                idUtilisateur: 42,
                datePrecise: new DateTime(2026, 6, 1),
                dateDebut: null,
                dateFin: null);

            var ok = Assert.IsType<OkObjectResult>(action);
            var dto = Assert.IsType<RapportCaisseDto>(ok.Value);
            Assert.Equal(1, dto.Synthese.NombreTransactions);
            Assert.Equal(100m, dto.Synthese.TotalEncaisse);
        }

        [Fact]
        public async Task GetRapportCaisse_returns_badrequest_when_half_interval()
        {
            await using var ctx = BuildDb(nameof(GetRapportCaisse_returns_badrequest_when_half_interval));
            var controller = new FinanceReportingController(ctx);

            var action = await controller.GetRapportCaisse(
                idSociete: 1,
                idUtilisateur: null,
                datePrecise: null,
                dateDebut: new DateTime(2026, 6, 1),
                dateFin: null);

            Assert.IsType<BadRequestObjectResult>(action);
        }

        [Fact]
        public void GetRapportCaisse_has_finance_reporting_permission_attribute()
        {
            var method = typeof(FinanceReportingController).GetMethod(nameof(FinanceReportingController.GetRapportCaisse));
            Assert.NotNull(method);
            var permission = method!.GetCustomAttributes(typeof(PermissionAttribute), false)
                .Cast<PermissionAttribute>()
                .FirstOrDefault();
            Assert.NotNull(permission);
            var permissionField = typeof(PermissionAttribute).GetField("_permission",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(permissionField);
            var value = permissionField!.GetValue(permission) as string;
            Assert.Equal("FinanceReporting.ReadAll", value);
        }

        private static Paiement NewPaiement(
            int id,
            string methode,
            decimal montantPaye,
            decimal montantPrincipal,
            string codeDevisePaiement,
            int idUtilisateur)
        {
            return new Paiement
            {
                IdPaiement = id,
                IdSociete = 1,
                IdUtilisateur = idUtilisateur,
                MontantAPaye = montantPaye,
                MontantAPayeDevisePrincipale = montantPrincipal,
                MontantPaye = montantPaye,
                MontantPayeDevisePrincipale = montantPrincipal,
                CodeDevisePaiement = codeDevisePaiement,
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                DatePaiement = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                Statut = true,
                IsDeleted = false,
                Origine = "CAISSIER",
                MethodePaiement = methode
            };
        }
    }
}
