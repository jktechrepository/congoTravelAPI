using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Helpers.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementTicketEligibilityHelperTests
    {
        [Fact]
        public void Evaluate_returns_valide_for_issued_confirmed_ticket_in_window()
        {
            var utcNow = new DateTime(2026, 7, 3, 18, 0, 0, DateTimeKind.Utc);
            var session = BuildSession(utcNow.AddHours(-1), utcNow.AddHours(2));
            var reservation = BuildReservation(EvenementReservationStatus.CONFIRMED);
            var ticket = BuildTicket(EvenementTicketStatus.ISSUED);

            var result = EvenementTicketEligibilityHelper.Evaluate(ticket, reservation, session, utcNow);

            Assert.True(result.EntreeAutorisee);
            Assert.Equal("Valide", result.Statut);
            Assert.Equal(200, result.SuggestedHttpStatus);
        }

        [Fact]
        public void Evaluate_returns_deja_utilise_for_used_ticket()
        {
            var result = EvenementTicketEligibilityHelper.Evaluate(
                BuildTicket(EvenementTicketStatus.USED),
                BuildReservation(EvenementReservationStatus.CONFIRMED),
                BuildSession(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1)),
                DateTime.UtcNow);

            Assert.False(result.EntreeAutorisee);
            Assert.Equal("DejaUtilise", result.Statut);
            Assert.Equal(409, result.SuggestedHttpStatus);
        }

        [Fact]
        public void Evaluate_returns_session_inactive_when_not_published()
        {
            var utcNow = DateTime.UtcNow;
            var session = BuildSession(utcNow.AddHours(-1), utcNow.AddHours(2));
            session.Status = EvenementSessionStatus.Draft;

            var result = EvenementTicketEligibilityHelper.Evaluate(
                BuildTicket(EvenementTicketStatus.ISSUED),
                BuildReservation(EvenementReservationStatus.CONFIRMED),
                session,
                utcNow);

            Assert.Equal("SessionInactive", result.Statut);
            Assert.Equal(400, result.SuggestedHttpStatus);
        }

        [Fact]
        public void Evaluate_returns_hors_fenetre_before_start()
        {
            var start = new DateTime(2026, 7, 3, 20, 0, 0, DateTimeKind.Utc);
            var result = EvenementTicketEligibilityHelper.Evaluate(
                BuildTicket(EvenementTicketStatus.ISSUED),
                BuildReservation(EvenementReservationStatus.CONFIRMED),
                BuildSession(start, start.AddHours(3)),
                start.AddMinutes(-30));

            Assert.Equal("HorsFenetre", result.Statut);
            Assert.Equal(400, result.SuggestedHttpStatus);
        }

        [Fact]
        public void Evaluate_allows_entry_within_early_window_hours()
        {
            var start = new DateTime(2026, 7, 3, 20, 0, 0, DateTimeKind.Utc);
            var utcNow = start.AddHours(-2);
            var result = EvenementTicketEligibilityHelper.Evaluate(
                BuildTicket(EvenementTicketStatus.ISSUED),
                BuildReservation(EvenementReservationStatus.CONFIRMED),
                BuildSession(start, start.AddHours(3)),
                utcNow,
                heuresOuvertureAvantDebut: 3);

            Assert.True(result.EntreeAutorisee);
            Assert.Equal("Valide", result.Statut);
        }

        [Fact]
        public void Evaluate_rejects_when_before_early_window()
        {
            var start = new DateTime(2026, 7, 3, 20, 0, 0, DateTimeKind.Utc);
            var utcNow = start.AddHours(-4);
            var result = EvenementTicketEligibilityHelper.Evaluate(
                BuildTicket(EvenementTicketStatus.ISSUED),
                BuildReservation(EvenementReservationStatus.CONFIRMED),
                BuildSession(start, start.AddHours(3)),
                utcNow,
                heuresOuvertureAvantDebut: 3);

            Assert.Equal("HorsFenetre", result.Statut);
            Assert.Contains("ouverture à partir de", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void IsWithinEntryWindow_treats_unspecified_kind_as_utc()
        {
            var start = new DateTime(2026, 7, 3, 18, 0, 0, DateTimeKind.Unspecified);
            var session = BuildSession(start, start.AddHours(4));
            var now = new DateTime(2026, 7, 3, 16, 0, 0, DateTimeKind.Utc);

            Assert.True(EvenementTicketEligibilityHelper.IsWithinEntryWindow(session, now, heuresOuvertureAvantDebut: 3));
            Assert.False(EvenementTicketEligibilityHelper.IsWithinEntryWindow(session, now, heuresOuvertureAvantDebut: 0));
        }

        [Fact]
        public void IsWithinEntryWindow_n0_matches_exact_start_behavior()
        {
            var start = new DateTime(2026, 7, 3, 18, 0, 0, DateTimeKind.Utc);
            var session = BuildSession(start, start.AddHours(2));

            Assert.False(EvenementTicketEligibilityHelper.IsWithinEntryWindow(
                session, start.AddMinutes(-1), heuresOuvertureAvantDebut: 0));
            Assert.True(EvenementTicketEligibilityHelper.IsWithinEntryWindow(
                session, start, heuresOuvertureAvantDebut: 0));
        }

        private static EvenementTicket BuildTicket(EvenementTicketStatus status) =>
            new() { IdEvenementTicket = 1, TicketCode = "EVT-TKT-001", Status = status };

        private static EvenementReservation BuildReservation(EvenementReservationStatus status) =>
            new()
            {
                IdEvenementReservation = 10,
                ReferenceReservation = "EVT-RES-TEST",
                Status = status,
                CustomerRef = "CUST-1"
            };

        private static EvenementSession BuildSession(DateTime start, DateTime end) =>
            new()
            {
                IdEvenementSession = 5,
                CodeSession = "GALA-1",
                Libelle = "Gala",
                StartAtUtc = start,
                EndAtUtc = end,
                Status = EvenementSessionStatus.Published
            };
    }
}
