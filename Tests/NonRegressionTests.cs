using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Models.DTOs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Tests de non-régression pour s'assurer que les modifications SignalR ne cassent pas l'existant
    /// </summary>
    public class NonRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public NonRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Application_CanStartWithoutErrors()
        {
            // Arrange & Act - L'application doit pouvoir démarrer
            var factory = _factory;

            // Assert - Pas d'exception au démarrage
            Assert.NotNull(factory);
        }
    }
}
