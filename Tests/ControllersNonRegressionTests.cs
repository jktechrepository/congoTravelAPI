using Microsoft.AspNetCore.Mvc.Testing;
using CongoTravel.Services;
using CongoTravel.Services.Notifications;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    /// <summary>Point d’accroche WebApplicationFactory — étendre avec des smoke tests HTTP si besoin.</summary>
    public class ControllersNonRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ControllersNonRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public void Factory_ShouldConstruct()
        {
            Assert.NotNull(_factory);
        }

        [Fact]
        public void FirebaseNotificationService_ImplementsInterface()
        {
            Assert.True(typeof(IFirebaseNotificationService).IsAssignableFrom(typeof(FirebaseNotificationService)));
            Assert.True(typeof(IFirebaseNotificationService).IsAssignableFrom(typeof(NullFirebaseNotificationService)));
        }
    }
}
