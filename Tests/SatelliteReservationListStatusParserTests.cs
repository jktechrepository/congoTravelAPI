using CongoTravel.Helpers;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Models.SiteTouristique.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class SatelliteReservationListStatusParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Blank_defaults_to_CONFIRMED(string? status)
        {
            var ok = SatelliteReservationListStatusParser.TryParse(
                status,
                EvenementReservationStatus.CONFIRMED,
                out var parsed,
                out var error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(EvenementReservationStatus.CONFIRMED, parsed);
        }

        [Theory]
        [InlineData("ALL")]
        [InlineData("all")]
        [InlineData(" All ")]
        public void ALL_disables_status_filter(string status)
        {
            var ok = SatelliteReservationListStatusParser.TryParse(
                status,
                EvenementReservationStatus.CONFIRMED,
                out var parsed,
                out var error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Null(parsed);
        }

        [Theory]
        [InlineData("HOLD", EvenementReservationStatus.HOLD)]
        [InlineData("confirmed", EvenementReservationStatus.CONFIRMED)]
        [InlineData("CANCELLED", EvenementReservationStatus.CANCELLED)]
        [InlineData("EXPIRED", EvenementReservationStatus.EXPIRED)]
        public void Explicit_enum_is_parsed(string status, EvenementReservationStatus expected)
        {
            var ok = SatelliteReservationListStatusParser.TryParse(
                status,
                EvenementReservationStatus.CONFIRMED,
                out var parsed,
                out var error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal(expected, parsed);
        }

        [Fact]
        public void Invalid_status_returns_error_mentioning_ALL()
        {
            var ok = SatelliteReservationListStatusParser.TryParse(
                "BOGUS",
                EvenementReservationStatus.CONFIRMED,
                out var parsed,
                out var error);

            Assert.False(ok);
            Assert.Null(parsed);
            Assert.Contains("ALL", error);
        }

        [Fact]
        public void Works_for_Restaurant_and_SiteTouristique_enums()
        {
            Assert.True(SatelliteReservationListStatusParser.TryParse(
                null,
                RestaurantReservationStatus.CONFIRMED,
                out var restaurant,
                out _));
            Assert.Equal(RestaurantReservationStatus.CONFIRMED, restaurant);

            Assert.True(SatelliteReservationListStatusParser.TryParse(
                "ALL",
                SiteTouristiqueReservationStatus.CONFIRMED,
                out var site,
                out _));
            Assert.Null(site);
        }
    }
}
