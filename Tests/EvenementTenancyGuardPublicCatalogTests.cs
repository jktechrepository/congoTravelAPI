using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementTenancyGuardPublicCatalogTests
    {
        [Fact]
        public void TryResolve_returns_false_when_no_societe_in_token()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(0);

            var ok = EvenementTenancyGuard.TryResolveEffectiveSocieteId(user.Object, null, out var id);

            Assert.False(ok);
            Assert.Null(id);
        }

        [Fact]
        public void TryResolve_returns_jwt_societe_when_present()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(42);

            var ok = EvenementTenancyGuard.TryResolveEffectiveSocieteId(user.Object, null, out var id);

            Assert.True(ok);
            Assert.Equal(42, id);
        }

        [Fact]
        public void TryResolve_super_admin_uses_requested_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(0);

            var ok = EvenementTenancyGuard.TryResolveEffectiveSocieteId(user.Object, 7, out var id);

            Assert.True(ok);
            Assert.Equal(7, id);
        }

        [Fact]
        public void ResolveEffectiveSocieteId_still_throws_without_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(0);

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.ResolveEffectiveSocieteId(user.Object));
        }

        [Fact]
        public void Catalog_client_with_other_idSociete_does_not_throw()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.SocieteId).Returns(10);

            var ok = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(user.Object, 99, out var id);

            Assert.False(ok);
            Assert.Null(id);
        }

        [Fact]
        public void Catalog_staff_with_other_idSociete_throws()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(10);

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(user.Object, 99, out _));
        }

        [Fact]
        public void Catalog_staff_without_query_uses_jwt_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(10);

            var ok = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(user.Object, null, out var id);

            Assert.True(ok);
            Assert.Equal(10, id);
        }

        [Fact]
        public void Catalog_client_without_query_uses_public_path()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(10);

            var ok = EvenementTenancyGuard.TryResolveStaffTenantForCatalogList(user.Object, null, out var id);

            Assert.False(ok);
            Assert.Null(id);
        }
    }
}
