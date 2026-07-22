using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Voyage;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageReportTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static VoyageReportService CreateService(
            CongoTravelDbContext ctx,
            Mock<IVoyageReportNotificationService>? notificationMock = null)
        {
            if (notificationMock == null)
            {
                notificationMock = new Mock<IVoyageReportNotificationService>();
                notificationMock
                    .Setup(n => n.NotifyReservedClientsAsync(
                        It.IsAny<Voyage>(),
                        It.IsAny<DateTime>(),
                        It.IsAny<TimeSpan>(),
                        It.IsAny<string?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((0, 0));
            }

            return new VoyageReportService(
                ctx,
                ConfigSocieteTestHelper.Create(ctx),
                notificationMock.Object,
                NullLogger<VoyageReportService>.Instance);
        }

        [Fact]
        public async Task Reporter_sans_reservation_met_a_jour_date()
        {
            await using var ctx = BuildDb(nameof(Reporter_sans_reservation_met_a_jour_date));
            var voyage = SeedVoyage(ctx, id: 1, date: DateTime.UtcNow.Date.AddDays(2), heure: TimeSpan.FromHours(8));
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var newDate = DateTime.UtcNow.Date.AddDays(5);
            var result = await svc.ReporterAsync(1, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = newDate,
                HeureDepart = TimeSpan.FromHours(10),
                NotifierClients = false
            });

            Assert.True(result.Success);
            Assert.Equal(newDate, result.Data!.NouvelleDateDepart);
            Assert.Equal(0, result.Data.NombreReservationsImpactees);
            var updated = await ctx.Voyages.FindAsync(1);
            Assert.Equal(newDate, updated!.DateDepart);
        }

        [Fact]
        public async Task Reporter_avec_reservation_recalcule_validite_billet()
        {
            await using var ctx = BuildDb(nameof(Reporter_avec_reservation_recalcule_validite_billet));
            var voyage = SeedVoyage(ctx, id: 2, date: DateTime.UtcNow.Date.AddDays(3), heure: TimeSpan.FromHours(8));
            SeedReservationWithBillet(ctx, idReservation: 10, idVoyage: 2, idBillet: 100);
            await ConfigSocieteTestHelper.SeedAsync(ctx, 1, c => c.DureeValiditeBilletJours = 7);
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var newDate = DateTime.UtcNow.Date.AddDays(6);
            var result = await svc.ReporterAsync(2, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = newDate,
                HeureDepart = TimeSpan.FromHours(9),
                NotifierClients = false
            });

            Assert.True(result.Success);
            Assert.Equal(1, result.Data!.NombreReservationsImpactees);
            Assert.Equal(1, result.Data.NombreBilletsRecalcules);

            var billet = await ctx.Billets.FindAsync(100);
            Assert.Equal(newDate.Date, billet!.DateValiditeDebut!.Value.Date);
            Assert.Equal(newDate.Date.AddDays(7), billet.DateValiditeFin!.Value.Date);
        }

        [Fact]
        public async Task Reporter_refuse_si_billet_deja_utilise()
        {
            await using var ctx = BuildDb(nameof(Reporter_refuse_si_billet_deja_utilise));
            var voyage = SeedVoyage(ctx, id: 3, date: DateTime.UtcNow.Date.AddDays(3), heure: TimeSpan.FromHours(8));
            SeedReservationWithBillet(ctx, idReservation: 11, idVoyage: 3, idBillet: 101, isUsed: true);
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var result = await svc.ReporterAsync(3, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(7),
                HeureDepart = TimeSpan.FromHours(9),
                NotifierClients = false
            });

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.NotNull(result.BilletsUtilises);
            Assert.Contains(101, result.BilletsUtilises!);
        }

        [Fact]
        public async Task Reporter_autorise_billet_utilise_avec_confirmation()
        {
            await using var ctx = BuildDb(nameof(Reporter_autorise_billet_utilise_avec_confirmation));
            var voyage = SeedVoyage(ctx, id: 4, date: DateTime.UtcNow.Date.AddDays(3), heure: TimeSpan.FromHours(8));
            SeedReservationWithBillet(ctx, idReservation: 12, idVoyage: 4, idBillet: 102, isUsed: true);
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var result = await svc.ReporterAsync(4, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(8),
                HeureDepart = TimeSpan.FromHours(11),
                ConfirmerAvecBilletsUtilises = true,
                NotifierClients = false
            });

            Assert.True(result.Success);
            Assert.NotEmpty(result.Data!.Avertissements);
        }

        [Fact]
        public async Task Reporter_refuse_conflit_vehicule()
        {
            await using var ctx = BuildDb(nameof(Reporter_refuse_conflit_vehicule));
            var targetDate = DateTime.UtcNow.Date.AddDays(10);
            SeedVoyage(ctx, id: 5, date: DateTime.UtcNow.Date.AddDays(2), heure: TimeSpan.FromHours(8));
            await ctx.SaveChangesAsync();
            ctx.Voyages.Add(new Voyage
            {
                Id = 6,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                IdSite = 1,
                DateDepart = targetDate,
                HeureDepart = TimeSpan.FromHours(14),
                Prix = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 5000,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var result = await svc.ReporterAsync(5, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = targetDate,
                HeureDepart = TimeSpan.FromHours(14),
                NotifierClients = false
            });

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("véhicule", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Reporter_refuse_si_flexpay_hold_actif()
        {
            await using var ctx = BuildDb(nameof(Reporter_refuse_si_flexpay_hold_actif));
            var voyage = SeedVoyage(ctx, id: 7, date: DateTime.UtcNow.Date.AddDays(2), heure: TimeSpan.FromHours(8));
            ctx.SiegeHoldsEnAttente.Add(new SiegeHoldEnAttente
            {
                IdVoyage = 7,
                IdSiege = 1,
                IdCommandeReservationEnAttente = Guid.NewGuid(),
                ExpireAt = DateTime.UtcNow.AddMinutes(10),
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var result = await svc.ReporterAsync(7, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(9),
                HeureDepart = TimeSpan.FromHours(7),
                NotifierClients = false
            });

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("FlexPay", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Reporter_refuse_si_depart_actuel_passe()
        {
            await using var ctx = BuildDb(nameof(Reporter_refuse_si_depart_actuel_passe));
            var voyage = SeedVoyage(ctx, id: 9, date: DateTime.UtcNow.Date.AddDays(-1), heure: TimeSpan.FromHours(8));
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var result = await svc.ReporterAsync(9, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(3),
                HeureDepart = TimeSpan.FromHours(10),
                NotifierClients = false
            });

            Assert.False(result.Success);
            Assert.Equal(409, result.StatusCode);
            Assert.Contains("passées", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Reporter_autorise_avancement_futur()
        {
            await using var ctx = BuildDb(nameof(Reporter_autorise_avancement_futur));
            var voyage = SeedVoyage(ctx, id: 10, date: DateTime.UtcNow.Date.AddDays(5), heure: TimeSpan.FromHours(8));
            await ctx.SaveChangesAsync();

            var svc = CreateService(ctx);
            var newDate = DateTime.UtcNow.Date.AddDays(3);
            var result = await svc.ReporterAsync(10, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = newDate,
                HeureDepart = TimeSpan.FromHours(10),
                NotifierClients = false
            });

            Assert.True(result.Success);
            Assert.Equal(newDate, result.Data!.NouvelleDateDepart);
            var updated = await ctx.Voyages.FindAsync(10);
            Assert.Equal(newDate, updated!.DateDepart);
        }

        [Fact]
        public async Task Reporter_declenche_notifications_quand_demande()
        {
            await using var ctx = BuildDb(nameof(Reporter_declenche_notifications_quand_demande));
            var voyage = SeedVoyage(ctx, id: 8, date: DateTime.UtcNow.Date.AddDays(2), heure: TimeSpan.FromHours(8));
            SeedReservationWithBillet(ctx, idReservation: 13, idVoyage: 8, idBillet: 103);
            await ctx.SaveChangesAsync();

            var notifMock = new Mock<IVoyageReportNotificationService>();
            notifMock
                .Setup(n => n.NotifyReservedClientsAsync(
                    It.IsAny<Voyage>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((2, 1));

            var svc = CreateService(ctx, notifMock);
            var result = await svc.ReporterAsync(8, 1, 99, "admin", new ReporterVoyageDto
            {
                DateDepart = DateTime.UtcNow.Date.AddDays(11),
                HeureDepart = TimeSpan.FromHours(6),
                NotifierClients = true,
                Motif = "Panne"
            });

            Assert.True(result.Success);
            Assert.Equal(2, result.Data!.NotificationsEnvoyees);
            Assert.Equal(1, result.Data.NotificationsEchecs);
            notifMock.Verify(n => n.NotifyReservedClientsAsync(
                It.IsAny<Voyage>(),
                It.IsAny<DateTime>(),
                It.IsAny<TimeSpan>(),
                "Panne",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Voyage SeedVoyage(CongoTravelDbContext ctx, int id, DateTime date, TimeSpan heure)
        {
            if (!ctx.Vehicules.Any())
            {
                ctx.Vehicules.Add(new Vehicule
                {
                    IdVehicule = 1,
                    AliasVehicule = "BUS-A",
                    NombreSiege = 20,
                    IdSociete = 1,
                    IdTypeVehicule = 1,
                    Statut = true
                });
            }

            if (!ctx.Destinations.Any())
            {
                ctx.Destinations.Add(new Destination
                {
                    IdDestination = 1,
                    IdSociete = 1,
                    VilleDepart = "Kinshasa",
                    VilleArrivee = "Goma",
                    Statut = true
                });
            }

            if (!ctx.Sites.Any())
            {
                ctx.Sites.Add(new Site
                {
                    IdSite = 1,
                    IdSociete = 1,
                    CodeSite = "PRIN",
                    NomSite = "Principal",
                    NomResponsableSite = "Responsable",
                    Genre = "Masculin",
                    Statut = true
                });
            }

            var voyage = new Voyage
            {
                Id = id,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                IdSite = 1,
                DateDepart = date,
                HeureDepart = heure,
                Prix = 5000,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = 5000,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.Voyages.Add(voyage);
            return voyage;
        }

        private static void SeedReservationWithBillet(
            CongoTravelDbContext ctx,
            int idReservation,
            int idVoyage,
            int idBillet,
            bool isUsed = false)
        {
            if (!ctx.Clients.Any())
            {
                ctx.Clients.Add(new Client
                {
                    IdClient = 1,
                    NomClient = "Client Test",
                    AdresseClient = "Adresse test",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            ctx.Reservations.Add(new Reservation
            {
                IdReservation = idReservation,
                IdVoyage = idVoyage,
                IdClient = 1,
                IdUtilisateur = 1,
                IdSociete = 1,
                DateReservation = DateTime.UtcNow,
                Statut = true,
                StatutReservation = "CONFIRMEE",
                NombreDePlace = 1
            });

            ctx.Billets.Add(new Billet
            {
                IdBillet = idBillet,
                IdSociete = 1,
                IdReservation = idReservation,
                QrCode = $"QR-{idBillet}",
                DateGeneration = DateTime.UtcNow,
                IsUsed = isUsed,
                DateValiditeDebut = DateTime.UtcNow.Date,
                DateValiditeFin = DateTime.UtcNow.Date.AddDays(7)
            });
        }
    }
}
