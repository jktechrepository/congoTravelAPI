using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Services.SiteTouristique;
using Xunit;

namespace CongoTravel.Tests
{
    public class SiteTouristiqueTicketLogoTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ListAsync_includes_logo_societe()
        {
            await using var ctx = BuildDb(nameof(ListAsync_includes_logo_societe));
            var seeded = await SeedConfirmedTicketAsync(ctx, "https://cdn.example/st-logo-list.png");
            var service = new SiteTouristiqueTicketService(ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var tickets = await service.ListAsync(seeded.IdSociete);

            var item = Assert.Single(tickets);
            Assert.Equal(seeded.TicketCode, item.TicketCode);
            Assert.Equal("https://cdn.example/st-logo-list.png", item.LogoSociete);
        }

        [Fact]
        public async Task GetByIdAsync_and_GetByTicketCodeAsync_include_logo_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_and_GetByTicketCodeAsync_include_logo_societe));
            var seeded = await SeedConfirmedTicketAsync(ctx, "https://cdn.example/st-logo-detail.png");
            var service = new SiteTouristiqueTicketService(ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var byId = await service.GetByIdAsync(seeded.TicketId, seeded.IdSociete);
            var byCode = await service.GetByTicketCodeAsync(seeded.TicketCode, seeded.IdSociete);

            Assert.NotNull(byId);
            Assert.NotNull(byCode);
            Assert.Equal("https://cdn.example/st-logo-detail.png", byId!.LogoSociete);
            Assert.Equal("https://cdn.example/st-logo-detail.png", byCode!.LogoSociete);
        }

        [Fact]
        public async Task CheckTicketAsync_includes_logo_societe()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_includes_logo_societe));
            var seeded = await SeedConfirmedTicketAsync(ctx, "https://cdn.example/st-logo-check.png");
            var service = new SiteTouristiqueTicketService(ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var result = await service.CheckTicketAsync(seeded.TicketCode, seeded.IdSociete);

            Assert.NotNull(result.Response);
            Assert.Equal("https://cdn.example/st-logo-check.png", result.Response.LogoSociete);
        }

        [Fact]
        public async Task Ticket_get_responses_return_null_logo_when_societe_has_no_logo()
        {
            await using var ctx = BuildDb(nameof(Ticket_get_responses_return_null_logo_when_societe_has_no_logo));
            var seeded = await SeedConfirmedTicketAsync(ctx, null);
            var service = new SiteTouristiqueTicketService(ctx, NullLogger<SiteTouristiqueTicketService>.Instance);

            var list = await service.ListAsync(seeded.IdSociete);
            var detail = await service.GetByIdAsync(seeded.TicketId, seeded.IdSociete);
            var check = await service.CheckTicketAsync(seeded.TicketCode, seeded.IdSociete);

            Assert.Null(Assert.Single(list).LogoSociete);
            Assert.NotNull(detail);
            Assert.Null(detail!.LogoSociete);
            Assert.Null(check.Response.LogoSociete);
        }

        private static async Task<(int IdSociete, int TicketId, string TicketCode)> SeedConfirmedTicketAsync(
            CongoTravelDbContext ctx,
            string? logoSociete)
        {
            var (idSociete, idSite) = await SiteTouristiqueTestFactories.SeedSocieteWithSiteAsync(ctx, "ST Ticket Logo");
            var societe = await ctx.Societes.FirstAsync(s => s.IdSociete == idSociete);
            societe.Logo = logoSociete;
            await ctx.SaveChangesAsync();

            var lieuService = SiteTouristiqueTestFactories.CreateLieuService(ctx);
            var journeeService = new SiteTouristiqueJourneeService(ctx, NullLogger<SiteTouristiqueJourneeService>.Instance);
            var holdService = SiteTouristiqueTestFactories.CreateHoldService(ctx);
            var paymentService = SiteTouristiqueTestFactories.CreatePaymentService(ctx);

            var lieu = await lieuService.CreateDraftAsync(new SiteTouristiqueCreateLieuRequestDto
            {
                CodeLieu = $"LOGO-{Guid.NewGuid():N}"[..10],
                Nom = "Parc Logo",
                IdSite = idSite
            }, idSociete);
            await lieuService.PublishAsync(lieu.IdSiteTouristique, idSociete);

            var journee = await journeeService.CreateDraftAsync(new SiteTouristiqueCreateJourneeRequestDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                DateVisite = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                InventoryMode = "GlobalQuota",
                CodeDevise = "USD",
                GlobalQuota = new SiteTouristiqueCreateJourneeGlobalQuotaDto
                {
                    CapaciteTotale = 10,
                    PrixUnitaire = 15m
                }
            }, idSociete);
            await journeeService.PublishAsync(journee.IdSiteTouristiqueJournee, idSociete);

            var hold = await holdService.CreateHoldAsync(
                journee.IdSiteTouristiqueJournee,
                idSociete,
                new SiteTouristiqueHoldRequestDto
                {
                    CustomerRef = "TICKET-LOGO",
                    Items = new List<SiteTouristiqueHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            await paymentService.ConfirmPaymentAsync(
                hold.IdSiteTouristiqueReservation,
                idSociete,
                new SiteTouristiqueConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var ticket = await ctx.SiteTouristiqueTickets.AsNoTracking().FirstAsync();
            return (idSociete, ticket.IdSiteTouristiqueTicket, ticket.TicketCode);
        }
    }
}
