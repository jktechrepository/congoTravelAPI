using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Services;

namespace CongoTravel.Tests
{
    public static class BilletServiceTestHelper
    {
        public static BilletService Create(CongoTravelDbContext ctx) =>
            new(
                ctx,
                ConfigSocieteTestHelper.Create(ctx),
                new VoyageTarifService(ctx),
                new SiegeDisponibiliteService(
                    ctx,
                    new SiegeService(ctx, NullLogger<SiegeService>.Instance),
                    NullLogger<SiegeDisponibiliteService>.Instance),
                NullLogger<BilletService>.Instance);
    }
}
