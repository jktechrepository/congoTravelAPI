using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class ClientRegistrationTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .Options);

        private static ClientController CreateController(
            Mock<IClientRepository> repo,
            CongoTravelDbContext db)
        {
            var audit = new Mock<IAuditService>();
            audit.Setup(a => a.LogCreateAsync(
                    It.IsAny<object>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>()))
                .Returns(Task.CompletedTask);

            var controller = new ClientController(
                audit.Object,
                new Mock<ICurrentUserService>().Object,
                null!,
                repo.Object,
                db,
                NullLogger<ClientController>.Instance)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };

            return controller;
        }

        [Fact]
        public async Task Register_with_null_email_returns_created()
        {
            await using var db = BuildDb(nameof(Register_with_null_email_returns_created));
            var repo = new Mock<IClientRepository>();
            Client? captured = null;

            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Client?)null);
            repo.Setup(r => r.CreateAsync(It.IsAny<Client>()))
                .Callback<Client>(c => captured = c)
                .ReturnsAsync((Client c) =>
                {
                    c.IdClient = 42;
                    return c;
                });

            var controller = CreateController(repo, db);
            var action = await controller.RegisterClient(new RegisterClientDto
            {
                NomClient = "Jean Dupont",
                EmailClient = null,
                Telephone = "+243812345678",
                AcceptTerms = true
            });

            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.NotNull(captured);
            Assert.Null(captured!.EmailClient);
        }

        [Fact]
        public async Task Register_with_whitespace_email_stores_null()
        {
            await using var db = BuildDb(nameof(Register_with_whitespace_email_stores_null));
            var repo = new Mock<IClientRepository>();
            Client? captured = null;

            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Client?)null);
            repo.Setup(r => r.CreateAsync(It.IsAny<Client>()))
                .Callback<Client>(c => captured = c)
                .ReturnsAsync((Client c) =>
                {
                    c.IdClient = 43;
                    return c;
                });

            var controller = CreateController(repo, db);
            await controller.RegisterClient(new RegisterClientDto
            {
                NomClient = "Marie Kabila",
                EmailClient = "   ",
                Telephone = "+243812345679",
                AcceptTerms = true
            });

            Assert.NotNull(captured);
            Assert.Null(captured!.EmailClient);
        }

        [Fact]
        public async Task Register_normalizes_email_to_lowercase()
        {
            await using var db = BuildDb(nameof(Register_normalizes_email_to_lowercase));
            var repo = new Mock<IClientRepository>();
            Client? captured = null;

            repo.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((Client?)null);
            repo.Setup(r => r.CreateAsync(It.IsAny<Client>()))
                .Callback<Client>(c => captured = c)
                .ReturnsAsync((Client c) =>
                {
                    c.IdClient = 44;
                    return c;
                });

            var controller = CreateController(repo, db);
            await controller.RegisterClient(new RegisterClientDto
            {
                NomClient = "Paul Test",
                EmailClient = "  Paul.TEST@Example.COM  ",
                Telephone = "+243812345680",
                AcceptTerms = true
            });

            Assert.Equal("paul.test@example.com", captured!.EmailClient);
        }
    }
}
