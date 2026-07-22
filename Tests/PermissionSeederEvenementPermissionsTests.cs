using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using Xunit;

namespace CongoTravel.Tests
{
    public class PermissionSeederEvenementPermissionsTests
    {
        private static readonly string[] ExpectedEvenementPermissionNames =
        {
            "Evenement.Session.Read",
            "Evenement.Session.Write",
            "Evenement.Hold.Create",
            "Evenement.Reservation.Confirm",
            "Evenement.Ticket.Check",
            "Evenement.Ticket.Use",
            "Evenement.Dashboard.Read"
        };

        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task SeedPermissionsAsync_creates_evenement_permissions_and_assigns_admin()
        {
            await using var ctx = BuildDb(nameof(SeedPermissionsAsync_creates_evenement_permissions_and_assigns_admin));

            ctx.Roles.Add(new Role { IdRole = 1, Nom = "Super-Admin", Statut = true });
            ctx.Roles.Add(new Role { IdRole = 2, Nom = "Admin", Statut = true });
            await ctx.SaveChangesAsync();

            await PermissionSeeder.SeedPermissionsAsync(ctx);

            var evenementNames = await ctx.Permissions
                .Where(p => p.Categorie == "Evenement")
                .Select(p => p.Nom)
                .ToListAsync();

            Assert.Equal(ExpectedEvenementPermissionNames.Length, evenementNames.Count);
            foreach (var expected in ExpectedEvenementPermissionNames)
                Assert.Contains(expected, evenementNames);

            var adminPermissionNames = await ctx.RolePermissions
                .Where(rp => rp.IdRole == 2)
                .Select(rp => rp.Permission!.Nom)
                .ToListAsync();

            foreach (var expected in ExpectedEvenementPermissionNames)
                Assert.Contains(expected, adminPermissionNames);
        }
    }
}
