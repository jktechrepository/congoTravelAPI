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
    public class EvenementTicketServiceUseTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementTicketService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementTicketService>.Instance);

        [Fact]
        public async Task UseTicketAsync_marks_issued_ticket_as_used()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_marks_issued_ticket_as_used));
            var (idSociete, ticketCode) = await SeedIssuedTicketAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.UseTicketAsync(ticketCode, idSociete);

            Assert.Equal(200, result.HttpStatusCode);
            Assert.NotNull(result.Response);
            Assert.False(result.Response!.AlreadyUsed);
            Assert.Equal("USED", result.Response.Ticket.Status);
            Assert.NotNull(result.Response.Ticket.UsedAtUtc);

            var persisted = await ctx.EvenementTickets.SingleAsync();
            Assert.Equal(EvenementTicketStatus.USED, persisted.Status);
            Assert.NotNull(persisted.UsedAtUtc);
        }

        [Fact]
        public async Task UseTicketAsync_is_idempotent_when_already_used()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_is_idempotent_when_already_used));
            var (idSociete, ticketCode) = await SeedIssuedTicketAsync(ctx);
            var service = CreateService(ctx);

            var first = await service.UseTicketAsync(ticketCode, idSociete);
            var second = await service.UseTicketAsync(ticketCode, idSociete);

            Assert.False(first.Response!.AlreadyUsed);
            Assert.True(second.Response!.AlreadyUsed);
            Assert.Equal(first.Response.Ticket.IdEvenementTicket, second.Response.Ticket.IdEvenementTicket);
        }

        [Fact]
        public async Task UseTicketAsync_returns_404_for_unknown_code()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_returns_404_for_unknown_code));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.UseTicketAsync("EVT-TKT-MISSING", idSociete);

            Assert.Equal(404, result.HttpStatusCode);
            Assert.Null(result.Response);
        }

        [Fact]
        public async Task UseTicketAsync_returns_409_for_void_ticket()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_returns_409_for_void_ticket));
            var (idSociete, ticketCode) = await SeedIssuedTicketAsync(ctx, EvenementTicketStatus.VOID);
            var service = CreateService(ctx);

            var result = await service.UseTicketAsync(ticketCode, idSociete);

            Assert.Equal(409, result.HttpStatusCode);
            Assert.Null(result.Response);
        }

        [Fact]
        public async Task UseTicketAsync_returns_400_when_session_not_started()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_returns_400_when_session_not_started));
            var utcNow = DateTime.UtcNow;
            var (idSociete, ticketCode) = await SeedIssuedTicketAsync(
                ctx,
                sessionStart: utcNow.AddHours(2),
                sessionEnd: utcNow.AddHours(5));
            var service = CreateService(ctx);

            var result = await service.UseTicketAsync(ticketCode, idSociete);

            Assert.Equal(400, result.HttpStatusCode);
            Assert.Contains("Entrée pas encore ouverte", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<(int IdSociete, string TicketCode)> SeedIssuedTicketAsync(
            CongoTravelDbContext ctx,
            EvenementTicketStatus ticketStatus = EvenementTicketStatus.ISSUED,
            DateTime? sessionStart = null,
            DateTime? sessionEnd = null)
        {
            var idSociete = await SeedSocieteAsync(ctx);
            var utcNow = DateTime.UtcNow;
            var start = sessionStart ?? utcNow.AddHours(-1);
            var end = sessionEnd ?? utcNow.AddHours(4);

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                CodeSession = "USE-TEST",
                Libelle = "Use test",
                StartAtUtc = start,
                EndAtUtc = end,
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
                PrixUnitaire = 15m,
                CodeDevise = "CDF"
            };

            var reservation = new EvenementReservation
            {
                IdSociete = idSociete,
                IdEvenementSession = session.IdEvenementSession,
                ReferenceReservation = "EVT-RES-USE",
                Status = EvenementReservationStatus.CONFIRMED,
                MontantSousTotal = 15m,
                CodeDevise = "CDF",
                DateCreation = utcNow,
                Lines = { line }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var ticketCode = "EVT-TKT-USE-001";
            ctx.EvenementTickets.Add(new EvenementTicket
            {
                IdEvenementReservationLine = line.IdEvenementReservationLine,
                TicketCode = ticketCode,
                Status = ticketStatus,
                IssuedAtUtc = utcNow
            });
            await ctx.SaveChangesAsync();

            return (idSociete, ticketCode);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Use Societe", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
