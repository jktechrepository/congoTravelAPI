using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant;
using Xunit;

namespace CongoTravel.Tests
{
    public class RestaurantTicketTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public void AddRestaurantReservations_registers_ticket_service()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<CongoTravelDbContext>(options =>
                options.UseInMemoryDatabase(nameof(AddRestaurantReservations_registers_ticket_service)));
            services.AddScoped<CongoTravel.Services.Repositories.IConfigSocieteRepository, CongoTravel.Services.ConfigSocieteService>();
            services.AddRestaurantReservations();

            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IRestaurantTicketService>());
        }

        [Fact]
        public async Task ConfirmHoldAndEmitTicketsAsync_emits_one_ticket_per_quantity()
        {
            await using var ctx = BuildDb(nameof(ConfirmHoldAndEmitTicketsAsync_emits_one_ticket_per_quantity));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "EMIT", capacite: 20);

            var hold = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 3 } }
                });

            var confirmed = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto
                {
                    MethodePaiement = "CASH",
                    ReferenceTransaction = "CAISSE-EMIT"
                });

            Assert.Equal("CONFIRMED", confirmed.Reservation.Status);
            Assert.Equal(3, confirmed.Reservation.Tickets.Count);
            Assert.All(confirmed.Reservation.Tickets, t => Assert.Equal("ISSUED", t.Status));
            Assert.Equal(3, await ctx.RestaurantTickets.CountAsync());
            Assert.All(
                await ctx.RestaurantTickets.ToListAsync(),
                t => Assert.StartsWith("REST-TKT-", t.TicketCode));
        }

        [Fact]
        public async Task CheckTicketAsync_valide_within_window_and_rejects_hors_fenetre_used_void()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_valide_within_window_and_rejects_hors_fenetre_used_void));
            var (idSociete, ticketCode, idTicket) = await SeedConfirmedTicketAsync(ctx, quantity: 1);
            await SetSocieteLogoAsync(ctx, idSociete, "https://cdn.example/restaurant-logo.png");

            var service = RestaurantTestFactories.CreateTicketService(ctx);

            var ok = await service.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, ok.HttpStatusCode);
            Assert.True(ok.Response.EntreeAutorisee);
            Assert.Equal("Valide", ok.Response.Statut);
            Assert.Equal("https://cdn.example/restaurant-logo.png", ok.Response.LogoSociete);

            var ticket = await ctx.RestaurantTickets.SingleAsync(t => t.IdRestaurantTicket == idTicket);
            ticket.Status = RestaurantTicketStatus.USED;
            ticket.UsedAtUtc = DateTime.UtcNow;
            await ctx.SaveChangesAsync();

            var used = await service.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(409, used.HttpStatusCode);
            Assert.Equal("DejaUtilise", used.Response.Statut);

            ticket.Status = RestaurantTicketStatus.VOID;
            ticket.UsedAtUtc = null;
            await ctx.SaveChangesAsync();

            var voided = await service.CheckTicketAsync(ticketCode, idSociete);
            Assert.Equal(409, voided.HttpStatusCode);
            Assert.Equal("Invalide", voided.Response.Statut);

            var unknown = await service.CheckTicketAsync("REST-TKT-UNKNOWN", idSociete);
            Assert.Equal(404, unknown.HttpStatusCode);
            Assert.Equal("NonReconnu", unknown.Response.Statut);
            Assert.Null(unknown.Response.LogoSociete);
        }

        [Fact]
        public async Task CheckTicketAsync_rejects_outside_entry_window()
        {
            await using var ctx = BuildDb(nameof(CheckTicketAsync_rejects_outside_entry_window));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "HORS", capacite: 10);

            var creneau = await ctx.RestaurantCreneaux.SingleAsync(c => c.IdRestaurantCreneau == idCreneau);
            creneau.StartAtUtc = DateTime.UtcNow.AddHours(5);
            creneau.EndAtUtc = DateTime.UtcNow.AddHours(7);
            await ctx.SaveChangesAsync();

            var hold = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } }
                });

            var confirmed = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var ticketCode = confirmed.Reservation.Tickets.Single().TicketCode;
            var result = await RestaurantTestFactories.CreateTicketService(ctx)
                .CheckTicketAsync(ticketCode, idSociete);

            Assert.Equal(400, result.HttpStatusCode);
            Assert.Equal("HorsFenetre", result.Response.Statut);
            Assert.False(result.Response.EntreeAutorisee);
        }

        [Fact]
        public async Task UseTicketAsync_is_idempotent_and_marks_used()
        {
            await using var ctx = BuildDb(nameof(UseTicketAsync_is_idempotent_and_marks_used));
            var (idSociete, ticketCode, _) = await SeedConfirmedTicketAsync(ctx, quantity: 1);
            var service = RestaurantTestFactories.CreateTicketService(ctx);

            var first = await service.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, first.HttpStatusCode);
            Assert.NotNull(first.Response);
            Assert.False(first.Response!.AlreadyUsed);
            Assert.Equal("USED", first.Response.Ticket.Status);

            var second = await service.UseTicketAsync(ticketCode, idSociete);
            Assert.Equal(200, second.HttpStatusCode);
            Assert.True(second.Response!.AlreadyUsed);
        }

        [Fact]
        public async Task Get_ticket_reads_include_logo_societe()
        {
            await using var ctx = BuildDb(nameof(Get_ticket_reads_include_logo_societe));
            var (idSociete, ticketCode, idTicket) = await SeedConfirmedTicketAsync(ctx, quantity: 1);
            await SetSocieteLogoAsync(ctx, idSociete, "https://cdn.example/restaurant-ticket.png");

            var reservationId = await ctx.RestaurantTickets
                .Where(t => t.IdRestaurantTicket == idTicket)
                .Select(t => t.ReservationLine!.IdRestaurantReservation)
                .SingleAsync();

            var service = RestaurantTestFactories.CreateTicketService(ctx);
            var byId = await service.GetByIdAsync(idTicket, idSociete);
            var byCode = await service.GetByTicketCodeAsync(ticketCode, idSociete);
            var list = await service.ListAsync(idSociete);
            var byReservation = await service.ListByReservationAsync(reservationId, idSociete);

            Assert.NotNull(byId);
            Assert.Equal("https://cdn.example/restaurant-ticket.png", byId!.LogoSociete);
            Assert.NotNull(byCode);
            Assert.Equal("https://cdn.example/restaurant-ticket.png", byCode!.LogoSociete);
            Assert.Single(list);
            Assert.Equal("https://cdn.example/restaurant-ticket.png", list[0].LogoSociete);
            Assert.Single(byReservation);
            Assert.Equal("https://cdn.example/restaurant-ticket.png", byReservation[0].LogoSociete);
        }

        [Fact]
        public async Task Reservation_get_by_id_includes_logo_societe_on_nested_tickets()
        {
            await using var ctx = BuildDb(nameof(Reservation_get_by_id_includes_logo_societe_on_nested_tickets));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "READ", capacite: 20);
            await SetSocieteLogoAsync(ctx, idSociete, "https://cdn.example/restaurant-reservation.png");

            var hold = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } }
                });

            var confirmed = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var reservation = await RestaurantTestFactories.CreateReservationService(ctx)
                .GetByIdAsync(confirmed.Reservation.IdRestaurantReservation, idSociete);

            Assert.NotNull(reservation);
            Assert.Equal(2, reservation!.Tickets.Count);
            Assert.All(
                reservation.Tickets,
                t => Assert.Equal("https://cdn.example/restaurant-reservation.png", t.LogoSociete));
        }

        [Fact]
        public async Task Ticket_get_responses_return_null_logo_when_societe_has_no_logo()
        {
            await using var ctx = BuildDb(nameof(Ticket_get_responses_return_null_logo_when_societe_has_no_logo));
            var (idSociete, ticketCode, idTicket) = await SeedConfirmedTicketAsync(ctx, quantity: 1);
            var service = RestaurantTestFactories.CreateTicketService(ctx);

            var detail = await service.GetByIdAsync(idTicket, idSociete);
            var byCode = await service.GetByTicketCodeAsync(ticketCode, idSociete);
            var check = await service.CheckTicketAsync(ticketCode, idSociete);

            Assert.NotNull(detail);
            Assert.Null(detail!.LogoSociete);
            Assert.NotNull(byCode);
            Assert.Null(byCode!.LogoSociete);
            Assert.Null(check.Response.LogoSociete);
        }

        [Fact]
        public async Task Cancel_confirmed_voids_issued_tickets_and_blocks_if_used()
        {
            await using var ctx = BuildDb(nameof(Cancel_confirmed_voids_issued_tickets_and_blocks_if_used));
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, "VOID", capacite: 10);

            var hold = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 2 } }
                });

            var confirmed = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var cancel = await RestaurantTestFactories.CreateReservationService(ctx).CancelAsync(
                confirmed.Reservation.IdRestaurantReservation,
                idSociete);

            Assert.False(cancel.AlreadyCancelled);
            Assert.Equal(2, cancel.TicketsVoided);
            Assert.All(await ctx.RestaurantTickets.ToListAsync(), t => Assert.Equal(RestaurantTicketStatus.VOID, t.Status));

            // second reservation with USED ticket cannot cancel
            var hold2 = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = 1 } }
                });
            var confirmed2 = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold2.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var usedTicket = await ctx.RestaurantTickets
                .SingleAsync(t => t.TicketCode == confirmed2.Reservation.Tickets.Single().TicketCode);
            usedTicket.Status = RestaurantTicketStatus.USED;
            usedTicket.UsedAtUtc = DateTime.UtcNow;
            await ctx.SaveChangesAsync();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                RestaurantTestFactories.CreateReservationService(ctx).CancelAsync(
                    confirmed2.Reservation.IdRestaurantReservation,
                    idSociete));
            Assert.Contains("utilisé", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void TicketCodeGenerator_uses_rest_prefix()
        {
            var code = RestaurantTicketCodeGenerator.GenerateTicketCodeCandidate(7);
            Assert.StartsWith("REST-TKT-007-", code);
            Assert.True(RestaurantTicketCodeGenerator.IsValidTicketCodeFormat(code));
        }

        private static async Task<(int IdSociete, string TicketCode, int IdTicket)> SeedConfirmedTicketAsync(
            CongoTravelDbContext ctx,
            int quantity)
        {
            var (idSociete, _, idCreneau) = await RestaurantTestFactories.SeedPublishedCreneauAsync(
                ctx, $"TKT-{Guid.NewGuid():N}"[..12], capacite: 20);

            var creneau = await ctx.RestaurantCreneaux.SingleAsync(c => c.IdRestaurantCreneau == idCreneau);
            creneau.StartAtUtc = DateTime.UtcNow.AddHours(-1);
            creneau.EndAtUtc = DateTime.UtcNow.AddHours(3);
            await ctx.SaveChangesAsync();

            var hold = await RestaurantTestFactories.CreateHoldService(ctx).CreateHoldAsync(
                idCreneau,
                idSociete,
                new RestaurantHoldRequestDto
                {
                    Items = new List<RestaurantHoldItemRequestDto> { new() { Quantity = quantity } }
                });

            var confirmed = await RestaurantTestFactories.CreatePaymentService(ctx).ConfirmPaymentAsync(
                hold.IdRestaurantReservation,
                idSociete,
                new RestaurantConfirmPaymentRequestDto { MethodePaiement = "CASH" });

            var ticket = confirmed.Reservation.Tickets.First();
            return (idSociete, ticket.TicketCode, ticket.IdRestaurantTicket);
        }

        private static async Task SetSocieteLogoAsync(
            CongoTravelDbContext ctx,
            int idSociete,
            string logo)
        {
            var societe = await ctx.Societes.SingleAsync(s => s.IdSociete == idSociete);
            societe.Logo = logo;
            await ctx.SaveChangesAsync();
        }
    }
}
