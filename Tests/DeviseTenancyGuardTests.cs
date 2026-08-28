using CongoTravel.Helpers;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class DeviseTenancyGuardTests
    {
        [Fact]
        public void CanReadDeviseDataForSociete_super_admin_allows_any_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(0);

            Assert.True(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 12));
        }

        [Fact]
        public void CanReadDeviseDataForSociete_client_allows_cross_tenant_read()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.SocieteId).Returns(1);

            Assert.True(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 12));
        }

        [Fact]
        public void CanReadDeviseDataForSociete_client_rejects_invalid_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(u => u.SocieteId).Returns(1);

            Assert.False(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 0));
        }

        [Fact]
        public void CanReadDeviseDataForSociete_staff_allows_own_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.CAISSIER);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(12);

            Assert.True(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 12));
        }

        [Fact]
        public void CanReadDeviseDataForSociete_staff_denies_other_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.ADMIN);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(1);

            Assert.False(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 12));
        }

        [Fact]
        public void CanReadDeviseDataForSociete_financier_denies_other_societe()
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.UserRole).Returns(UserRoles.FINANCIER);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(5);

            Assert.False(DeviseTenancyGuard.CanReadDeviseDataForSociete(user.Object, 12));
        }
    }
}
