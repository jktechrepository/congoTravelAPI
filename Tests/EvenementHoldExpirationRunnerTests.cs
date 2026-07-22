using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using CongoTravel.Data;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementHoldExpirationRunnerTests
    {
        private static CongoTravelDbContext BuildInMemoryDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task ExpireHoldsAsync_skips_non_relational_database_without_error()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_skips_non_relational_database_without_error));
            var runner = new EvenementHoldExpirationRunner(NullLogger<EvenementHoldExpirationRunner>.Instance);

            await runner.ExpireHoldsAsync(ctx);

            Assert.True(true);
        }

        [Fact]
        public async Task ExpireHoldsAsync_skips_when_no_expired_holds_on_mysql_provider_check()
        {
            await using var ctx = BuildInMemoryDb(nameof(ExpireHoldsAsync_skips_when_no_expired_holds_on_mysql_provider_check));
            ctx.EvenementReservations.Add(new EvenementReservation
            {
                IdSociete = 1,
                IdEvenementSession = 1,
                ReferenceReservation = "EVT-HOLD-1",
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                MontantSousTotal = 0m,
                CodeDevise = "CDF",
                DateCreation = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            var runner = new EvenementHoldExpirationRunner(NullLogger<EvenementHoldExpirationRunner>.Instance);
            await runner.ExpireHoldsAsync(ctx);

            var reservation = await ctx.EvenementReservations.SingleAsync();
            Assert.Equal(EvenementReservationStatus.HOLD, reservation.Status);
        }
    }
}
