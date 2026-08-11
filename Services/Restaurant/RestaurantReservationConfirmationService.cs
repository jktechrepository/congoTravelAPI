using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant.Strategies;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantReservationConfirmationService : IRestaurantReservationConfirmationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantInventoryConfirmStrategyFactory _confirmStrategyFactory;
        private readonly ILogger<RestaurantReservationConfirmationService> _logger;

        public RestaurantReservationConfirmationService(
            CongoTravelDbContext context,
            IRestaurantInventoryConfirmStrategyFactory confirmStrategyFactory,
            ILogger<RestaurantReservationConfirmationService> logger)
        {
            _context = context;
            _confirmStrategyFactory = confirmStrategyFactory;
            _logger = logger;
        }

        public void EnsureHoldConfirmable(RestaurantReservation reservation)
        {
            if (reservation.Status != RestaurantReservationStatus.HOLD)
            {
                throw new InvalidOperationException(
                    $"Impossible de confirmer une réservation au statut {reservation.Status}.");
            }

            if (reservation.ExpiresAtUtc.HasValue && reservation.ExpiresAtUtc.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Le hold a expiré. Créez une nouvelle réservation.");
            }

            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");

            if (reservation.MontantSousTotal <= 0)
            {
                throw new InvalidOperationException(
                    "Montant d'acompte invalide (doit être strictement positif).");
            }
        }

        public async Task ConfirmHoldAndMarkPaymentSucceededAsync(
            RestaurantReservation reservation,
            RestaurantPayment payment,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            EnsureHoldConfirmable(reservation);

            var creneau = await _context.RestaurantCreneaux
                .Include(c => c.GlobalQuota)
                .FirstOrDefaultAsync(
                    c => c.IdRestaurantCreneau == reservation.IdRestaurantCreneau
                         && c.IdSociete == idSociete,
                    cancellationToken);

            if (creneau == null)
            {
                throw new InvalidOperationException(
                    "Créneau associé à la réservation introuvable.");
            }

            if (creneau.Status != RestaurantStatus.Published)
            {
                throw new InvalidOperationException(
                    "Le créneau doit être publié pour confirmer la réservation.");
            }

            var confirmStrategy = _confirmStrategyFactory.GetStrategy(creneau.InventoryMode);
            await confirmStrategy.ConfirmHoldAsync(
                new RestaurantInventoryConfirmRequest
                {
                    Reservation = reservation,
                    Creneau = creneau
                },
                cancellationToken);

            var utcNow = DateTime.UtcNow;
            reservation.Status = RestaurantReservationStatus.CONFIRMED;
            reservation.ExpiresAtUtc = null;
            reservation.DateModification = utcNow;

            payment.Status = RestaurantPaymentStatus.SUCCEEDED;
            payment.DateModification = utcNow;

            if (payment.IdRestaurantPayment == 0)
            {
                payment.IdRestaurantReservation = reservation.IdRestaurantReservation;
                payment.DateCreation = utcNow;
                _context.RestaurantPayments.Add(payment);
            }

            _logger.LogInformation(
                "Réservation restaurant confirmée — IdReservation={Id}, Provider={Provider}, Couverts={Couverts}",
                reservation.IdRestaurantReservation,
                payment.Provider,
                reservation.NombreCouverts);
        }
    }
}
