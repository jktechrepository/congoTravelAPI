using CongoTravel.Helpers;
using CongoTravel.Models.DTOs;
using Xunit;

namespace CongoTravel.Tests
{
    public class VoyageListeDateFilterTests
    {
        [Fact]
        public void Resolve_jour_default_uses_today_when_no_date()
        {
            var today = DateTime.Today;
            var (debut, fin) = VoyageListeDateFilter.Resolve(null, VoyageListePeriode.Jour);
            Assert.Equal(today, debut);
            Assert.Equal(today, fin);
        }

        [Fact]
        public void Resolve_jour_uses_single_day()
        {
            var reference = new DateTime(2026, 5, 15);
            var (debut, fin) = VoyageListeDateFilter.Resolve(reference, VoyageListePeriode.Jour);
            Assert.Equal(reference.Date, debut);
            Assert.Equal(reference.Date, fin);
        }

        [Fact]
        public void Resolve_hebdomadaire_covers_monday_to_sunday()
        {
            var wednesday = new DateTime(2026, 5, 13);
            var (debut, fin) = VoyageListeDateFilter.Resolve(wednesday, VoyageListePeriode.Hebdomadaire);
            Assert.Equal(new DateTime(2026, 5, 11), debut);
            Assert.Equal(new DateTime(2026, 5, 17), fin);
        }

        [Fact]
        public void Resolve_mensuel_covers_full_calendar_month()
        {
            var reference = new DateTime(2026, 5, 15);
            var (debut, fin) = VoyageListeDateFilter.Resolve(reference, VoyageListePeriode.Mensuel);
            Assert.Equal(new DateTime(2026, 5, 1), debut);
            Assert.Equal(new DateTime(2026, 5, 31), fin);
        }

        [Fact]
        public void Resolve_tout_returns_null_dates_and_ignores_reference_date()
        {
            var reference = new DateTime(2026, 5, 15);
            var (debut, fin) = VoyageListeDateFilter.Resolve(reference, VoyageListePeriode.Tout);
            Assert.Null(debut);
            Assert.Null(fin);
        }
    }
}
