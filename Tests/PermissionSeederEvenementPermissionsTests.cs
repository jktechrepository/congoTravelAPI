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

        [Fact]
        public async Task SeedPermissionsAsync_assigns_hold_and_confirm_to_client_for_flexpay()
        {
            await using var ctx = BuildDb(nameof(SeedPermissionsAsync_assigns_hold_and_confirm_to_client_for_flexpay));

            ctx.Roles.Add(new Role { IdRole = 1, Nom = "Super-Admin", Statut = true });
            ctx.Roles.Add(new Role { IdRole = 10, Nom = "Client", Statut = true });
            await ctx.SaveChangesAsync();

            await PermissionSeeder.SeedPermissionsAsync(ctx);

            var clientPermissionNames = await ctx.RolePermissions
                .Where(rp => rp.IdRole == 10)
                .Select(rp => rp.Permission!.Nom)
                .ToListAsync();

            Assert.Contains("Evenement.Hold.Create", clientPermissionNames);
            Assert.Contains("Evenement.Reservation.Confirm", clientPermissionNames);
            Assert.Contains("Evenement.Session.Read", clientPermissionNames);
            Assert.DoesNotContain("Evenement.Session.Write", clientPermissionNames);
            Assert.DoesNotContain("Evenement.Ticket.Check", clientPermissionNames);
        }
    }
}
