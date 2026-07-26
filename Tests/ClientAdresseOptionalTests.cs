using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientAdresseOptionalTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static ClientService CreateService(CongoTravelDbContext ctx)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://test.local"
                })
                .Build();

            return new ClientService(
                ctx,
                new Mock<IEmailService>().Object,
                new Mock<IEmailVerificationService>().Object,
                new Mock<ISmsNotificationService>().Object,
                new Mock<IUtilisateurRepository>().Object,
                NullLogger<ClientService>.Instance,
                config);
        }

        [Fact]
        public async Task CreateAsync_allows_client_without_adresse()
        {
            await using var db = BuildDb(nameof(CreateAsync_allows_client_without_adresse));
            var service = CreateService(db);

            var created = await service.CreateAsync(new Client
            {
                NomClient = "Client Sans Adresse",
                Telephone = "+243900000001",
                EmailClient = "sans.adresse@test.com",
                AdresseClient = null,
                Statut = true,
                IsActif = true
            });

            var stored = await db.Clients.FirstAsync(c => c.IdClient == created.IdClient);
            Assert.Null(stored.AdresseClient);
        }

        [Fact]
        public async Task CreateAsync_normalizes_empty_adresse_to_null()
        {
            await using var db = BuildDb(nameof(CreateAsync_normalizes_empty_adresse_to_null));
            var service = CreateService(db);

            var created = await service.CreateAsync(new Client
            {
                NomClient = "Client Adresse Vide",
                Telephone = "+243900000002",
                EmailClient = "adresse.vide@test.com",
                AdresseClient = "   ",
                Statut = true,
                IsActif = true
            });

            var stored = await db.Clients.FirstAsync(c => c.IdClient == created.IdClient);
            Assert.Null(stored.AdresseClient);
        }

        [Fact]
        public async Task SearchAsync_does_not_fail_when_adresse_is_null()
        {
            await using var db = BuildDb(nameof(SearchAsync_does_not_fail_when_adresse_is_null));
            db.Clients.Add(new Client
            {
                NomClient = "Jean Null Adresse",
                AdresseClient = null,
                Telephone = "+243900000003",
                EmailClient = "jean.null@test.com",
                Statut = true,
                IsActif = true,
                DateCreation = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var results = await service.SearchAsync("Jean");

            Assert.Single(results);
            Assert.Null(results.First().AdresseClient);
        }
    }
}
