using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using Xunit;

namespace CongoTravel.Tests
{
    public class RefreshTokenServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static RefreshTokenService BuildService(CongoTravelDbContext ctx) =>
            new(ctx, new ConfigurationBuilder().Build());

        [Fact]
        public async Task RevokeAllRefreshTokensAsync_revokes_only_non_revoked_tokens()
        {
            await using var ctx = BuildDb(nameof(RevokeAllRefreshTokensAsync_revokes_only_non_revoked_tokens));
            const int userId = 9;
            var now = DateTime.UtcNow;

            ctx.RefreshTokens.AddRange(
                new RefreshToken
                {
                    IdRefreshToken = 1,
                    IdUtilisateur = userId,
                    TokenHash = "hash-active-1",
                    DateCreation = now,
                    DateExpiration = now.AddDays(30)
                },
                new RefreshToken
                {
                    IdRefreshToken = 2,
                    IdUtilisateur = userId,
                    TokenHash = "hash-active-2",
                    DateCreation = now,
                    DateExpiration = now.AddDays(30)
                },
                new RefreshToken
                {
                    IdRefreshToken = 3,
                    IdUtilisateur = userId,
                    TokenHash = "hash-revoked",
                    DateCreation = now.AddDays(-2),
                    DateExpiration = now.AddDays(28),
                    DateRevocation = now.AddDays(-1)
                },
                new RefreshToken
                {
                    IdRefreshToken = 4,
                    IdUtilisateur = 99,
                    TokenHash = "hash-other-user",
                    DateCreation = now,
                    DateExpiration = now.AddDays(30)
                });
            await ctx.SaveChangesAsync();

            var svc = BuildService(ctx);
            var result = await svc.RevokeAllRefreshTokensAsync(userId);

            Assert.True(result);

            var userTokens = await ctx.RefreshTokens
                .Where(t => t.IdUtilisateur == userId)
                .OrderBy(t => t.IdRefreshToken)
                .ToListAsync();

            Assert.NotNull(userTokens[0].DateRevocation);
            Assert.NotNull(userTokens[1].DateRevocation);
            Assert.Equal(now.AddDays(-1), userTokens[2].DateRevocation);

            var otherUserToken = await ctx.RefreshTokens.SingleAsync(t => t.IdUtilisateur == 99);
            Assert.Null(otherUserToken.DateRevocation);
        }

        [Fact]
        public async Task RevokeAllRefreshTokensAsync_returns_false_when_no_active_tokens()
        {
            await using var ctx = BuildDb(nameof(RevokeAllRefreshTokensAsync_returns_false_when_no_active_tokens));
            var now = DateTime.UtcNow;

            ctx.RefreshTokens.Add(new RefreshToken
            {
                IdUtilisateur = 5,
                TokenHash = "already-revoked",
                DateCreation = now.AddDays(-2),
                DateExpiration = now.AddDays(28),
                DateRevocation = now.AddDays(-1)
            });
            await ctx.SaveChangesAsync();

            var svc = BuildService(ctx);
            var result = await svc.RevokeAllRefreshTokensAsync(5);

            Assert.False(result);
        }
    }
}
