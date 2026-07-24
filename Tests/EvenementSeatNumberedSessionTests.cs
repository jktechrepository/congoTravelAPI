using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSeatNumberedSessionTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task CreateDraftAsync_creates_seat_numbered_session_with_sections()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_creates_seat_numbered_session_with_sections));
            var (idSociete, idSite, idClasseVip) = await SeedClasseAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "CONCERT-A",
                IdSite = idSite,
                Libelle = "Concert mode A",
                StartAtUtc = DateTime.UtcNow.AddDays(7),
                InventoryMode = "SeatNumbered",
                Sections = new List<EvenementCreateSessionSectionDto>
                {
                    new()
                    {
                        CodeSection = "ORCH",
                        Libelle = "Orchestre",
                        Seats = new List<EvenementCreateSessionSeatDto>
                        {
                            new() { SeatCode = "A-01", IdEvenementClasse = idClasseVip, PrixUnitaire = 100m, CodeDevise = "USD" },
                            new() { SeatCode = "A-02", PrixUnitaire = 80m, CodeDevise = "USD" }
                        }
                    }
                },
                Seats = new List<EvenementCreateSessionSeatDto>
                {
                    new() { SeatCode = "GA-01", PrixUnitaire = 30m, CodeDevise = "USD" }
                }
            }, idSociete);

            Assert.Equal("SeatNumbered", created.InventoryMode);
            Assert.Equal(3, created.Seats.Count);
            Assert.Contains(created.Seats, s => s.SeatCode == "A-01" && s.CodeSection == "ORCH");
            Assert.Contains(created.Seats, s => s.SeatCode == "GA-01" && s.CodeSection == null);

            Assert.Equal(3, await ctx.EvenementSessionSeats.CountAsync());
            Assert.Equal(1, await ctx.EvenementSessionSections.CountAsync());
        }

        [Fact]
        public async Task PublishAsync_publishes_seat_numbered_session()
        {
            await using var ctx = BuildDb(nameof(PublishAsync_publishes_seat_numbered_session));
            var (idSociete, idSite, _) = await SeedClasseAsync(ctx);
            var service = CreateService(ctx);

            var draft = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "PUB-A",
                IdSite = idSite,
                Libelle = "Publish A",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = "SeatNumbered",
                Seats = new List<EvenementCreateSessionSeatDto>
                {
                    new() { SeatCode = "B-01", PrixUnitaire = 25m, CodeDevise = "CDF" }
                }
            }, idSociete);

            var published = await service.PublishAsync(draft.IdEvenementSession, idSociete);
            Assert.Equal("Published", published.Status);
            Assert.Single(published.Seats);
            Assert.Equal("Available", published.Seats[0].SeatStatus);
        }

        [Fact]
        public async Task CreateDraftAsync_rejects_duplicate_seat_codes()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_rejects_duplicate_seat_codes));
            var (idSociete, idSite, _) = await SeedClasseAsync(ctx);
            var service = CreateService(ctx);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateDraftAsync(new EvenementCreateSessionRequestDto
                {
                    CodeSession = "DUPE-A",
                IdSite = idSite,
                    Libelle = "Dupe seats",
                    StartAtUtc = DateTime.UtcNow.AddDays(1),
                    InventoryMode = "SeatNumbered",
                    Sections = new List<EvenementCreateSessionSectionDto>
                    {
                        new()
                        {
                            CodeSection = "S1",
                            Libelle = "S1",
                            Seats = new List<EvenementCreateSessionSeatDto>
                            {
                                new() { SeatCode = "X-01", PrixUnitaire = 10m, CodeDevise = "CDF" }
                            }
                        }
                    },
                    Seats = new List<EvenementCreateSessionSeatDto>
                    {
                        new() { SeatCode = "x-01", PrixUnitaire = 10m, CodeDevise = "CDF" }
                    }
                }, idSociete));
        }

        private static EvenementSessionService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, new EvenementSessionPhotoService(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionPhotoService>.Instance), Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

        private static async Task<(int IdSociete, int IdSite, int IdClasseVip)> SeedClasseAsync(CongoTravelDbContext ctx)
        {
            var (idSociete, idSite) = await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, "Session A");

            var vip = new EvenementClasse
            {
                IdSociete = idSociete,
                CodeClasse = "VIP",
                Libelle = "VIP",
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
            ctx.EvenementClasses.Add(vip);
            await ctx.SaveChangesAsync();
            return (idSociete, idSite, vip.IdEvenementClasse);
        }
    }
}
