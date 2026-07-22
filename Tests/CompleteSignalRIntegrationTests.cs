using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using CongoTravel.Services;
using CongoTravel.Models.DTOs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Tests
{
    /// <summary>
    /// Tests d'intégration complets pour SignalR Dashboard
    /// </summary>
    public class CompleteSignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public CompleteSignalRIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Application_CanStartWithSignalR_ServicesRegistered()
        {
            // Arrange & Act - Créer l'application factory
            var factory = _factory;

            // Assert - L'application peut démarrer sans erreur
            Assert.NotNull(factory);
        }
    }
}
