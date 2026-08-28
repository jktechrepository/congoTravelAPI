using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
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
        public void FlexPayVerifier_client_may_pass_organizer_idSociete()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.SocieteId).Returns(1);

            var id = EvenementTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(user.Object, 4);

            Assert.Equal(4, id);
        }

        [Fact]
        public void FlexPayVerifier_staff_with_other_idSociete_throws()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.SocieteId).Returns(1);

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(user.Object, 4));
        }

        [Fact]
        public void ApplyClientSelfScope_forces_jwt_user_and_clears_query_ids()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.UserId).Returns(11);
            user.SetupGet(u => u.ClientId).Returns(1);

            var filter = new EvenementReservationListFilter
            {
                IdUtilisateur = 999,
                IdClient = 888
            };
            EvenementTenancyGuard.ApplyClientSelfScopeToListFilter(user.Object, filter);

            Assert.Equal(11, filter.IdUtilisateur);
            Assert.Null(filter.IdClient);
        }

        [Fact]
        public void ApplyClientSelfScope_staff_keeps_query_filters()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.UserId).Returns(5);

            var filter = new EvenementReservationListFilter { IdClient = 1, IdUtilisateur = 11 };
            EvenementTenancyGuard.ApplyClientSelfScopeToListFilter(user.Object, filter);

            Assert.Equal(1, filter.IdClient);
            Assert.Equal(11, filter.IdUtilisateur);
        }

        [Fact]
        public void EnsureClientOwnsReservation_allows_own_user()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.UserId).Returns(11);
            user.SetupGet(u => u.ClientId).Returns(1);

            EvenementTenancyGuard.EnsureClientOwnsReservation(user.Object, 11, 1);
        }

        [Fact]
        public void EnsureClientOwnsReservation_rejects_foreign()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.UserId).Returns(11);
            user.SetupGet(u => u.ClientId).Returns(1);

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.EnsureClientOwnsReservation(user.Object, 99, 2));
        }

        [Fact]
        public void EnsureClientMayQueryByClientId_allows_own_client()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.ClientId).Returns(1);

            EvenementTenancyGuard.EnsureClientMayQueryByClientId(user.Object, 1);
        }

        [Fact]
        public void EnsureClientMayQueryByClientId_rejects_other_client()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.ClientId).Returns(1);

            Assert.Throws<UnauthorizedAccessException>(() =>
                EvenementTenancyGuard.EnsureClientMayQueryByClientId(user.Object, 2));
        }

        [Fact]
        public void EnsureClientMayQueryByClientId_staff_may_query_any()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.ClientId).Returns((int?)null);

            EvenementTenancyGuard.EnsureClientMayQueryByClientId(user.Object, 99);
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
