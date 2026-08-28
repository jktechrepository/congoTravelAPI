using CongoTravel.Controllers;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class EvenementTicketControllerClientTenancyTests
    {
        [Fact]
        public async Task GetBySocieteAndReservation_client_cross_org_returns_ok_when_owner()
        {
            var ticketService = new Mock<IEvenementTicketService>();
            var reservationService = new Mock<IEvenementReservationService>();
            var currentUser = ClientUser(jwtSociete: 1, userId: 11, clientId: 1);

            reservationService
                .Setup(s => s.GetByIdAsync(3, 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementReservationResponseDto
                {
                    IdEvenementReservation = 3,
                    IdSociete = 4,
                    IdUtilisateur = 11,
                    IdClient = 1
                });

            var expected = new List<EvenementTicketListItemDto>
            {
                new() { IdEvenementTicket = 10, IdEvenementReservation = 3 }
            };
            ticketService
                .Setup(s => s.ListBySocieteAndReservationAsync(4, 3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var controller = CreateController(ticketService, reservationService, currentUser);

            var action = await controller.GetBySocieteAndReservation(4, 3);

            var ok = Assert.IsType<OkObjectResult>(action.Result);
            var tickets = Assert.IsAssignableFrom<IEnumerable<EvenementTicketListItemDto>>(ok.Value).ToList();
            Assert.Single(tickets);
            Assert.Equal(10, tickets[0].IdEvenementTicket);
        }

        [Fact]
        public async Task GetBySocieteAndReservation_client_foreign_reservation_forbids()
        {
            var ticketService = new Mock<IEvenementTicketService>();
            var reservationService = new Mock<IEvenementReservationService>();
            var currentUser = ClientUser(jwtSociete: 1, userId: 11, clientId: 1);

            reservationService
                .Setup(s => s.GetByIdAsync(3, 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementReservationResponseDto
                {
                    IdEvenementReservation = 3,
                    IdSociete = 4,
                    IdUtilisateur = 99,
                    IdClient = 2
                });

            var controller = CreateController(ticketService, reservationService, currentUser);

            var action = await controller.GetBySocieteAndReservation(4, 3);

            Assert.IsType<ForbidResult>(action.Result);
            ticketService.Verify(
                s => s.ListBySocieteAndReservationAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetBySocieteAndReservation_staff_other_societe_forbids()
        {
            var ticketService = new Mock<IEvenementTicketService>();
            var reservationService = new Mock<IEvenementReservationService>();
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(u => u.IsSuperAdmin).Returns(false);
            currentUser.SetupGet(u => u.IsStaff).Returns(true);
            currentUser.SetupGet(u => u.UserRole).Returns(UserRoles.CAISSIER);
            currentUser.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CAISSIER);
            currentUser.SetupGet(u => u.SocieteId).Returns(1);

            var controller = CreateController(ticketService, reservationService, currentUser);

            var action = await controller.GetBySocieteAndReservation(4, 3);

            Assert.IsType<ForbidResult>(action.Result);
            reservationService.Verify(
                s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByReservation_client_with_organizer_query_returns_ok()
        {
            var ticketService = new Mock<IEvenementTicketService>();
            var reservationService = new Mock<IEvenementReservationService>();
            var currentUser = ClientUser(jwtSociete: 1, userId: 11, clientId: 1);

            reservationService
                .Setup(s => s.GetByIdAsync(3, 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EvenementReservationResponseDto
                {
                    IdEvenementReservation = 3,
                    IdSociete = 4,
                    IdUtilisateur = 11,
                    IdClient = 1
                });

            ticketService
                .Setup(s => s.ListByReservationAsync(3, 4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<EvenementTicketListItemDto>
                {
                    new() { IdEvenementTicket = 10, IdEvenementReservation = 3 }
                });

            var controller = CreateController(ticketService, reservationService, currentUser);

            var action = await controller.GetByReservation(3, idSociete: 4);

            Assert.IsType<OkObjectResult>(action.Result);
        }

        private static Mock<ICurrentUserService> ClientUser(int jwtSociete, int userId, int clientId)
        {
            var currentUser = new Mock<ICurrentUserService>();
            currentUser.SetupGet(u => u.IsSuperAdmin).Returns(false);
            currentUser.SetupGet(u => u.IsStaff).Returns(false);
            currentUser.SetupGet(u => u.UserRole).Returns(UserRoles.CLIENT);
            currentUser.SetupGet(u => u.PrimaryRole).Returns(UserRoles.CLIENT);
            currentUser.SetupGet(u => u.SocieteId).Returns(jwtSociete);
            currentUser.SetupGet(u => u.UserId).Returns(userId);
            currentUser.SetupGet(u => u.ClientId).Returns(clientId);
            return currentUser;
        }

        private static EvenementTicketController CreateController(
            Mock<IEvenementTicketService> ticketService,
            Mock<IEvenementReservationService> reservationService,
            Mock<ICurrentUserService> currentUser) =>
            new(
                ticketService.Object,
                reservationService.Object,
                currentUser.Object,
                NullLogger<EvenementTicketController>.Instance);
    }
}
