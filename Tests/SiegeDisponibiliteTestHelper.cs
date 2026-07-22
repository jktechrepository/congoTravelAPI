using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Data;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Tests
{
    internal static class SiegeDisponibiliteTestHelper
    {
        public static ISiegeDisponibiliteService Create(CongoTravelDbContext ctx)
        {
            var siegeMock = new Mock<ISiegeService>();
            siegeMock
                .Setup(s => s.EnsureSeatsForVehiculeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return new SiegeDisponibiliteService(ctx, siegeMock.Object, NullLogger<SiegeDisponibiliteService>.Instance);
        }
    }
}
