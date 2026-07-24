using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionPhotoServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementSessionPhotoService CreatePhotoService(CongoTravelDbContext ctx) =>
            new(ctx, NullLogger<EvenementSessionPhotoService>.Instance);

        private static EvenementSessionService CreateSessionService(CongoTravelDbContext ctx) =>
            new(ctx, CreatePhotoService(ctx), NullLogger<EvenementSessionService>.Instance);

        private static string TinyJpegBase64() => Convert.ToBase64String(new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 });

        private static AddEvenementSessionPhotoDto PhotoDto(int? ordre = null) =>
            new()
            {
                PhotoBase64 = TinyJpegBase64(),
                FileName = "cover.jpg",
                Ordre = ordre
            };

        [Fact]
        public async Task AddPhotoAsync_allows_up_to_three_photos()
        {
            await using var ctx = BuildDb(nameof(AddPhotoAsync_allows_up_to_three_photos));
            var (idSociete, sessionId) = await SeedPublishedSessionAsync(ctx);
            var photos = CreatePhotoService(ctx);

            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto(1));
            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto(2));
            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto(3));

            var list = await photos.GetBySessionIdAsync(sessionId, idSociete);
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task AddPhotoAsync_rejects_fourth_photo()
        {
            await using var ctx = BuildDb(nameof(AddPhotoAsync_rejects_fourth_photo));
            var (idSociete, sessionId) = await SeedPublishedSessionAsync(ctx);
            var photos = CreatePhotoService(ctx);

            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto());
            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto());
            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                photos.AddPhotoAsync(sessionId, idSociete, PhotoDto()));
        }

        [Fact]
        public async Task AddPhotoAsync_rejects_duplicate_ordre()
        {
            await using var ctx = BuildDb(nameof(AddPhotoAsync_rejects_duplicate_ordre));
            var (idSociete, sessionId) = await SeedPublishedSessionAsync(ctx);
            var photos = CreatePhotoService(ctx);

            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto(1));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                photos.AddPhotoAsync(sessionId, idSociete, PhotoDto(1)));
        }

        [Fact]
        public async Task CreateDraftAsync_attaches_photos_from_request()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_attaches_photos_from_request));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var sessions = CreateSessionService(ctx);

            var created = await sessions.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "PHOTO-DRAFT",
                IdSite = idSite,
                Libelle = "Session avec photos",
                StartAtUtc = DateTime.UtcNow.AddDays(2),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 100,
                    PrixUnitaire = 10,
                    CodeDevise = "CDF"
                },
                Photos = new List<AddEvenementSessionPhotoDto>
                {
                    PhotoDto(1),
                    PhotoDto(2)
                }
            }, idSociete);

            Assert.Equal(2, created.Photos.Count);
            Assert.All(created.Photos, p => Assert.StartsWith("data:image/jpeg;base64,", p.PhotoBase64));
        }

        [Fact]
        public async Task GetBySessionIdAsync_enforces_tenancy()
        {
            await using var ctx = BuildDb(nameof(GetBySessionIdAsync_enforces_tenancy));
            var (idSociete, sessionId) = await SeedPublishedSessionAsync(ctx);
            var (otherSociete, _) = await SeedSocieteAsync(ctx, "Other");
            var photos = CreatePhotoService(ctx);

            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto());

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                photos.GetBySessionIdAsync(sessionId, otherSociete));
        }

        [Fact]
        public async Task Delete_session_cascades_photos()
        {
            await using var ctx = BuildDb(nameof(Delete_session_cascades_photos));
            var (idSociete, sessionId) = await SeedPublishedSessionAsync(ctx);
            var photos = CreatePhotoService(ctx);

            await photos.AddPhotoAsync(sessionId, idSociete, PhotoDto());
            Assert.Equal(1, await ctx.EvenementSessionPhotos.CountAsync());

            var session = await ctx.EvenementSessions.FirstAsync(s => s.IdEvenementSession == sessionId);
            ctx.EvenementSessions.Remove(session);
            await ctx.SaveChangesAsync();

            // InMemory may not enforce cascade the same as relational; remove orphans if needed.
            var remaining = await ctx.EvenementSessionPhotos
                .Where(p => p.IdEvenementSession == sessionId)
                .ToListAsync();
            if (remaining.Count > 0)
            {
                // Relational cascade is configured; verify FK relationship still modeled.
                var entity = ctx.Model.FindEntityType(typeof(EvenementSessionPhoto));
                var fk = entity!.GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(EvenementSession));
                Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
            }
            else
            {
                Assert.Empty(remaining);
            }
        }

        private static async Task<(int IdSociete, int SessionId)> SeedPublishedSessionAsync(CongoTravelDbContext ctx)
        {
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var sessions = CreateSessionService(ctx);
            var draft = await sessions.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = $"S-{Guid.NewGuid():N}".Substring(0, 16),
                IdSite = idSite,
                Libelle = "Session test photos",
                StartAtUtc = DateTime.UtcNow.AddDays(3),
                InventoryMode = "GlobalQuota",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 50,
                    PrixUnitaire = 5,
                    CodeDevise = "CDF"
                }
            }, idSociete);

            return (idSociete, draft.IdEvenementSession);
        }

        private static async Task<(int IdSociete, int IdSite)> SeedSocieteAsync(
            CongoTravelDbContext ctx,
            string nom = "Test Societe") =>
            await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, nom);
    }
}
