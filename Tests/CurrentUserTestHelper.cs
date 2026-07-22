using Moq;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Tests
{
    internal static class CurrentUserTestHelper
    {
        public static ICurrentUserService MockRole(string role, bool isStaff = false, int userId = 1)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.IsAuthenticated).Returns(true);
            mock.SetupGet(x => x.UserRole).Returns(role);
            mock.SetupGet(x => x.UserId).Returns(userId);
            mock.SetupGet(x => x.IsStaff).Returns(isStaff || UserRoles.IsStaffRole(role));
            return mock.Object;
        }

        public static ICurrentUserService MockClient(int userId = 10, int clientId = 3) =>
            MockRole(UserRoles.CLIENT, isStaff: false, userId: userId);

        public static ICurrentUserService MockCaissier(int userId = 10) =>
            MockRole(UserRoles.CAISSIER, isStaff: true, userId: userId);

        public static ICurrentUserService MockUnauthenticated()
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(x => x.IsAuthenticated).Returns(false);
            mock.SetupGet(x => x.UserRole).Returns(string.Empty);
            mock.SetupGet(x => x.IsStaff).Returns(false);
            return mock.Object;
        }
    }
}
