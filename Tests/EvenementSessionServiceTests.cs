using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static EvenementSessionService CreateService(CongoTravelDbContext ctx) =>
            new(ctx, new EvenementSessionPhotoService(ctx, Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionPhotoService>.Instance), Microsoft.Extensions.Logging.Abstractions.NullLogger<EvenementSessionService>.Instance);

        [Fact]
        public async Task CreateDraftAsync_creates_session_with_global_quota_pricing()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_creates_session_with_global_quota_pricing));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.CreateDraftAsync(new EvenementCreateSessionRequestDto
            {
                CodeSession = "GALA-2026",
                IdSite = idSite,
                Libelle = "Gala annuel",
                Description = " Grande soiree culturelle annuelle ",
                StartAtUtc = DateTime.UtcNow.AddDays(10),
                InventoryMode = "GlobalQuota",
                TypeEvenement = "Music",
                NomOrganisateur = " Kansa Events ",
                TelephoneOrganisateur = " +243900000001 ",
                MailOrganisateur = " orga@kansa.cd ",
                LogoOrganisateur = " https://cdn.example/event-logo.png ",
                Ville = " Kinshasa ",
                Commune = " Lingwala ",
                Quartier = " Quartier Test ",
                Avenue = " Avenue exemple ",
                Numero = " 12A ",
                GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
                {
                    CapaciteTotale = 200,
                    PrixUnitaire = 50m,
                    CodeDevise = "USD"
                }
            }, idSociete);

            Assert.Equal("Draft", result.Status);
            Assert.Equal("GlobalQuota", result.InventoryMode);
            Assert.Equal("Music", result.TypeEvenement);
            Assert.Equal("Kansa Events", result.NomOrganisateur);
            Assert.Equal("+243900000001", result.TelephoneOrganisateur);
            Assert.Equal("orga@kansa.cd", result.MailOrganisateur);
            Assert.Equal("https://cdn.example/event-logo.png", result.LogoOrganisateur);
            Assert.Equal("Grande soiree culturelle annuelle", result.Description);
            Assert.Equal("Kinshasa", result.Ville);
            Assert.Equal("Lingwala", result.Commune);
            Assert.Equal("Quartier Test", result.Quartier);
            Assert.Equal("Avenue exemple", result.Avenue);
            Assert.Equal("12A", result.Numero);
            Assert.Equal(idSite, result.IdSite);
            Assert.NotNull(result.NomSite);
            Assert.NotNull(result.GlobalQuota);
            Assert.Equal(200, result.GlobalQuota!.CapaciteTotale);
            Assert.Equal(50m, result.GlobalQuota.PrixUnitaire);
            Assert.Equal("USD", result.GlobalQuota.CodeDevise);
        }

        [Fact]
        public async Task CreateDraftAsync_defaults_type_evenement_to_Autres()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_defaults_type_evenement_to_Autres));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var result = await service.CreateDraftAsync(BuildValidCreateRequest("DEFAULT-TYPE", idSite), idSociete);

            Assert.Equal("Autres", result.TypeEvenement);
            Assert.Null(result.Description);
            Assert.Null(result.NomOrganisateur);
            Assert.Null(result.LogoOrganisateur);
            Assert.Null(result.Ville);
            Assert.Null(result.Commune);
            Assert.Null(result.Quartier);
            Assert.Null(result.Avenue);
            Assert.Null(result.Numero);
        }

        [Fact]
        public async Task CreateDraftAsync_throws_when_type_evenement_invalid()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_throws_when_type_evenement_invalid));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);
            var request = BuildValidCreateRequest("BAD-TYPE", idSite);
            request.TypeEvenement = "InvalidType";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateDraftAsync(request, idSociete));

            Assert.Contains("TypeEvenement invalide", ex.Message);
        }

        [Fact]
        public async Task CreateDraftAsync_throws_conflict_on_duplicate_code()
        {
            await using var ctx = BuildDb(nameof(CreateDraftAsync_throws_conflict_on_duplicate_code));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);
            var request = BuildValidCreateRequest("DUPE-1", idSite);

            await service.CreateDraftAsync(request, idSociete);

            await Assert.ThrowsAsync<EvenementSessionConflictException>(() =>
                service.CreateDraftAsync(request, idSociete));
        }

        [Fact]
        public async Task PublishAsync_moves_draft_to_published()
        {
            await using var ctx = BuildDb(nameof(PublishAsync_moves_draft_to_published));
            var (idSociete, idSite) = await SeedSocieteAsync(ctx);
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("PUB-1", idSite), idSociete);
            var published = await service.PublishAsync(created.IdEvenementSession, idSociete);

            Assert.Equal("Published", published.Status);
        }

        [Fact]
        public async Task GetByIdAsync_returns_null_for_other_societe()
        {
            await using var ctx = BuildDb(nameof(GetByIdAsync_returns_null_for_other_societe));
            var (idSociete1, idSite1) = await SeedSocieteAsync(ctx, "Societe A");
            var (idSociete2, idSite2) = await SeedSocieteAsync(ctx, "Societe B");
            var service = CreateService(ctx);

            var created = await service.CreateDraftAsync(BuildValidCreateRequest("ISO-1", idSite1), idSociete1);
            var other = await service.GetByIdAsync(created.IdEvenementSession, idSociete2);

            Assert.Null(other);
        }

        private static EvenementCreateSessionRequestDto BuildValidCreateRequest(string code, int idSite) => new()
        {
            CodeSession = code,
            IdSite = idSite,
            Libelle = "Test session",
            StartAtUtc = DateTime.UtcNow.AddDays(5),
            InventoryMode = "GlobalQuota",
            GlobalQuota = new EvenementCreateSessionGlobalQuotaDto
            {
                CapaciteTotale = 50,
                PrixUnitaire = 10m,
                CodeDevise = "CDF"
            }
        };

        private static async Task<(int IdSociete, int IdSite)> SeedSocieteAsync(
            CongoTravelDbContext ctx,
            string nom = "Test Societe") =>
            await EvenementTestFactories.SeedSocieteWithSiteAsync(ctx, nom);
    }
}
