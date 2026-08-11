using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionSalesEligibilityHelperTests
    {
        [Fact]
        public void CanSell_true_when_published_and_before_start()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(1),
                EndAtUtc = DateTime.UtcNow.AddHours(5)
            };

            Assert.True(EvenementSessionSalesEligibilityHelper.CanSell(session, DateTime.UtcNow));
        }

        [Fact]
        public void CanSell_true_when_started_but_before_end()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(-1),
                EndAtUtc = DateTime.UtcNow.AddHours(3)
            };

            Assert.True(EvenementSessionSalesEligibilityHelper.CanSell(session, DateTime.UtcNow));
        }

        [Fact]
        public void CanSell_false_when_after_end()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(-5),
                EndAtUtc = DateTime.UtcNow.AddHours(-1)
            };

            Assert.False(EvenementSessionSalesEligibilityHelper.CanSell(session, DateTime.UtcNow));
        }

        [Fact]
        public void CanSell_false_when_no_end_and_past_start_plus_24h()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(-25),
                EndAtUtc = null
            };

            Assert.False(EvenementSessionSalesEligibilityHelper.CanSell(session, DateTime.UtcNow));
        }

        [Fact]
        public void CanSell_true_when_no_end_and_within_24h_after_start()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(-2),
                EndAtUtc = null
            };

            Assert.True(EvenementSessionSalesEligibilityHelper.CanSell(session, DateTime.UtcNow));
        }

        [Fact]
        public void EnsureCanSell_throws_when_session_ended()
        {
            var session = new EvenementSession
            {
                Status = EvenementSessionStatus.Published,
                StartAtUtc = DateTime.UtcNow.AddHours(-3),
                EndAtUtc = DateTime.UtcNow.AddMinutes(-5)
            };

            var ex = Assert.Throws<InvalidOperationException>(() =>
                EvenementSessionSalesEligibilityHelper.EnsureCanSell(session, DateTime.UtcNow));
            Assert.Contains("Vente fermée", ex.Message);
            Assert.Contains("terminée", ex.Message);
        }

        [Fact]
        public void ResolveSalesEndUtc_uses_end_or_start_plus_24h()
        {
            var start = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 8, 1, 18, 0, 0, DateTimeKind.Utc);

            var withEnd = new EvenementSession { StartAtUtc = start, EndAtUtc = end };
            Assert.Equal(end, EvenementSessionSalesEligibilityHelper.ResolveSalesEndUtc(withEnd));

            var withoutEnd = new EvenementSession { StartAtUtc = start, EndAtUtc = null };
            Assert.Equal(start.AddHours(24), EvenementSessionSalesEligibilityHelper.ResolveSalesEndUtc(withoutEnd));
        }
    }
}
