using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.ConfigSociete;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class ConfigSocieteTests
    {
        private static CongoTravelDbContext BuildDb(string db) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options);

        [Fact]
        public async Task GetOrCreateAsync_returns_defaults_for_new_societe()
        {
            await using var ctx = BuildDb(nameof(GetOrCreateAsync_returns_defaults_for_new_societe));
            var s = new Societe { Nom = "Test", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var svc = new ConfigSocieteService(ctx);
            var config = await svc.GetOrCreateAsync(s.IdSociete);

            Assert.Equal(ConfigSocieteDefaults.DureeValiditeBilletJours, config.DureeValiditeBilletJours);
            Assert.Equal(ConfigSocieteDefaults.HeuresLimiteReaffectation, config.HeuresLimiteReaffectation);
            Assert.Equal(ConfigSocieteDefaults.JoursAvanceMaxReservationDefault, config.JoursAvanceMaxReservation);
            Assert.Equal(
                ConfigSocieteDefaults.HeuresOuvertureEntreeEvenementAvantDebut,
                config.HeuresOuvertureEntreeEvenementAvantDebut);
            Assert.Equal(
                ConfigSocieteDefaults.HeuresOuvertureEntreeRestaurantAvantDebut,
                config.HeuresOuvertureEntreeRestaurantAvantDebut);
            Assert.True(config.ReservationIsActif);
            Assert.True(config.ReaffectationActive);
        }

        [Fact]
        public async Task UpdateAsync_persists_custom_values()
        {
            await using var ctx = BuildDb(nameof(UpdateAsync_persists_custom_values));
            var s = new Societe { Nom = "Test", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var svc = new ConfigSocieteService(ctx);
            var updated = await svc.UpdateAsync(s.IdSociete, new ConfigSocieteUpdateDto
            {
                DureeValiditeBilletJours = 14,
                PenaliteReaffectationPourcentage = 10m,
                JoursAvanceMaxReservation = 60,
                HeuresLimiteReaffectation = 4,
                HeuresOuvertureEmbarquementAvantDepart = 2,
                HeuresFermetureEmbarquementApresJourDepart = 12,
                HeuresOuvertureEntreeEvenementAvantDebut = 1,
                HeuresOuvertureEntreeRestaurantAvantDebut = 2,
                DureeHoldFlexPayMinutes = 20,
                ReaffectationActive = false,
                ReservationIsActif = false,
                PoidsBagageParKiloOffert = 25m
            });

            Assert.Equal(14, updated.DureeValiditeBilletJours);
            Assert.Equal(10m, updated.PenaliteReaffectationPourcentage);
            Assert.Equal(60, updated.JoursAvanceMaxReservation);
            Assert.Equal(1, updated.HeuresOuvertureEntreeEvenementAvantDebut);
            Assert.Equal(2, updated.HeuresOuvertureEntreeRestaurantAvantDebut);
            Assert.False(updated.ReaffectationActive);
            Assert.False(updated.ReservationIsActif);
            Assert.Equal(25m, updated.PoidsBagageParKiloOffert);
        }

        [Fact]
        public async Task EnsureReservationsActivesAsync_throws_when_disabled()
        {
            await using var ctx = BuildDb(nameof(EnsureReservationsActivesAsync_throws_when_disabled));
            var s = new Societe { Nom = "Test", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var svc = new ConfigSocieteService(ctx);
            var config = await svc.GetOrCreateAsync(s.IdSociete);
            config.ReservationIsActif = false;
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                svc.EnsureReservationsActivesAsync(s.IdSociete));
            Assert.Equal("La reservation n'est pas Activée pour cette société", ex.Message);
        }

        [Fact]
        public async Task EnsureReservationsActivesAsync_allows_when_active()
        {
            await using var ctx = BuildDb(nameof(EnsureReservationsActivesAsync_allows_when_active));
            var s = new Societe { Nom = "Test", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(s);
            await ctx.SaveChangesAsync();

            var svc = new ConfigSocieteService(ctx);
            await svc.EnsureReservationsActivesAsync(s.IdSociete);
        }

        [Fact]
        public void Normalize_clamps_poids_bagage_to_non_negative()
        {
            var config = ConfigSocieteDefaults.CreateForSociete(1);
            Assert.True(config.ReservationIsActif);
            config.PoidsBagageParKiloOffert = -5m;
            ConfigSocieteDefaults.Normalize(config);
            Assert.Equal(0m, config.PoidsBagageParKiloOffert);
        }

        [Fact]
        public void Normalize_clamps_penalite_pourcentage_to_0_100()
        {
            var config = ConfigSocieteDefaults.CreateForSociete(1);
            config.PenaliteReaffectationPourcentage = 150m;
            ConfigSocieteDefaults.Normalize(config);
            Assert.Equal(100m, config.PenaliteReaffectationPourcentage);
        }

        [Fact]
        public void EnsureReservationHorizon_blocks_far_departures()
        {
            var config = ConfigSocieteDefaults.CreateForSociete(1);
            config.JoursAvanceMaxReservation = 30;
            var voyage = new Voyage
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(31),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = 100,
                IdVehicule = 1,
                IdDestination = 1,
                IdSociete = 1,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 100
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                ConfigSocieteDefaults.EnsureReservationHorizon(voyage, config));
            Assert.Contains("horizon", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
