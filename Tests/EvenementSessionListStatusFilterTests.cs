using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CongoTravel.Controllers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementSessionListStatusFilterTests
    {
        private static EvenementSessionController CreateController(
            Mock<IEvenementSessionService> sessions,
            Mock<ICurrentUserService> user) =>
            new(
                sessions.Object,
                Mock.Of<IEvenementSessionPhotoService>(),
                Mock.Of<IEvenementAvailabilityService>(),
                user.Object,
                NullLogger<EvenementSessionController>.Instance);

        [Fact]
        public async Task GetList_staff_defaults_to_Published_when_status_omitted()
        {
            var sessions = new Mock<IEvenementSessionService>();
            sessions
                .Setup(s => s.ListAsync(
                    42,
                    It.Is<EvenementSessionListFilter>(f =>
                        f.Status == EvenementSessionStatus.Published
                        && f.TypeEvenement == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<EvenementSessionListItemDto>());

            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(42);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, null, null);

            Assert.IsType<OkObjectResult>(result.Result);
            sessions.Verify(s => s.ListAsync(
                42,
                It.Is<EvenementSessionListFilter>(f =>
                    f.Status == EvenementSessionStatus.Published
                    && f.TypeEvenement == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetList_staff_applies_Draft_status_filter()
        {
            var sessions = new Mock<IEvenementSessionService>();
            sessions
                .Setup(s => s.ListAsync(
                    42,
                    It.Is<EvenementSessionListFilter>(f =>
                        f.Status == EvenementSessionStatus.Draft
                        && f.TypeEvenement == null),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<EvenementSessionListItemDto>());

            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(42);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, "Draft", null);

            Assert.IsType<OkObjectResult>(result.Result);
            sessions.Verify(s => s.ListAsync(
                42,
                It.Is<EvenementSessionListFilter>(f =>
                    f.Status == EvenementSessionStatus.Draft
                    && f.TypeEvenement == null),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetList_public_rejects_Draft_status()
        {
            var sessions = new Mock<IEvenementSessionService>();
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(0);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, "Draft", null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
            sessions.Verify(
                s => s.ListPublishedGlobalAsync(It.IsAny<EvenementSessionListFilter>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetList_public_accepts_Publie_alias_as_Published()
        {
            var sessions = new Mock<IEvenementSessionService>();
            sessions
                .Setup(s => s.ListPublishedGlobalAsync(
                    It.IsAny<EvenementSessionListFilter>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<EvenementSessionListItemDto>());

            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(0);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, "Publié", null);

            Assert.IsType<OkObjectResult>(result.Result);
            sessions.Verify(
                s => s.ListPublishedGlobalAsync(It.IsAny<EvenementSessionListFilter>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetList_staff_applies_type_evenement_filter()
        {
            var sessions = new Mock<IEvenementSessionService>();
            sessions
                .Setup(s => s.ListAsync(
                    42,
                    It.Is<EvenementSessionListFilter>(f =>
                        f.Status == EvenementSessionStatus.Published
                        && f.TypeEvenement == EvenementSessionType.Music),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<EvenementSessionListItemDto>());

            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(true);
            user.SetupGet(u => u.SocieteId).Returns(42);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, null, null, "Music");

            Assert.IsType<OkObjectResult>(result.Result);
            sessions.Verify(s => s.ListAsync(
                42,
                It.Is<EvenementSessionListFilter>(f =>
                    f.Status == EvenementSessionStatus.Published
                    && f.TypeEvenement == EvenementSessionType.Music),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetList_rejects_invalid_type_evenement()
        {
            var sessions = new Mock<IEvenementSessionService>();
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(u => u.IsSuperAdmin).Returns(false);
            user.SetupGet(u => u.IsStaff).Returns(false);
            user.SetupGet(u => u.SocieteId).Returns(0);

            var controller = CreateController(sessions, user);
            var result = await controller.GetList(null, null, null, "WrongType");

            Assert.IsType<BadRequestObjectResult>(result.Result);
            sessions.Verify(
                s => s.ListPublishedGlobalAsync(It.IsAny<EvenementSessionListFilter>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
