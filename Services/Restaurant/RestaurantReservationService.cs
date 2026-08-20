using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant.Strategies;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantReservationService : IRestaurantReservationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantInventoryCancelStrategyFactory _cancelStrategyFactory;
        private readonly ILogger<RestaurantReservationService> _logger;

        public RestaurantReservationService(
            CongoTravelDbContext context,
            IRestaurantInventoryCancelStrategyFactory cancelStrategyFactory,
            ILogger<RestaurantReservationService> logger)
        {
            _context = context;
            _cancelStrategyFactory = cancelStrategyFactory;
            _logger = logger;
        }

        public async Task<RestaurantReservationResponseDto?> GetByIdAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var reservation = await LoadReservationGraphAsync(idRestaurantReservation, idSociete, cancellationToken);
            return reservation == null ? null : RestaurantReservationMapper.ToResponseDto(reservation);
        }

        public async Task<IReadOnlyList<RestaurantReservationListItemDto>> ListAsync(
            int idSociete,
            RestaurantReservationListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var reservations = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(RestaurantReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<RestaurantCancelReservationResponseDto> CancelAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await _context.RestaurantReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (snapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");
            }

            if (snapshot.Status == RestaurantReservationStatus.CANCELLED)
            {
                var cancelled = await LoadReservationGraphAsync(idRestaurantReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");

                return BuildCancelResponse(cancelled, alreadyCancelled: true, ticketsVoided: 0);
            }

            if (snapshot.Status == RestaurantReservationStatus.EXPIRED)
            {
                throw new InvalidOperationException(
                    "Impossible d'annuler une réservation expirée (stock déjà restitué).");
            }

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    DetachTrackedReservation(idRestaurantReservation);

                    var reservation = await _context.RestaurantReservations
                        .Include(r => r.Lines)
                            .ThenInclude(l => l.Tickets)
                        .Include(r => r.Payments)
                        .FirstOrDefaultAsync(
                            r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                            cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation restaurant {idRestaurantReservation} introuvable pour la société {idSociete}.");
                    }

                    if (reservation.Status == RestaurantReservationStatus.CANCELLED)
                    {
                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return BuildCancelResponse(reservation, alreadyCancelled: true, ticketsVoided: 0);
                    }

                    EnsureCancellable(reservation);

                    var creneau = await _context.RestaurantCreneaux
                        .FirstOrDefaultAsync(
                            c => c.IdRestaurantCreneau == reservation.IdRestaurantCreneau && c.IdSociete == idSociete,
                            cancellationToken);

                    if (creneau == null)
                    {
                        throw new InvalidOperationException(
                            "Créneau associé à la réservation introuvable.");
                    }

                    var fromConfirmed = reservation.Status == RestaurantReservationStatus.CONFIRMED;
                    var wasHold = reservation.Status == RestaurantReservationStatus.HOLD;
                    var cancelStrategy = _cancelStrategyFactory.GetStrategy(creneau.InventoryMode);
                    await cancelStrategy.ReleaseReservationAsync(
                        new RestaurantInventoryCancelRequest
                        {
                            Reservation = reservation,
                            Creneau = creneau,
                            FromConfirmedSale = fromConfirmed
                        },
                        cancellationToken);

                    var ticketsVoided = VoidUnusedTickets(reservation);
                    MarkPaymentsRefunded(reservation);

                    var shouldPurge = wasHold
                        && reservation.Payments.All(p => p.Status != RestaurantPaymentStatus.SUCCEEDED);

                    if (shouldPurge)
                    {
                        reservation.Status = RestaurantReservationStatus.CANCELLED;
                        reservation.ExpiresAtUtc = null;
                        reservation.DateModification = DateTime.UtcNow;
                        var purgedResponse = BuildCancelResponse(reservation, alreadyCancelled: false, ticketsVoided);
                        HardDeleteTrackedReservation(reservation);
                        await _context.SaveChangesAsync(cancellationToken);

                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        _logger.LogInformation(
                            "Réservation restaurant HOLD annulée et purgée — Id={Id}, TicketsVoided={TicketsVoided}",
                            idRestaurantReservation,
                            ticketsVoided);

                        return purgedResponse;
                    }

                    reservation.Status = RestaurantReservationStatus.CANCELLED;
                    reservation.ExpiresAtUtc = null;
                    reservation.DateModification = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Réservation restaurant annulée — Id={Id}, TicketsVoided={TicketsVoided}, FromConfirmed={FromConfirmed}",
                        reservation.IdRestaurantReservation,
                        ticketsVoided,
                        fromConfirmed);

                    return BuildCancelResponse(reservation, alreadyCancelled: false, ticketsVoided);
                }
                catch
                {
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
                finally
                {
                    if (transaction != null)
                        await transaction.DisposeAsync();
                }
            });
        }

        public async Task<bool> PurgeNeverConfirmedAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            DetachTrackedReservation(idRestaurantReservation);

            var reservation = await _context.RestaurantReservations
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservation == null)
                return false;

            if (reservation.Status == RestaurantReservationStatus.CONFIRMED)
            {
                _logger.LogWarning(
                    "Purge refusée — réservation restaurant {Id} encore CONFIRMED.",
                    idRestaurantReservation);
                return false;
            }

            if (reservation.Status is not (
                    RestaurantReservationStatus.HOLD
                    or RestaurantReservationStatus.CANCELLED
                    or RestaurantReservationStatus.EXPIRED))
            {
                _logger.LogWarning(
                    "Purge refusée — réservation restaurant {Id} statut {Status} non éligible.",
                    idRestaurantReservation,
                    reservation.Status);
                return false;
            }

            if (reservation.Payments.Any(p => p.Status == RestaurantPaymentStatus.SUCCEEDED))
            {
                _logger.LogWarning(
                    "Purge refusée — réservation restaurant {Id} a un paiement SUCCEEDED.",
                    idRestaurantReservation);
                return false;
            }

            var orderNumbers = reservation.Payments
                .Select(p => p.ProviderTxRef)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct()
                .ToList();

            HardDeleteTrackedReservation(reservation);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Réservation restaurant jamais confirmée purgée — Id={Id}, Orders={Orders}",
                idRestaurantReservation,
                string.Join(',', orderNumbers));

            return true;
        }

        private void HardDeleteTrackedReservation(Models.Restaurant.RestaurantReservation reservation)
        {
            var tickets = reservation.Lines.SelectMany(l => l.Tickets).ToList();
            if (tickets.Count > 0)
                _context.RestaurantTickets.RemoveRange(tickets);

            if (reservation.Payments.Count > 0)
                _context.RestaurantPayments.RemoveRange(reservation.Payments);

            _context.RestaurantReservations.Remove(reservation);
        }

        private static void EnsureCancellable(Models.Restaurant.RestaurantReservation reservation)
        {
            if (reservation.Status is not (RestaurantReservationStatus.HOLD or RestaurantReservationStatus.CONFIRMED))
            {
                throw new InvalidOperationException(
                    $"Impossible d'annuler une réservation au statut {reservation.Status}.");
            }

            if (reservation.Status == RestaurantReservationStatus.CONFIRMED)
            {
                var hasUsedTicket = reservation.Lines
                    .SelectMany(l => l.Tickets)
                    .Any(t => t.Status == RestaurantTicketStatus.USED);

                if (hasUsedTicket)
                {
                    throw new InvalidOperationException(
                        "Impossible d'annuler : au moins un ticket a déjà été utilisé.");
                }
            }

            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");
        }

        private static int VoidUnusedTickets(Models.Restaurant.RestaurantReservation reservation)
        {
            var count = 0;
            foreach (var ticket in reservation.Lines.SelectMany(l => l.Tickets))
            {
                if (ticket.Status != RestaurantTicketStatus.ISSUED)
                    continue;

                ticket.Status = RestaurantTicketStatus.VOID;
                count++;
            }

            return count;
        }

        private static RestaurantCancelReservationResponseDto BuildCancelResponse(
            Models.Restaurant.RestaurantReservation reservation,
            bool alreadyCancelled,
            int ticketsVoided) =>
            new()
            {
                Reservation = RestaurantReservationMapper.ToResponseDto(reservation),
                AlreadyCancelled = alreadyCancelled,
                TicketsVoided = ticketsVoided
            };

        private static void MarkPaymentsRefunded(Models.Restaurant.RestaurantReservation reservation)
        {
            var utcNow = DateTime.UtcNow;
            foreach (var payment in reservation.Payments.Where(p => p.Status == RestaurantPaymentStatus.SUCCEEDED))
            {
                payment.Status = RestaurantPaymentStatus.REFUNDED;
                payment.DateModification = utcNow;
            }
        }

        private void DetachTrackedReservation(int idRestaurantReservation)
        {
            var tracked = _context.ChangeTracker
                .Entries<Models.Restaurant.RestaurantReservation>()
                .Where(e => e.Entity.IdRestaurantReservation == idRestaurantReservation)
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;
        }

        private IQueryable<Models.Restaurant.RestaurantReservation> BuildListQuery(
            int idSociete,
            RestaurantReservationListFilter? filter)
        {
            var query = _context.RestaurantReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter?.IdRestaurant.HasValue == true)
                query = query.Where(r => r.IdRestaurant == filter.IdRestaurant.Value);

            if (filter?.IdRestaurantCreneau.HasValue == true)
                query = query.Where(r => r.IdRestaurantCreneau == filter.IdRestaurantCreneau.Value);

            if (!string.IsNullOrWhiteSpace(filter?.CustomerRef))
            {
                var customerRef = filter.CustomerRef.Trim();
                query = query.Where(r => r.CustomerRef == customerRef);
            }

            if (filter?.IdUtilisateur.HasValue == true)
                query = query.Where(r => r.IdUtilisateur == filter.IdUtilisateur.Value);

            if (filter?.IdClient.HasValue == true)
                query = query.Where(r => r.IdClient == filter.IdClient.Value);

            return query.OrderByDescending(r => r.DateCreation);
        }

        private async Task<Models.Restaurant.RestaurantReservation?> LoadReservationGraphAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.RestaurantReservations
                .AsNoTracking()
                .Include(r => r.Societe)
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }
    }
}
