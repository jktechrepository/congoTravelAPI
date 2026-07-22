using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class BilletMontantPayeHelperTests
    {
        private static CongoTravelDbContext BuildDb(string db) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>().UseInMemoryDatabase(db).Options);

        [Fact]
        public async Task ResolveMontantPayeBillet_single_passenger_uses_total_paid()
        {
            await using var ctx = BuildDb(nameof(ResolveMontantPayeBillet_single_passenger_uses_total_paid));
            var voyage = SeedVoyage(ctx, idVoyage: 1, prix: 1000);
            var reservation = SeedReservation(ctx, idReservation: 10, idVoyage: voyage.Id);
            var passenger = SeedPassenger(ctx, idReservation: reservation.IdReservation, idPassenger: 100);
            var siege = SeedSiege(ctx, idSiege: 1000, idVehicule: voyage.IdVehicule);
            SeedAllocation(ctx, voyage.Id, siege.IdSiege, passenger.IdReservationPassenger);
            var billet = SeedBillet(ctx, idBillet: 500, reservation.IdReservation, passenger.IdReservationPassenger, siege.IdSiege);

            ctx.Paiements.Add(new Paiement
            {
                IdReservation = reservation.IdReservation,
                IdSociete = 1,
                MontantAPaye = 800m,
                MontantPaye = 800m,
                Statut = true,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                MontantAPayeDevisePrincipale = 800m,
                MontantPayeDevisePrincipale = 800m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var montant = await BilletMontantPayeHelper.ResolveMontantPayeBilletAsync(
                ctx,
                new VoyageTarifService(ctx),
                billet,
                voyage);

            Assert.Equal(800m, montant);
        }

        [Fact]
        public async Task ResolveMontantPayeBillet_multi_passenger_prorata_by_seat_tariff()
        {
            await using var ctx = BuildDb(nameof(ResolveMontantPayeBillet_multi_passenger_prorata_by_seat_tariff));
            ctx.CategorieSieges.AddRange(
                new CategorieSiege { IdCategorieSiege = 1, IdSociete = 1, CodeCategorieSiege = "ECO", Libelle = "Eco", Statut = true },
                new CategorieSiege { IdCategorieSiege = 2, IdSociete = 1, CodeCategorieSiege = "VIP", Libelle = "Vip", Statut = true });
            var voyage = SeedVoyage(ctx, idVoyage: 2, prix: 1000);
            ctx.VoyageTarifsCategorieSiege.AddRange(
                new VoyageTarifCategorieSiege { IdVoyage = voyage.Id, IdCategorieSiege = 1, Prix = 1000, IdSociete = 1, DateCreation = DateTime.UtcNow },
                new VoyageTarifCategorieSiege { IdVoyage = voyage.Id, IdCategorieSiege = 2, Prix = 3000, IdSociete = 1, DateCreation = DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var reservation = SeedReservation(ctx, idReservation: 11, idVoyage: voyage.Id);
            var pEco = SeedPassenger(ctx, idReservation: reservation.IdReservation, idPassenger: 101);
            var pVip = SeedPassenger(ctx, idReservation: reservation.IdReservation, idPassenger: 102);
            var sEco = SeedSiege(ctx, idSiege: 1001, idVehicule: voyage.IdVehicule, idCategorie: 1);
            var sVip = SeedSiege(ctx, idSiege: 1002, idVehicule: voyage.IdVehicule, idCategorie: 2);
            SeedAllocation(ctx, voyage.Id, sEco.IdSiege, pEco.IdReservationPassenger);
            SeedAllocation(ctx, voyage.Id, sVip.IdSiege, pVip.IdReservationPassenger);
            var billetVip = SeedBillet(ctx, idBillet: 501, reservation.IdReservation, pVip.IdReservationPassenger, sVip.IdSiege);

            ctx.Paiements.Add(new Paiement
            {
                IdReservation = reservation.IdReservation,
                IdSociete = 1,
                MontantAPaye = 3200m,
                MontantPaye = 3200m,
                Statut = true,
                CodeDevisePaiement = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                MontantAPayeDevisePrincipale = 3200m,
                MontantPayeDevisePrincipale = 3200m,
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var montant = await BilletMontantPayeHelper.ResolveMontantPayeBilletAsync(
                ctx,
                new VoyageTarifService(ctx),
                billetVip,
                voyage);

            Assert.Equal(2400m, montant);
        }

        [Fact]
        public async Task ResolveMontantPayeBillet_without_payment_falls_back_to_catalogue_tariff()
        {
            await using var ctx = BuildDb(nameof(ResolveMontantPayeBillet_without_payment_falls_back_to_catalogue_tariff));
            ctx.CategorieSieges.Add(new CategorieSiege { IdCategorieSiege = 3, IdSociete = 1, CodeCategorieSiege = "ECO2", Libelle = "Eco2", Statut = true });
            var voyage = SeedVoyage(ctx, idVoyage: 3, prix: 500);
            ctx.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = voyage.Id,
                IdCategorieSiege = 3,
                Prix = 750,
                IdSociete = 1,
                DateCreation = DateTime.UtcNow
            });
            var reservation = SeedReservation(ctx, idReservation: 12, idVoyage: voyage.Id);
            var passenger = SeedPassenger(ctx, idReservation: reservation.IdReservation, idPassenger: 103);
            var siege = SeedSiege(ctx, idSiege: 1003, idVehicule: voyage.IdVehicule, idCategorie: 3);
            SeedAllocation(ctx, voyage.Id, siege.IdSiege, passenger.IdReservationPassenger);
            var billet = SeedBillet(ctx, idBillet: 502, reservation.IdReservation, passenger.IdReservationPassenger, siege.IdSiege);
            await ctx.SaveChangesAsync();

            var montant = await BilletMontantPayeHelper.ResolveMontantPayeBilletAsync(
                ctx,
                new VoyageTarifService(ctx),
                billet,
                voyage);

            Assert.Equal(750m, montant);
        }

        private static Voyage SeedVoyage(CongoTravelDbContext ctx, int idVoyage, int prix)
        {
            if (!ctx.Vehicules.Any())
                ctx.Vehicules.Add(new Vehicule { IdVehicule = 1, AliasVehicule = "BUS", NombreSiege = 10, IdSociete = 1, IdTypeVehicule = 1, Statut = true });
            if (!ctx.Destinations.Any())
                ctx.Destinations.Add(new Destination { IdDestination = 1, IdSociete = 1, VilleDepart = "A", VilleArrivee = "B", Statut = true });

            var voyage = new Voyage
            {
                Id = idVoyage,
                IdSociete = 1,
                IdVehicule = 1,
                IdDestination = 1,
                DateDepart = DateTime.Today.AddDays(1),
                HeureDepart = TimeSpan.FromHours(8),
                Prix = prix,
                CodeDevisePrix = "CDF",
                CodeDevisePrincipale = "CDF",
                TauxVersDevisePrincipale = 1m,
                PrixDevisePrincipale = prix
            };
            ctx.Voyages.Add(voyage);
            return voyage;
        }

        private static Reservation SeedReservation(CongoTravelDbContext ctx, int idReservation, int idVoyage) =>
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
            }).Entity;

        private static ReservationPassenger SeedPassenger(CongoTravelDbContext ctx, int idReservation, int idPassenger) =>
            ctx.ReservationPassengers.Add(new ReservationPassenger
            {
                IdReservationPassenger = idPassenger,
                IdReservation = idReservation,
                IdSociete = 1,
                NomComplet = $"Passager {idPassenger}",
                Statut = true
            }).Entity;

        private static Siege SeedSiege(CongoTravelDbContext ctx, int idSiege, int idVehicule, int idCategorie = 1)
        {
            var categorieExists = ctx.CategorieSieges.Local.Any(c => c.IdCategorieSiege == idCategorie)
                || ctx.CategorieSieges.Any(c => c.IdCategorieSiege == idCategorie);
            if (!categorieExists)
            {
                ctx.CategorieSieges.Add(new CategorieSiege
                {
                    IdCategorieSiege = idCategorie,
                    IdSociete = 1,
                    CodeCategorieSiege = $"C{idCategorie}",
                    Libelle = "Cat",
                    Statut = true
                });
            }

            return ctx.Sieges.Add(new Siege
            {
                IdSiege = idSiege,
                IdVehicule = idVehicule,
                IdSociete = 1,
                IdCategorieSiege = idCategorie,
                CodeSiege = $"S{idSiege}",
                NumeroOrdre = idSiege,
                EstActif = true
            }).Entity;
        }

        private static void SeedAllocation(CongoTravelDbContext ctx, int idVoyage, int idSiege, int idPassenger) =>
            ctx.VoyageSeatAllocations.Add(new VoyageSeatAllocation
            {
                IdVoyage = idVoyage,
                IdSiege = idSiege,
                IdReservationPassenger = idPassenger,
                Statut = "CONFIRME"
            });

        private static Billet SeedBillet(CongoTravelDbContext ctx, int idBillet, int idReservation, int idPassenger, int idSiege) =>
            ctx.Billets.Add(new Billet
            {
                IdBillet = idBillet,
                IdSociete = 1,
                IdReservation = idReservation,
                IdReservationPassenger = idPassenger,
                IdSiege = idSiege,
                CodeSiege = $"S{idSiege}",
                QrCode = $"QR-{idBillet}",
                DateGeneration = DateTime.UtcNow,
                IsUsed = false
            }).Entity;
    }
}
