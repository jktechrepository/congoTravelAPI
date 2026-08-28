using CongoTravel.Data;
using CongoTravel.Extensions;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CongoTravel.Tests
{
    public class HotelPhase5DashboardTests
    {
        private static CongoTravelDbContext BuildDb(string name) =>
            new(new DbContextOptionsBuilder<CongoTravelDbContext>()
                .UseInMemoryDatabase(name)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Mock<ICurrentUserService> Client(int clientId = 42, int userId = 11)
        {
            var user = new Mock<ICurrentUserService>();
            user.SetupGet(x => x.IsStaff).Returns(false);
            user.SetupGet(x => x.IsSuperAdmin).Returns(false);
            user.SetupGet(x => x.UserRole).Returns(UserRoles.CLIENT);
            user.SetupGet(x => x.PrimaryRole).Returns(UserRoles.CLIENT);
            user.SetupGet(x => x.ClientId).Returns(clientId);
            user.SetupGet(x => x.UserId).Returns(userId);
            user.SetupGet(x => x.SocieteId).Returns(999);
            return user;
        }

        [Fact]
        public void HotelTenancyGuard_enforces_client_scope_and_ownership()
        {
            var user = Client();
            var filter = new HotelReservationListFilter { IdClient = 99, IdUtilisateur = 88 };

            Assert.Equal(7,
                HotelTenancyGuard.ResolveEffectiveSocieteIdForFlexPayVerifier(user.Object, 7));
            HotelTenancyGuard.ApplyClientSelfScopeToListFilter(user.Object, filter);
            Assert.Equal(11, filter.IdUtilisateur);
            Assert.Null(filter.IdClient);
            HotelTenancyGuard.EnsureClientOwnsReservation(user.Object, 11, null);
            HotelTenancyGuard.EnsureClientMayQueryByClientId(user.Object, 42);
            Assert.Throws<UnauthorizedAccessException>(() =>
                HotelTenancyGuard.EnsureClientOwnsReservation(user.Object, 12, 43));
            Assert.Throws<UnauthorizedAccessException>(() =>
                HotelTenancyGuard.EnsureClientMayQueryByClientId(user.Object, 43));
        }

        [Fact]
        public async Task ListByClientAsync_returns_reservations_across_societes()
        {
            await using var db = BuildDb(nameof(ListByClientAsync_returns_reservations_across_societes));
            db.HotelReservations.AddRange(
                NewReservation(1, 10, 42, "HOT-A"),
                NewReservation(2, 20, 42, "HOT-B"),
                NewReservation(3, 30, 99, "OTHER"));
            await db.SaveChangesAsync();

            var service = new HotelReservationService(db, new HotelInventoryCancelStrategyFactory(
                new HotelGlobalQuotaCancelStrategy(db), new HotelClassQuotaCancelStrategy(db)));
            var rows = await service.ListByClientAsync(
                42, new HotelReservationListFilter { Status = HotelReservationStatus.CONFIRMED });

            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { 1, 2 }, rows.Select(r => r.IdSociete).OrderBy(x => x));
        }

        [Fact]
        public async Task Dashboard_returns_hotel_inventory_reservation_and_payment_kpis()
        {
            await using var db = BuildDb(nameof(Dashboard_returns_hotel_inventory_reservation_and_payment_kpis));
            var hotel = new Hotel
            {
                IdSociete = 1, CodeHotel = "DASH", Nom = "Hôtel Dashboard",
                Status = HotelStatus.Published
            };
            db.Hotels.Add(hotel);
            await db.SaveChangesAsync();
            db.HotelRoomTypes.Add(new HotelRoomType
            {
                IdSociete = 1, IdHotel = hotel.IdHotel, Code = "STD",
                Libelle = "Standard", Status = HotelStatus.Published
            });
            db.HotelNightAllotments.Add(new HotelNightAllotment
            {
                IdSociete = 1, IdHotel = hotel.IdHotel, IdHotelRoomType = 1,
                NightDate = DateTime.UtcNow.Date, CapaciteTotale = 5,
                PrixNuit = 100m, CodeDevise = "USD", Status = HotelStatus.Published
            });
            var reservation = NewReservation(1, hotel.IdHotel, 42, "DASH-RES");
            reservation.NombreNuits = 2;
            reservation.MontantSousTotal = 200m;
            db.HotelReservations.Add(reservation);
            await db.SaveChangesAsync();
            db.HotelPayments.Add(new HotelPayment
            {
                IdHotelReservation = reservation.IdHotelReservation,
                ReferencePaiement = "DASH-PAY", Provider = "CASH",
                Status = HotelPaymentStatus.SUCCEEDED, Montant = 50m,
                CodeDevise = "USD", MontantTarif = 50m, CodeDeviseTarif = "USD"
            });
            await db.SaveChangesAsync();

            var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var dashboard = await new HotelDashboardService(
                db, NullLogger<HotelDashboardService>.Instance)
                .GetSocieteDashboardAsync(1, start, start.AddMonths(1));

            Assert.Equal(1, dashboard.Summary.HotelsPublies);
            Assert.Equal(1, dashboard.Summary.RoomTypesPublies);
            Assert.Equal(1, dashboard.Summary.AllotmentsActifs);
            Assert.Equal(1, dashboard.Reservations.Confirmed);
            Assert.Equal(50m, dashboard.Summary.MontantAcomptesSuccesMois);
            Assert.Contains(dashboard.RevenuParProvider, x => x.Provider == "CASH");
            Assert.Single(dashboard.Top5HotelsCa);
            Assert.Single(dashboard.ReservationsRecentes);
            Assert.Single(dashboard.PaiementsRecents);
        }

        [Fact]
        public void AddHotelReservations_registers_dashboard_service()
        {
            var services = new ServiceCollection().AddLogging();
            services.AddDbContext<CongoTravelDbContext>(o =>
                o.UseInMemoryDatabase(nameof(AddHotelReservations_registers_dashboard_service)));
            services.AddHotelReservations();
            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IHotelDashboardService>());
        }

        private static HotelReservation NewReservation(
            int idSociete, int idHotel, int idClient, string reference) =>
            new()
            {
                IdSociete = idSociete,
                IdHotel = idHotel,
                IdClient = idClient,
                ReferenceReservation = reference,
                CheckInDate = DateTime.UtcNow.Date.AddDays(5),
                CheckOutDate = DateTime.UtcNow.Date.AddDays(6),
                NombreNuits = 1,
                Status = HotelReservationStatus.CONFIRMED,
                MontantSejour = 100m,
                MontantSousTotal = 100m,
                CodeDevise = "USD"
            };
    }
}
