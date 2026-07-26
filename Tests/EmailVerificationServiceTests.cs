using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class EmailVerificationServiceTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .Options);

        private static (EmailVerificationService Svc, Mock<IEmailService> Email) CreateService(
            CongoTravelDbContext ctx,
            bool emailSendOk = true)
        {
            var email = new Mock<IEmailService>();
            email.Setup(e => e.SendEmailVerificationLinkAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(emailSendOk);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://app.test",
                    ["FrontendSettings:VerifyEmailPath"] = "/verify-email"
                })
                .Build();

            var svc = new EmailVerificationService(
                ctx,
                email.Object,
                config,
                NullLogger<EmailVerificationService>.Instance);

            return (svc, email);
        }

        private static async Task<Utilisateur> SeedUserAsync(CongoTravelDbContext ctx, string email)
        {
            var user = new Utilisateur
            {
                NomComplet = "Test User",
                Email = email,
                MotDePasseHash = "hash",
                DefaultUsername = "u_" + Guid.NewGuid().ToString("N"),
                Statut = true,
                DateCreation = DateTime.UtcNow,
                EmailVerified = false
            };
            ctx.Utilisateurs.Add(user);
            await ctx.SaveChangesAsync();
            return user;
        }

        [Fact]
        public async Task IssueAndSend_sends_link_and_stores_hash_only()
        {
            await using var ctx = BuildDb(nameof(IssueAndSend_sends_link_and_stores_hash_only));
            var user = await SeedUserAsync(ctx, "owner@example.com");
            var (svc, email) = CreateService(ctx);

            var sent = await svc.IssueAndSendAsync(user);

            Assert.True(sent);
            email.Verify(e => e.SendEmailVerificationLinkAsync(
                "owner@example.com",
                "Test User",
                It.Is<string>(url => url.StartsWith("https://app.test/verify-email?token="))),
                Times.Once);

            var token = Assert.Single(ctx.EmailVerificationTokens);
            Assert.False(string.IsNullOrWhiteSpace(token.CodeHash));
            Assert.Equal(64, token.CodeHash.Length); // SHA-256 hex
            Assert.Null(token.DateUtilisation);
            Assert.Equal(false, user.EmailVerified);
        }

        [Fact]
        public async Task IssueAndSend_skips_synthetic_email()
        {
            await using var ctx = BuildDb(nameof(IssueAndSend_skips_synthetic_email));
            var user = await SeedUserAsync(ctx, "client_1_abc@congotravel.local");
            var (svc, email) = CreateService(ctx);

            var sent = await svc.IssueAndSendAsync(user);

            Assert.False(sent);
            email.Verify(e => e.SendEmailVerificationLinkAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.Empty(ctx.EmailVerificationTokens);
        }

        [Fact]
        public async Task Verify_marks_email_verified()
        {
            await using var ctx = BuildDb(nameof(Verify_marks_email_verified));
            var user = await SeedUserAsync(ctx, "verify@example.com");
            var (svc, _) = CreateService(ctx);

            // Capture token from URL
            string? capturedUrl = null;
            var email = new Mock<IEmailService>();
            email.Setup(e => e.SendEmailVerificationLinkAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, url) => capturedUrl = url)
                .ReturnsAsync(true);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://app.test",
                    ["FrontendSettings:VerifyEmailPath"] = "/verify-email"
                })
                .Build();

            var service = new EmailVerificationService(
                ctx, email.Object, config, NullLogger<EmailVerificationService>.Instance);

            await service.IssueAndSendAsync(user);
            Assert.NotNull(capturedUrl);
            var raw = capturedUrl!.Split("token=", 2)[1];
            raw = Uri.UnescapeDataString(raw);

            var (ok, status, message) = await service.VerifyAsync(raw);

            Assert.True(ok);
            Assert.Equal(200, status);
            Assert.Contains("vérifiée", message, StringComparison.OrdinalIgnoreCase);
            Assert.True(user.EmailVerified);
            Assert.NotNull(ctx.EmailVerificationTokens.Single().DateUtilisation);
        }

        [Fact]
        public async Task Verify_rejects_invalid_token()
        {
            await using var ctx = BuildDb(nameof(Verify_rejects_invalid_token));
            var (svc, _) = CreateService(ctx);

            var (ok, status, _) = await svc.VerifyAsync("not-a-real-token");

            Assert.False(ok);
            Assert.Equal(400, status);
        }

        [Fact]
        public async Task Verify_rejects_reuse()
        {
            await using var ctx = BuildDb(nameof(Verify_rejects_reuse));
            var user = await SeedUserAsync(ctx, "reuse@example.com");

            string? capturedUrl = null;
            var email = new Mock<IEmailService>();
            email.Setup(e => e.SendEmailVerificationLinkAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, _, url) => capturedUrl = url)
                .ReturnsAsync(true);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FrontendSettings:BaseUrl"] = "https://app.test"
                })
                .Build();

            var service = new EmailVerificationService(
                ctx, email.Object, config, NullLogger<EmailVerificationService>.Instance);

            await service.IssueAndSendAsync(user);
            var raw = Uri.UnescapeDataString(capturedUrl!.Split("token=", 2)[1]);

            Assert.True((await service.VerifyAsync(raw)).Success);
            var second = await service.VerifyAsync(raw);
            Assert.False(second.Success);
            Assert.Equal(400, second.StatusCode);
        }
    }
}
