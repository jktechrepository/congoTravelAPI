using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementTicketReadTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementTicketService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementTicketService>.Instance);

        [Fact]
        public async Task GetByIdAsync_returns_detail_for_own_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_detail_for_own_societe));
            var (idSociete, ticketId, _) = await SeedConfirmedTicketAsync(ctx, used: false);
            var service = CreateService(ctx);

            var result = await service.GetByIdAsync(ticketId, idSociete);

            Assert.NotNull(result);
            Assert.Equal("EVT-TKT-001-READ", result!.TicketCode);
            Assert.Equal("EVT-RES-READ", result.ReferenceReservation);
            Assert.Equal("GALA-READ", result.CodeSession);
            Assert.Equal("CUST-READ", result.CustomerRef);
        }

        [Fact]
        public async Task GetByIdAsync_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_null_for_other_societe));
            var (_, ticketId, _) = await SeedConfirmedTicketAsync(ctx, used: false);
            var otherSociete = await SeedSocieteAsync(ctx, "Other");
            var service = CreateService(ctx);

            var result = await service.GetByIdAsync(ticketId, otherSociete);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByTicketCodeAsync_finds_ticket_with_trimmed_code()
        {
            await using var ctx = BuildDb(nameof(GetByTicketCodeAsync_finds_ticket_with_trimmed_code));
            var (idSociete, _, ticketCode) = await SeedConfirmedTicketAsync(ctx, used: false);
            var service = CreateService(ctx);

            var result = await service.GetByTicketCodeAsync($"  {ticketCode}  ", idSociete);

            Assert.NotNull(result);
            Assert.Equal(ticketCode, result!.TicketCode);
        }

        [Fact]
        public async Task ListAsync_filters_by_status_and_reservation()
        {
            await using var ctx = BuildDb(nameof(ListAsync_filters_by_status_and_reservation));
            var (idSociete, ticketId, _) = await SeedConfirmedTicketAsync(ctx, used: false);
            var reservationId = await ctx.EvenementTickets
                .Where(t => t.IdEvenementTicket == ticketId)
                .Select(t => t.ReservationLine!.IdEvenementReservation)
                .SingleAsync();

            var usedTicket = new EvenementTicket
            {
                IdEvenementReservationLine = await ctx.EvenementReservationLines.Select(l => l.IdEvenementReservationLine).SingleAsync(),
                TicketCode = "EVT-TKT-USED-READ",
                Status = EvenementTicketStatus.USED,
                IssuedAtUtc = DateTime.UtcNow,
                UsedAtUtc = DateTime.UtcNow
            };
            ctx.EvenementTickets.Add(usedTicket);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var issued = await service.ListAsync(
                idSociete,
                new Models.DTOs.Evenement.EvenementTicketListFilter
                {
                    Status = EvenementTicketStatus.ISSUED,
                    IdEvenementReservation = reservationId
                });

            Assert.Single(issued);
            Assert.Equal(ticketId, issued[0].IdEvenementTicket);
        }

        [Fact]
        public async Task ListByDateRangeAsync_returns_tickets_in_range()
        {
            await using var ctx = BuildDb(nameof(ListByDateRangeAsync_returns_tickets_in_range));
            var (idSociete, _, _) = await SeedConfirmedTicketAsync(ctx, used: false);

            var oldTicket = new EvenementTicket
            {
                IdEvenementReservationLine = await ctx.EvenementReservationLines.Select(l => l.IdEvenementReservationLine).SingleAsync(),
                TicketCode = "EVT-TKT-OLD",
                Status = EvenementTicketStatus.VOID,
                IssuedAtUtc = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc)
            };
            ctx.EvenementTickets.Add(oldTicket);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var recent = await service.ListByDateRangeAsync(
                DateTime.UtcNow.Date.AddDays(-1),
                DateTime.UtcNow.Date.AddDays(1),
                idSociete);
            var oldOnly = await service.ListByDateRangeAsync(
                new DateTime(2026, 1, 5),
                new DateTime(2026, 1, 5),
                idSociete);

            Assert.Single(recent);
            Assert.Equal("ISSUED", recent[0].Status);
            Assert.Single(oldOnly);
            Assert.Equal("EVT-TKT-OLD", oldOnly[0].TicketCode);
        }

        [Fact]
        public async Task ListBySocieteAndReservationAsync_returns_null_when_reservation_not_in_societe()
        {
            await using var ctx = BuildDb(nameof(ListBySocieteAndReservationAsync_returns_null_when_reservation_not_in_societe));
            var (idSociete, _, _) = await SeedConfirmedTicketAsync(ctx, used: false);
            var reservationId = await ctx.EvenementReservations.Select(r => r.IdEvenementReservation).SingleAsync();
            var otherSociete = await SeedSocieteAsync(ctx, "Other");
            var service = CreateService(ctx);

            var invalid = await service.ListBySocieteAndReservationAsync(otherSociete, reservationId);
            var valid = await service.ListBySocieteAndReservationAsync(idSociete, reservationId);

            Assert.Null(invalid);
            Assert.NotNull(valid);
            Assert.Single(valid!);
        }

        private static async Task<(int IdSociete, int IdTicket, string TicketCode)> SeedConfirmedTicketAsync(
            CongoTravelDbContext ctx,
            bool used)
        {
            var idSociete = await SeedSocieteAsync(ctx);
            var utcNow = DateTime.UtcNow;

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                CodeSession = "GALA-READ",
                Libelle = "Gala read test",
                StartAtUtc = utcNow.AddHours(-2),
                EndAtUtc = utcNow.AddHours(4),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = utcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var line = new EvenementReservationLine
            {
                LineType = EvenementReservationLineType.GlobalQuota,
                Quantite = 1,
                PrixUnitaire = 20m,
                CodeDevise = "USD"
            };

            var reservation = new EvenementReservation
            {
                IdSociete = idSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-RES-READ",
                CustomerRef = "CUST-READ",
                Status = EvenementReservationStatus.CONFIRMED,
                MontantSousTotal = 20m,
                CodeDevise = "USD",
                DateCreation = utcNow,
                Lines = { line }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var ticketCode = "EVT-TKT-001-READ";
            var ticket = new EvenementTicket
            {
                IdEvenementReservationLine = line.IdEvenementReservationLine,
                TicketCode = ticketCode,
                Status = used ? EvenementTicketStatus.USED : EvenementTicketStatus.ISSUED,
                IssuedAtUtc = utcNow,
                UsedAtUtc = used ? utcNow : null
            };
            ctx.EvenementTickets.Add(ticket);
            await ctx.SaveChangesAsync();

            return (idSociete, ticket.IdEvenementTicket, ticketCode);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx, string nom = "Ticket Read Societe")
        {
            var societe = new Societe { Nom = nom, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
