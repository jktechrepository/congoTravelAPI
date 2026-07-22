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
