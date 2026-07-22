using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementTicketServiceCheckTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementTicketService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementTicketService>.Instance);

        [Fact]
        public async Task CheckTicketAsync_returns_valide_for_confirmed_ticket()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_returns_valide_for_confirmed_ticket));
            var (idSociete, ticketCode) = await SeedConfirmedTicketAsync(ctx, used: false);
            var service = CreateService(ctx);

            var result = await service.CheckTicketAsync(ticketCode, idSociete);

            Assert.Equal(200, result.HttpStatusCode);
            Assert.True(result.Response.EntreeAutorisee);
            Assert.Equal("Valide", result.Response.Statut);
            Assert.Equal("GALA-TEST", result.Response.CodeSession);
            Assert.Equal("CUST-42", result.Response.CustomerRef);
        }

        [Fact]
        public async Task CheckTicketAsync_returns_409_for_used_ticket()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_returns_409_for_used_ticket));
            var (idSociete, ticketCode) = await SeedConfirmedTicketAsync(ctx, used: true);
            var service = CreateService(ctx);

            var result = await service.CheckTicketAsync(ticketCode, idSociete);

            Assert.Equal(409, result.HttpStatusCode);
            Assert.Equal("DejaUtilise", result.Response.Statut);
        }

        [Fact]
        public async Task CheckTicketAsync_returns_404_for_unknown_code()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_returns_404_for_unknown_code));
            var idSociete = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.CheckTicketAsync("EVT-TKT-UNKNOWN", idSociete);

            Assert.Equal(404, result.HttpStatusCode);
            Assert.Equal("NonReconnu", result.Response.Statut);
        }

        [Fact]
        public async Task CheckTicketAsync_masks_ticket_from_other_societe_as_unknown()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_masks_ticket_from_other_societe_as_unknown));
            var (_, ticketCode) = await SeedConfirmedTicketAsync(ctx, used: false);
            var otherSociete = await SeedSocieteAsync(ctx, "Other");
            var service = CreateService(ctx);

            var result = await service.CheckTicketAsync(ticketCode, otherSociete);

            Assert.Equal(404, result.HttpStatusCode);
            Assert.Equal("NonReconnu", result.Response.Statut);
        }

        [Fact]
        public async Task CheckTicketAsync_normalizes_ticket_code()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_normalizes_ticket_code));
            var (idSociete, ticketCode) = await SeedConfirmedTicketAsync(ctx, used: false);
            var service = CreateService(ctx);

            var result = await service.CheckTicketAsync($"  {ticketCode}  ", idSociete);

            Assert.Equal("Valide", result.Response.Statut);
        }

        private static async Task<(int IdSociete, string TicketCode)> SeedConfirmedTicketAsync(
            CongoTravelDbContext ctx,
            bool used)
        {
            var idSociete = await SeedSocieteAsync(ctx);
            var utcNow = DateTime.UtcNow;

            var session = new EvenementSession
            {
                IdSociete = idSociete,
                CodeSession = "GALA-TEST",
                Libelle = "Gala test",
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
                ReferenceReservation = "EVT-RES-CHECK",
                CustomerRef = "CUST-42",
                Status = EvenementReservationStatus.CONFIRMED,
                MontantSousTotal = 20m,
                CodeDevise = "USD",
                DateCreation = utcNow,
                Lines = { line }
            };
            ctx.EvenementReservations.Add(reservation);
            await ctx.SaveChangesAsync();

            var ticketCode = "EVT-TKT-001-CHECK";
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

            return (idSociete, ticketCode);
        }

        private static async Task<int> SeedSocieteAsync(CongoTravelDbContext ctx, string nom = "Ticket Societe")
        {
            var societe = new Societe { Nom = nom, DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
