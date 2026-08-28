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
    public class EvenementAvailabilityServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementAvailabilityService CreateAvailabilityService(CongoTravelDbContext ctx) =>
            new(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementAvailabilityService>.Instance);

        [Fact]
        public async Task GetSessionAvailabilityAsync_returns_global_quota_stock()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_returns_global_quota_stock));
            var (idSociete, idSession) = await SeedSessionAsync(ctx, capacity: 100, hold: 15, sold: 30);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete);

            Assert.NotNull(result);
            Assert.Equal(idSession, result!.IdEvenementSession);
            Assert.Equal(idSociete, result.IdSociete);
            Assert.Equal("EVT Availability", result.NomSociete);
            Assert.Equal("GlobalQuota", result.InventoryMode);
            Assert.Equal("Published", result.Status);
            Assert.NotNull(result.GlobalQuota);
            Assert.Equal(100, result.GlobalQuota!.CapaciteTotale);
            Assert.Equal(15, result.GlobalQuota.QuantiteHold);
            Assert.Equal(30, result.GlobalQuota.QuantiteVendue);
            Assert.Equal(55, result.GlobalQuota.QuantiteDisponible);
            Assert.Equal(25m, result.GlobalQuota.PrixUnitaire);
            Assert.Equal("USD", result.GlobalQuota.CodeDevise);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_returns_null_for_other_societe));
            var (idSociete1, idSession) = await SeedSessionAsync(ctx, capacity: 50, hold: 0, sold: 0);
            var idSociete2 = await SeedOtherSocieteAsync(ctx);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete2);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_reflects_hold_decrement()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_reflects_hold_decrement));
            var (idSociete, idSession) = await SeedSessionAsync(ctx, capacity: 20, hold: 0, sold: 0);

            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

            await holdService.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
            {
                Items = new List<EvenementHoldItemRequestDto> { new() { Quantity = 7 } }
            });

            var availability = await CreateAvailabilityService(ctx)
                .GetSessionAvailabilityAsync(idSession, idSociete);

            Assert.NotNull(availability?.GlobalQuota);
            Assert.Equal(7, availability.GlobalQuota!.QuantiteHold);
            Assert.Equal(13, availability.GlobalQuota.QuantiteDisponible);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_never_returns_negative_disponible()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_never_returns_negative_disponible));
            var (idSociete, idSession) = await SeedSessionAsync(ctx, capacity: 10, hold: 8, sold: 5);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete);

            Assert.Equal(0, result!.GlobalQuota!.QuantiteDisponible);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_returns_class_quota_stock()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_returns_class_quota_stock));
            var (idSociete, idSession, vipClassId, stdClassId) = await SeedClassSessionAsync(
                ctx, vipCapacity: 50, vipHold: 5, vipSold: 10, stdCapacity: 100, stdHold: 20, stdSold: 30);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete);

            Assert.NotNull(result);
            Assert.Equal("ClassQuota", result!.InventoryMode);
            Assert.Null(result.GlobalQuota);
            Assert.NotNull(result.ClassQuotas);
            Assert.Equal(2, result.ClassQuotas!.Count);

            var vip = result.ClassQuotas.Single(q => q.IdEvenementClasse == vipClassId);
            Assert.Equal("VIP", vip.CodeClasse);
            Assert.Equal("VIP", vip.LibelleClasse);
            Assert.Equal(50, vip.CapaciteTotale);
            Assert.Equal(5, vip.QuantiteHold);
            Assert.Equal(10, vip.QuantiteVendue);
            Assert.Equal(35, vip.QuantiteDisponible);
            Assert.Equal(50m, vip.PrixUnitaire);
            Assert.Equal("USD", vip.CodeDevise);

            var std = result.ClassQuotas.Single(q => q.IdEvenementClasse == stdClassId);
            Assert.Equal("STD", std.CodeClasse);
            Assert.Equal(50, std.QuantiteDisponible);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_class_quota_reflects_hold_decrement()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_class_quota_reflects_hold_decrement));
            var (idSociete, idSession, vipClassId, _) = await SeedClassSessionAsync(
                ctx, vipCapacity: 20, vipHold: 0, vipSold: 0, stdCapacity: 30, stdHold: 0, stdSold: 0);

            var holdService = new EvenementHoldService(
                ctx,
                new EvenementInventoryHoldStrategyFactory(
                    new EvenementGlobalQuotaHoldStrategy(ctx),
                    new EvenementClassQuotaHoldStrategy(ctx),
                    new EvenementSeatNumberedHoldStrategy(ctx)),
                new ConfigSocieteService(ctx),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementHoldService>.Instance);

            await holdService.CreateHoldAsync(idSession, idSociete, new EvenementHoldRequestDto
            {
                Items = new List<EvenementHoldItemRequestDto>
                {
                    new() { ClassId = vipClassId, Quantity = 7 }
                }
            });

            var availability = await CreateAvailabilityService(ctx)
                .GetSessionAvailabilityAsync(idSession, idSociete);

            var vip = availability!.ClassQuotas!.Single(q => q.IdEvenementClasse == vipClassId);
            Assert.Equal(7, vip.QuantiteHold);
            Assert.Equal(13, vip.QuantiteDisponible);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_class_quota_never_returns_negative_disponible()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_class_quota_never_returns_negative_disponible));
            var (idSociete, idSession, _, stdClassId) = await SeedClassSessionAsync(
                ctx, vipCapacity: 10, vipHold: 0, vipSold: 0, stdCapacity: 10, stdHold: 8, stdSold: 5);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete);

            var std = result!.ClassQuotas!.Single(q => q.IdEvenementClasse == stdClassId);
            Assert.Equal(0, std.QuantiteDisponible);
        }

        private static async Task<(int IdSociete, int IdSession, int VipClassId, int StdClassId)> SeedClassSessionAsync(
            CongoTravelDbContext ctx,
            int vipCapacity,
            int vipHold,
            int vipSold,
            int stdCapacity,
            int stdHold,
            int stdSold)
        {
            var societe = new Societe { Nom = "EVT Class Availability", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var vip = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            var std = new EvenementClasse
            {
                IdSociete = societe.IdSociete,
                CodeClasse = "STD",
                Libelle = "Standard",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.AddRange(vip, std);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"AV-CLASS-{Guid.NewGuid():N}"[..12],
                Libelle = "Class availability test",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = EvenementInventoryMode.ClassQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            ctx.EvenementSessionClassQuotas.AddRange(
                new EvenementSessionClassQuota
                {
                    IdEvenementSession = session.IdEvenementSession,
                    IdEvenementClasse = vip.IdEvenementClasse,
                    CapaciteTotale = vipCapacity,
                    QuantiteHold = vipHold,
                    QuantiteVendue = vipSold,
                    PrixUnitaire = 50m,
                    CodeDevise = "USD"
                },
                new EvenementSessionClassQuota
                {
                    IdEvenementSession = session.IdEvenementSession,
                    IdEvenementClasse = std.IdEvenementClasse,
                    CapaciteTotale = stdCapacity,
                    QuantiteHold = stdHold,
                    QuantiteVendue = stdSold,
                    PrixUnitaire = 15m,
                    CodeDevise = "USD"
                });
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, session.IdEvenementSession, vip.IdEvenementClasse, std.IdEvenementClasse);
        }

        [Fact]
        public async Task GetSessionAvailabilityAsync_returns_seat_statuses()
        {
            await using var ctx = BuildDb(nameof(GetSessionAvailabilityAsync_returns_seat_statuses));
            var (idSociete, idSession, seatA, _) = await SeedSeatSessionAsync(ctx);
            var service = CreateAvailabilityService(ctx);

            var result = await service.GetSessionAvailabilityAsync(idSession, idSociete);

            Assert.NotNull(result);
            Assert.Equal("SeatNumbered", result!.InventoryMode);
            Assert.Equal(2, result.Seats!.Count);
            Assert.Equal("Available", result.Seats.Single(s => s.IdEvenementSessionSeat == seatA).SeatStatus);
        }

        private static async Task<(int IdSociete, int IdSession, int SeatAId, int SeatBId)> SeedSeatSessionAsync(
            CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "EVT Seat Availability", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"AV-SEAT-{Guid.NewGuid():N}"[..12],
                Libelle = "Seat availability",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = EvenementInventoryMode.SeatNumbered,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            var seatA = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "S-01",
                PrixUnitaire = 25m,
                CodeDevise = "USD"
            };
            var seatB = new EvenementSessionSeat
            {
                IdEvenementSession = session.IdEvenementSession,
                SeatCode = "S-02",
                SeatStatus = EvenementSessionSeatStatus.Sold,
                PrixUnitaire = 25m,
                CodeDevise = "USD"
            };
            ctx.EvenementSessionSeats.AddRange(seatA, seatB);
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, session.IdEvenementSession, seatA.IdEvenementSessionSeat, seatB.IdEvenementSessionSeat);
        }

        private static async Task<(int IdSociete, int IdSession)> SeedSessionAsync(
            CongoTravelDbContext ctx,
            int capacity,
            int hold,
            int sold)
        {
            var societe = new Societe { Nom = "EVT Availability", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();

            var session = new EvenementSession
            {
                IdSociete = societe.IdSociete,
                CodeSession = $"AV-{Guid.NewGuid():N}"[..10],
                Libelle = "Availability test",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = EvenementInventoryMode.GlobalQuota,
                Status = EvenementSessionStatus.Published,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementSessions.Add(session);
            await ctx.SaveChangesAsync();

            ctx.EvenementSessionGlobalQuotas.Add(new EvenementSessionGlobalQuota
            {
                IdEvenementSession = session.IdEvenementSession,
                CapaciteTotale = capacity,
                QuantiteHold = hold,
                QuantiteVendue = sold,
                PrixUnitaire = 25m,
                CodeDevise = "USD"
            });
            await ctx.SaveChangesAsync();

            return (societe.IdSociete, session.IdEvenementSession);
        }

        private static async Task<int> SeedOtherSocieteAsync(CongoTravelDbContext ctx)
        {
            var societe = new Societe { Nom = "Other", DateCreation = DateTime.UtcNow };
            ctx.Societes.Add(societe);
            await ctx.SaveChangesAsync();
            return societe.IdSociete;
        }
    }
}
