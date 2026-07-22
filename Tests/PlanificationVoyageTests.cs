using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.PlanificationVoyage;
using CongoTravel.Models.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class PlanificationVoyageTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static VoyageService CreateVoyageService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<VoyageService>.Instance, new VoyageTarifService(ctx), SiegeDisponibiliteTestHelper.Create(ctx));

        private static VoyageGenerationService CreateGenerationService(CongoTravelDbContext ctx) =>
            new(ctx, CreateVoyageService(ctx), new VoyageTarifService(ctx), NullLogger<VoyageGenerationService>.Instance);

        [Fact]
        public async Task Generer_mois_courant_cree_voyages_pour_jours_selectionnes()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Generer_mois_courant_cree_voyages_pour_jours_selectionnes)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday });

            var svc = CreateGenerationService(ctx);
            var result = await svc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 30)
            });

            Assert.Equal(5, result.Resume.Creees);
            Assert.All(result.Details.Where(d => d.Statut == PlanificationGenerationItemStatut.Cree),
                d => Assert.NotNull(d.IdVoyage));

            var voyages = await ctx.Voyages.Where(v => v.IdPlanificationVoyage == seed.PlanifId).ToListAsync();
            Assert.Equal(5, voyages.Count);
            Assert.All(voyages, v => Assert.Equal(DayOfWeek.Monday, v.DateDepart.DayOfWeek));
        }

        [Fact]
        public async Task Generer_multi_jours_lundi_mercredi_vendredi()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Generer_multi_jours_lundi_mercredi_vendredi)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int>
            {
                (int)DayOfWeek.Monday,
                (int)DayOfWeek.Wednesday,
                (int)DayOfWeek.Friday
            });

            var svc = CreateGenerationService(ctx);
            var result = await svc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 7)
            });

            Assert.Equal(3, result.Resume.Creees);
        }

        [Fact]
        public async Task Generer_ignore_creneau_deja_existant()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Generer_ignore_creneau_deja_existant)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday });

            ctx.Voyages.Add(new Voyage
            {
                DateDepart = new DateTime(2026, 6, 1),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 5000m,
                IdVehicule = seed.IdVehicule,
                IdDestination = seed.IdDestination,
                IdSociete = seed.IdSociete,
                IdSite = seed.IdSite,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = CreateGenerationService(ctx);
            var result = await svc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 30)
            });

            Assert.Equal(4, result.Resume.Creees);
            Assert.Equal(1, result.Resume.Ignorees);
        }

        [Fact]
        public async Task Generer_resout_devise_principale_par_date()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Generer_resout_devise_principale_par_date)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Tuesday });

            var svc = CreateGenerationService(ctx);
            await svc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 2),
                DateFin = new DateTime(2026, 6, 2)
            });

            var voyage = await ctx.Voyages.FirstAsync(v => v.IdPlanificationVoyage == seed.PlanifId);
            Assert.Equal(5000m, voyage.PrixDevisePrincipale);
            Assert.Equal("CDF", voyage.CodeDevisePrincipale);
        }

        [Fact]
        public async Task Generer_retourne_avertissement_horizon_reservation()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Generer_retourne_avertissement_horizon_reservation)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday });

            ctx.ConfigSocietes.Add(new ConfigSociete
            {
                IdSociete = seed.IdSociete,
                JoursAvanceMaxReservation = 7,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = CreateGenerationService(ctx);
            var farFutureStart = DateTime.UtcNow.Date.AddDays(30);
            var farFutureEnd = farFutureStart.AddDays(6);

            var result = await svc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = farFutureStart,
                DateFin = farFutureEnd
            });

            Assert.NotEmpty(result.Avertissements);
            Assert.Contains(result.Avertissements, a => a.Contains("horizon de réservation"));
        }

        [Fact]
        public async Task Modifier_template_ne_modifie_pas_voyages_existants()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Modifier_template_ne_modifie_pas_voyages_existants)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday }, prix: 5000);

            var genSvc = CreateGenerationService(ctx);
            await genSvc.GenererAsync(seed.PlanifId, new GenererPlanificationVoyageDto
            {
                Mode = PlanificationGenerationMode.PeriodePersonnalisee,
                DateDebut = new DateTime(2026, 6, 1),
                DateFin = new DateTime(2026, 6, 7)
            });

            var planifSvc = new PlanificationVoyageService(ctx, NullLogger<PlanificationVoyageService>.Instance);
            await planifSvc.UpdateAsync(new UpdatePlanificationVoyageDto
            {
                IdPlanificationVoyage = seed.PlanifId,
                Libelle = "Modifié",
                IdSociete = seed.IdSociete,
                IdSite = seed.IdSite,
                IdVehicule = seed.IdVehicule,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 9999,
                CodeDevisePrix = "CDF",
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday },
                IdDestination = seed.IdDestination,
                Statut = true
            });

            var voyage = await ctx.Voyages.FirstAsync(v => v.IdPlanificationVoyage == seed.PlanifId);
            Assert.Equal(5000, voyage.Prix);
        }

        [Fact]
        public async Task UpdateAsync_persiste_jours_semaine_modifies()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(UpdateAsync_persiste_jours_semaine_modifies)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday });

            var planifSvc = new PlanificationVoyageService(ctx, NullLogger<PlanificationVoyageService>.Instance);
            await planifSvc.UpdateAsync(new UpdatePlanificationVoyageDto
            {
                IdPlanificationVoyage = seed.PlanifId,
                Libelle = "Multi-jours",
                IdSociete = seed.IdSociete,
                IdSite = seed.IdSite,
                IdVehicule = seed.IdVehicule,
                HeureDepart = TimeSpan.FromHours(7),
                Prix = 5000,
                CodeDevisePrix = "CDF",
                JoursSemaine = new List<int> { (int)DayOfWeek.Monday, (int)DayOfWeek.Wednesday, (int)DayOfWeek.Friday },
                IdDestination = seed.IdDestination,
                Statut = true
            });

            var reloaded = await ctx.PlanificationsVoyage.AsNoTracking()
                .FirstAsync(p => p.IdPlanificationVoyage == seed.PlanifId);

            Assert.Equal(new[] { 1, 3, 5 }, reloaded.JoursSemaine.OrderBy(j => j));
        }

        [Fact]
        public async Task Controller_forbid_si_societe_mismatch()
        {
            await using var ctx = BuildDb($"{nameof(PlanificationVoyageTests)}_{nameof(Controller_forbid_si_societe_mismatch)}");
            var seed = await SeedPlanificationAsync(ctx, joursSemaine: new List<int> { (int)DayOfWeek.Monday });

            var user = new Mock<ICurrentUserService>();
            user.SetupGet(x => x.IsSuperAdmin).Returns(false);
            user.SetupGet(x => x.SocieteId).Returns(99);

            var controller = new PlanificationVoyageController(
                new PlanificationVoyageService(ctx, NullLogger<PlanificationVoyageService>.Instance),
                CreateGenerationService(ctx),
                user.Object,
                NullLogger<PlanificationVoyageController>.Instance);

            var result = await controller.GetBySociete(seed.IdSociete);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(403, status.StatusCode);
        }

        [Fact]
        public void ExpandDates_filtre_jours_semaine()
        {
            var dates = PlanificationVoyageDateHelper.ExpandDates(
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 7),
                new[] { (int)DayOfWeek.Monday, (int)DayOfWeek.Friday });

            Assert.Equal(2, dates.Count);
            Assert.Equal(DayOfWeek.Monday, dates[0].DayOfWeek);
            Assert.Equal(DayOfWeek.Friday, dates[1].DayOfWeek);
        }

        private static async Task<(int PlanifId, int IdSociete, int IdSite, int IdVehicule, int IdDestination)> SeedPlanificationAsync(
            CongoTravelDbContext ctx,
            List<int> joursSemaine,
            int prix = 5000)
        {
            var societe = new Societe
            {
                Nom = "Rusa Demo",
                CodeDevisePrincipale = "CDF",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            ctx.DevisesMonetaires.Add(new DeviseMonetaire
            {
                CodeDevise = "CDF",
                Libelle = "Franc congolais",
                Statut = true,
                IdSociete = societe.IdSociete
            });

            var site = new Site
            {
                IdSociete = societe.IdSociete,
                CodeSite = "S1",
                NomSite = "Gare",
                NomResponsableSite = "Resp",
                Genre = "Masculin",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Sites.Add(site);

            var type = new TypeVehicule
            {
                Libelle = "Bus",
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.TypeVehicules.Add(type);

            var dest = new Destination
            {
                VilleDepart = "Kinshasa",
                VilleArrivee = "Goma",
                Montant = prix,
                IdSociete = societe.IdSociete,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Destinations.Add(dest);
            await ctx.SaveChangesAsync();

            var vehicule = new Vehicule
            {
                AliasVehicule = "BUS-1",
                Marques = "Toyota",
                IdTypeVehicule = type.IdTypeVehicule,
                NombreSiege = 20,
                IdSociete = societe.IdSociete,
                NumeroDePlaque = "ABC-1",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Vehicules.Add(vehicule);

            ctx.CategorieSieges.Add(new CategorieSiege
            {
                IdSociete = societe.IdSociete,
                CodeCategorieSiege = "ECO",
                Libelle = "Eco",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var planifSvc = new PlanificationVoyageService(ctx, NullLogger<PlanificationVoyageService>.Instance);
            var created = await planifSvc.CreateAsync(new CreatePlanificationVoyageDto
            {
                Libelle = "Kin-Gom semaine",
                IdSociete = societe.IdSociete,
                IdSite = site.IdSite,
                IdVehicule = vehicule.IdVehicule,
                HeureDepart = TimeSpan.FromHours(8),
                Prix = prix,
                CodeDevisePrix = "CDF",
                JoursSemaine = joursSemaine,
                IdDestination = dest.IdDestination,
                Statut = true
            });

            return (created.IdPlanificationVoyage, societe.IdSociete, site.IdSite, vehicule.IdVehicule, dest.IdDestination);
        }
    }
}
