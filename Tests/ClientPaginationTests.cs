using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Controllers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientPaginationTests
    {
        private static DbContextOptions<CongoTravelDbContext> Options(string db) =>
            new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(db)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

        private static ClientService BuildService(CongoTravelDbContext context)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://test.local"
                })
                .Build();

            return new ClientService(
                context,
                new Mock<IEmailService>().Object,
                new Mock<IEmailVerificationService>().Object,
                new Mock<ISmsNotificationService>().Object,
                new Mock<IUtilisateurRepository>().Object,
                NullLogger<ClientService>.Instance,
                config);
        }

        [Fact]
        public async Task GetPagedAsync_returns_only_active_clients_by_default()
        {
            var db = nameof(GetPagedAsync_returns_only_active_clients_by_default);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Actif", AdresseClient = "A", IsActif = true, Statut = true, DateCreation = DateTime.UtcNow.AddDays(-1) },
                new Client { NomClient = "Inactif", AdresseClient = "B", IsActif = false, Statut = true, DateCreation = DateTime.UtcNow }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto { PageNumber = 1, PageSize = 20 };

            var result = await service.GetPagedAsync(request);

            Assert.Single(result.Data);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Data, c => Assert.True(c.IsActif));
        }

        [Fact]
        public async Task GetPagedAsync_includeInactive_returns_active_and_inactive()
        {
            var db = nameof(GetPagedAsync_includeInactive_returns_active_and_inactive);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Actif", AdresseClient = "A", IsActif = true, Statut = true },
                new Client { NomClient = "Inactif", AdresseClient = "B", IsActif = false, Statut = true }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto { IncludeInactive = true, PageNumber = 1, PageSize = 20 };

            var result = await service.GetPagedAsync(request);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Data.Count());
        }

        [Fact]
        public async Task GetPagedAsync_isActif_filter_has_priority_over_includeInactive()
        {
            var db = nameof(GetPagedAsync_isActif_filter_has_priority_over_includeInactive);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Actif", AdresseClient = "A", IsActif = true, Statut = true },
                new Client { NomClient = "Inactif", AdresseClient = "B", IsActif = false, Statut = true }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto
            {
                IncludeInactive = true,
                IsActif = false,
                PageNumber = 1,
                PageSize = 20
            };

            var result = await service.GetPagedAsync(request);

            Assert.Single(result.Data);
            Assert.Equal(1, result.TotalCount);
            Assert.All(result.Data, c => Assert.False(c.IsActif));
        }

        [Fact]
        public async Task GetPagedAsync_searchTerm_matches_email_and_applies_sort_and_pagination()
        {
            var db = nameof(GetPagedAsync_searchTerm_matches_email_and_applies_sort_and_pagination);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Jean Delta", AdresseClient = "A", EmailClient = "delta@sample.com", IsActif = true, Statut = true },
                new Client { NomClient = "Jean Bravo", AdresseClient = "B", EmailClient = "bravo@sample.com", IsActif = true, Statut = true },
                new Client { NomClient = "Jean Alpha", AdresseClient = "C", EmailClient = "alpha@sample.com", IsActif = true, Statut = true },
                new Client { NomClient = "Autre", AdresseClient = "D", EmailClient = "other@test.com", IsActif = true, Statut = true }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto
            {
                SearchTerm = "sample.com",
                SortBy = "NomClient",
                SortDescending = true,
                PageNumber = 2,
                PageSize = 2
            };

            var result = await service.GetPagedAsync(request);
            var data = result.Data.ToList();

            Assert.Equal(3, result.TotalCount);
            Assert.Single(data);
            Assert.Equal("Jean Alpha", data[0].NomClient);
        }

        [Fact]
        public async Task GetPagedAsync_excludes_soft_deleted_clients()
        {
            var db = nameof(GetPagedAsync_excludes_soft_deleted_clients);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Visible", AdresseClient = "A", IsActif = true, Statut = true, IsDeleted = false },
                new Client { NomClient = "Deleted", AdresseClient = "B", IsActif = true, Statut = true, IsDeleted = true }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto { PageNumber = 1, PageSize = 20 };

            var result = await service.GetPagedAsync(request);

            Assert.Single(result.Data);
            Assert.Equal("Visible", result.Data.First().NomClient);
        }

        [Fact]
        public async Task GetPagedAsync_uses_base_paged_request_search_term_for_backward_compatibility()
        {
            var db = nameof(GetPagedAsync_uses_base_paged_request_search_term_for_backward_compatibility);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "NoMatch", AdresseClient = "A", EmailClient = "nomatch@test.com", IsActif = true, Statut = true },
                new Client { NomClient = "Jean Legacy", AdresseClient = "B", EmailClient = "legacy@sample.com", IsActif = true, Statut = true }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var request = new ClientPagedSearchRequestDto { PageNumber = 1, PageSize = 20 };
            ((PagedRequest)request).SearchTerm = "sample.com";

            var result = await service.GetPagedAsync(request);

            Assert.Single(result.Data);
            Assert.Equal("Jean Legacy", result.Data.First().NomClient);
        }

        [Fact]
        public async Task GetPagedAsync_defaults_to_DateCreation_desc_when_no_sort()
        {
            var db = nameof(GetPagedAsync_defaults_to_DateCreation_desc_when_no_sort);
            await using var ctx = new CongoTravelDbContext(Options(db));

            var oldest = DateTime.UtcNow.AddDays(-10);
            var middle = DateTime.UtcNow.AddDays(-5);
            var newest = DateTime.UtcNow.AddDays(-1);

            ctx.Clients.AddRange(
                new Client { NomClient = "Ancien", AdresseClient = "A", IsActif = true, Statut = true, DateCreation = oldest },
                new Client { NomClient = "Recent", AdresseClient = "B", IsActif = true, Statut = true, DateCreation = newest },
                new Client { NomClient = "Milieu", AdresseClient = "C", IsActif = true, Statut = true, DateCreation = middle }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var result = await service.GetPagedAsync(new ClientPagedSearchRequestDto { PageNumber = 1, PageSize = 20 });
            var names = result.Data.Select(c => c.NomClient).ToList();

            Assert.Equal(3, result.TotalCount);
            Assert.Equal(new[] { "Recent", "Milieu", "Ancien" }, names);
        }

        [Fact]
        public async Task GetPagedAsync_explicit_sortBy_overrides_default()
        {
            var db = nameof(GetPagedAsync_explicit_sortBy_overrides_default);
            await using var ctx = new CongoTravelDbContext(Options(db));

            ctx.Clients.AddRange(
                new Client { NomClient = "Zulu", AdresseClient = "A", IsActif = true, Statut = true, DateCreation = DateTime.UtcNow },
                new Client { NomClient = "Alpha", AdresseClient = "B", IsActif = true, Statut = true, DateCreation = DateTime.UtcNow.AddDays(-30) }
            );
            await ctx.SaveChangesAsync();

            var service = BuildService(ctx);
            var result = await service.GetPagedAsync(new ClientPagedSearchRequestDto
            {
                SortBy = "NomClient",
                SortDescending = false,
                PageNumber = 1,
                PageSize = 20
            });

            Assert.Equal(new[] { "Alpha", "Zulu" }, result.Data.Select(c => c.NomClient));
        }
    }

    public class ClientPaginationNonRegressionTests
    {
        [Fact]
        public void ClientController_GetClients_returns_paged_result_contract()
        {
            var method = typeof(ClientController).GetMethod(nameof(ClientController.GetClients));
            Assert.NotNull(method);

            var returnType = method!.ReturnType;
            var expected = typeof(Task<Microsoft.AspNetCore.Mvc.ActionResult<PagedResult<ClientResponseDto>>>);
            Assert.Equal(expected, returnType);
        }

        [Fact]
        public void IClientRepository_GetPagedAsync_accepts_client_paged_search_request_contract()
        {
            var method = typeof(IClientRepository).GetMethod(nameof(IClientRepository.GetPagedAsync));
            Assert.NotNull(method);

            var parameter = method!.GetParameters().Single();
            Assert.Equal(typeof(ClientPagedSearchRequestDto), parameter.ParameterType);
        }
    }
}
