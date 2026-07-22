using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CongoTravel.Data;
using CongoTravel.Models;
using Xunit;

namespace CongoTravel.Tests
{
    public class PermissionSeederClientPermissionsTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        [Fact]
        public async Task SeedPermissionsAsync_modernizes_client_role_permissions()
        {
            await using var ctx = BuildDb(nameof(SeedPermissionsAsync_modernizes_client_role_permissions));

            var clientRole = new Role { IdRole = 5, Nom = "Client", Statut = true };
            ctx.Roles.Add(clientRole);

            var permissions = new[]
            {
                Perm("Facture.Read", "Facture", "Read"),
                Perm("CategorieClient.Read", "CategorieClient", "Read"),
                Perm("Client.Read", "Client", "Read"),
                Perm("Client.ReadAll", "Client", "ReadAll"),
                Perm("PlainteClient.Create", "PlainteClient", "Create"),
                Perm("ClientDashboard.ReadAll", "ClientDashboard", "ReadAll"),
                Perm("Reservation.Create", "Reservation", "Create"),
                Perm("Reservation.Read", "Reservation", "Read"),
                Perm("Paiement.Read", "Paiement", "Read"),
                Perm("Billet.Read", "Billet", "Read"),
                Perm("Voyage.Read", "Voyage", "Read"),
                Perm("Destination.Read", "Destination", "Read"),
                Perm("Utilisateur.DeactivateSelf", "Utilisateur", "DeactivateSelf")
            };
            ctx.Permissions.AddRange(permissions);
            await ctx.SaveChangesAsync();

            foreach (var p in permissions)
            {
                ctx.RolePermissions.Add(new RolePermission
                {
                    IdRole = clientRole.IdRole,
                    IdPermission = p.IdPermission,
                    DateAttribution = DateTime.UtcNow
                });
            }
            await ctx.SaveChangesAsync();

            await PermissionSeeder.SeedPermissionsAsync(ctx);

            var clientPermissionNames = await ctx.RolePermissions
                .Where(rp => rp.IdRole == clientRole.IdRole)
                .Select(rp => rp.Permission!.Nom)
                .ToListAsync();

            Assert.DoesNotContain("Facture.Read", clientPermissionNames);
            Assert.DoesNotContain("CategorieClient.Read", clientPermissionNames);
            Assert.Contains("Reservation.Create", clientPermissionNames);
            Assert.Contains("Utilisateur.DeactivateSelf", clientPermissionNames);
            Assert.Contains("ClientDashboard.ReadAll", clientPermissionNames);
        }

        private static Permission Perm(string nom, string categorie, string action) =>
            new()
            {
                Nom = nom,
                Categorie = categorie,
                Action = action,
                Description = nom,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
    }
}
