using CongoTravel.Helpers.Transport;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;
using Xunit;

namespace CongoTravel.Tests
{
    public class AllerRetourVoyageCompatibilityHelperTests
    {
        private static Voyage Voyage(int id, int societe, DateTime date, TimeSpan heure) => new()
        {
            Id = id,
            IdSociete = societe,
            DateDepart = date,
            HeureDepart = heure
        };

        private static Destination Dest(string depart, string arrivee) => new()
        {
            VilleDepart = depart,
            VilleArrivee = arrivee
        };

        [Fact]
        public void EnsureCompatible_ReverseCitiesSameSociete_Ok()
        {
            var aller = Voyage(1, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));
            var retour = Voyage(2, 10, new DateTime(2026, 9, 3), TimeSpan.FromHours(14));

            AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                aller, retour, Dest("Kinshasa", "Lubumbashi"), Dest("lubumbashi", "KINSHASA"));
        }

        [Fact]
        public void EnsureCompatible_SameDayRetourAfterAller_Ok()
        {
            var aller = Voyage(1, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));
            var retour = Voyage(2, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(18));

            AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                aller, retour, Dest("A", "B"), Dest("B", "A"));
        }

        [Fact]
        public void EnsureCompatible_DifferentSociete_Throws()
        {
            var aller = Voyage(1, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));
            var retour = Voyage(2, 11, new DateTime(2026, 9, 3), TimeSpan.FromHours(8));

            Assert.Throws<InvalidOperationException>(() =>
                AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                    aller, retour, Dest("A", "B"), Dest("B", "A")));
        }

        [Fact]
        public void EnsureCompatible_CitiesNotMirror_Throws()
        {
            var aller = Voyage(1, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));
            var retour = Voyage(2, 10, new DateTime(2026, 9, 3), TimeSpan.FromHours(8));

            Assert.Throws<InvalidOperationException>(() =>
                AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                    aller, retour, Dest("Kinshasa", "Lubumbashi"), Dest("Goma", "Kisangani")));
        }

        [Fact]
        public void EnsureCompatible_RetourBeforeAller_Throws()
        {
            var aller = Voyage(1, 10, new DateTime(2026, 9, 3), TimeSpan.FromHours(8));
            var retour = Voyage(2, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));

            Assert.Throws<InvalidOperationException>(() =>
                AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                    aller, retour, Dest("A", "B"), Dest("B", "A")));
        }

        [Fact]
        public void EnsureCompatible_SameVoyage_Throws()
        {
            var v = Voyage(1, 10, new DateTime(2026, 9, 1), TimeSpan.FromHours(8));
            Assert.Throws<InvalidOperationException>(() =>
                AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                    v, v, Dest("A", "B"), Dest("B", "A")));
        }

        [Fact]
        public void EnsureSamePassengers_CountMismatch_Throws()
        {
            var passagers = new List<ReservationPassengerInputDto>
            {
                new() { NomComplet = "A", IdCategorieSiege = 1 }
            };
            Assert.Throws<InvalidOperationException>(() =>
                AllerRetourVoyageCompatibilityHelper.EnsureSamePassengers(passagers, 2));
        }

        [Fact]
        public void ClonePassagers_CopiesIdentityFields()
        {
            var source = new List<ReservationPassengerInputDto>
            {
                new()
                {
                    NomComplet = "Jean",
                    IdCategorieSiege = 3,
                    Telephone = "099",
                    Email = "a@b.c"
                }
            };
            var clone = AllerRetourVoyageCompatibilityHelper.ClonePassagers(source);
            Assert.Single(clone);
            Assert.Equal("Jean", clone[0].NomComplet);
            Assert.Equal(3, clone[0].IdCategorieSiege);
            Assert.Equal("099", clone[0].Telephone);
            Assert.NotSame(source[0], clone[0]);
        }
    }
}
