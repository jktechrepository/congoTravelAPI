using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueReservationService : ISiteTouristiqueReservationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueInventoryCancelStrategyFactory _cancelStrategyFactory;
        private readonly IFlexPayRealtimeNotifier _flexPayRealtimeNotifier;
        private readonly ILogger<SiteTouristiqueReservationService> _logger;

        public SiteTouristiqueReservationService(
            CongoTravelDbContext context,
            ISiteTouristiqueInventoryCancelStrategyFactory cancelStrategyFactory,
            IFlexPayRealtimeNotifier flexPayRealtimeNotifier,
            ILogger<SiteTouristiqueReservationService> logger)
        {
            _context = context;
            _cancelStrategyFactory = cancelStrategyFactory;
            _flexPayRealtimeNotifier = flexPayRealtimeNotifier;
            _logger = logger;
        }

        public async Task<SiteTouristiqueReservationResponseDto?> GetByIdAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var reservation = await LoadReservationGraphAsync(idSiteTouristiqueReservation, idSociete, cancellationToken);
            return reservation == null ? null : SiteTouristiqueReservationMapper.ToResponseDto(reservation);
        }

        public async Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueReservationListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var reservations = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(SiteTouristiqueReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<SiteTouristiqueReservationResponseDto?> GetByReferenceAsync(
            string reference,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("Reference est obligatoire.");

            var normalized = reference.Trim();
            var reservation = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdSociete == idSociete && r.ReferenceReservation == normalized,
                    cancellationToken);

            return reservation == null ? null : SiteTouristiqueReservationMapper.ToResponseDto(reservation);
        }

        public Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListBySessionAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueReservationListFilter { IdSiteTouristiqueJournee = idSiteTouristiqueJournee },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>?> ListBySocieteAndSessionAsync(
            int idSociete,
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            var sessionExists = await _context.SiteTouristiqueJournees
                .AsNoTracking()
                .AnyAsync(
                    s => s.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && s.IdSociete == idSociete,
                    cancellationToken);

            if (!sessionExists)
                return null;

            return await ListBySessionAsync(idSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByStatusAsync(
            SiteTouristiqueReservationStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueReservationListFilter { Status = status },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var reservations = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.DateCreation.Date == day)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(SiteTouristiqueReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueReservationListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var reservations = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.DateCreation >= start && r.DateCreation <= end)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(SiteTouristiqueReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueTicketResponseDto>?> GetTicketsByReservationAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservation == null)
                return null;

            return reservation.Lines
                .SelectMany(l => l.Tickets)
                .OrderBy(t => t.IdSiteTouristiqueTicket)
                .Select(SiteTouristiqueReservationMapper.ToTicketResponse)
                .ToList();
        }

        public async Task<SiteTouristiqueCancelReservationResponseDto> CancelAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (snapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");
            }

            if (snapshot.Status == SiteTouristiqueReservationStatus.CANCELLED)
            {
                var cancelled = await LoadReservationGraphAsync(idSiteTouristiqueReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");

                return BuildCancelResponse(cancelled, alreadyCancelled: true, ticketsVoided: 0);
            }

            if (snapshot.Status == SiteTouristiqueReservationStatus.EXPIRED)
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
                    DetachTrackedReservation(idSiteTouristiqueReservation);

                    var reservation = await _context.SiteTouristiqueReservations
                        .Include(r => r.Lines)
                            .ThenInclude(l => l.Tickets)
                        .Include(r => r.Payments)
                        .FirstOrDefaultAsync(
                            r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                            cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation site touristique {idSiteTouristiqueReservation} introuvable pour la société {idSociete}.");
                    }

                    if (reservation.Status == SiteTouristiqueReservationStatus.CANCELLED)
                    {
                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return BuildCancelResponse(reservation, alreadyCancelled: true, ticketsVoided: 0);
                    }

                    EnsureCancellable(reservation);

                    var journee = await _context.SiteTouristiqueJournees
                        .FirstOrDefaultAsync(
                            s => s.IdSiteTouristiqueJournee == reservation.IdSiteTouristiqueJournee && s.IdSociete == idSociete,
                            cancellationToken);

                    if (journee == null)
                    {
                        throw new InvalidOperationException(
                            "Session associée à la réservation introuvable.");
                    }

                    var fromConfirmed = reservation.Status == SiteTouristiqueReservationStatus.CONFIRMED;
                    var wasHold = reservation.Status == SiteTouristiqueReservationStatus.HOLD;
                    var cancelStrategy = _cancelStrategyFactory.GetStrategy(journee.InventoryMode);
                    await cancelStrategy.ReleaseReservationAsync(
                        new SiteTouristiqueInventoryCancelRequest
                        {
                            Reservation = reservation,
                            Journee = journee,
                            FromConfirmedSale = fromConfirmed
                        },
                        cancellationToken);

                    var ticketsVoided = VoidUnusedTickets(reservation);
                    MarkPaymentsRefunded(reservation);
                    var flexPayFailedOrders = wasHold
                        ? MarkPendingFlexPayPaymentsFailed(reservation)
                        : Array.Empty<(string OrderNumber, int UserId)>();

                    reservation.Status = SiteTouristiqueReservationStatus.CANCELLED;
                    reservation.ExpiresAtUtc = null;
                    reservation.DateModification = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    await TryNotifyFlexPayFailedAsync(flexPayFailedOrders, cancellationToken);

                    _logger.LogInformation(
                        "Réservation site touristique annulée — Id={Id}, TicketsVoided={TicketsVoided}, FromConfirmed={FromConfirmed}",
                        reservation.IdSiteTouristiqueReservation,
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

        private static void EnsureCancellable(Models.SiteTouristique.SiteTouristiqueReservation reservation)
        {
            if (reservation.Status is not (SiteTouristiqueReservationStatus.HOLD or SiteTouristiqueReservationStatus.CONFIRMED))
            {
                throw new InvalidOperationException(
                    $"Impossible d'annuler une réservation au statut {reservation.Status}.");
            }

            if (reservation.Status == SiteTouristiqueReservationStatus.CONFIRMED)
            {
                var hasUsedTicket = reservation.Lines
                    .SelectMany(l => l.Tickets)
                    .Any(t => t.Status == SiteTouristiqueTicketStatus.USED);

                if (hasUsedTicket)
                {
                    throw new InvalidOperationException(
                        "Impossible d'annuler : au moins un ticket a déjà été utilisé.");
                }
            }

            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");
        }

        private static int VoidUnusedTickets(Models.SiteTouristique.SiteTouristiqueReservation reservation)
        {
            var count = 0;
            foreach (var ticket in reservation.Lines.SelectMany(l => l.Tickets))
            {
                if (ticket.Status != SiteTouristiqueTicketStatus.ISSUED)
                    continue;

                ticket.Status = SiteTouristiqueTicketStatus.VOID;
                count++;
            }

            return count;
        }

        private static void MarkPaymentsRefunded(Models.SiteTouristique.SiteTouristiqueReservation reservation)
        {
            var utcNow = DateTime.UtcNow;
            foreach (var payment in reservation.Payments.Where(p => p.Status == SiteTouristiquePaymentStatus.SUCCEEDED))
            {
                payment.Status = SiteTouristiquePaymentStatus.REFUNDED;
                payment.DateModification = utcNow;
            }
        }

        /// <summary>
        /// Annulation d’un HOLD avec paiement FlexPay encore PENDING (ex. abandon MM depuis l’app).
        /// </summary>
        private static IReadOnlyList<(string OrderNumber, int UserId)> MarkPendingFlexPayPaymentsFailed(
            Models.SiteTouristique.SiteTouristiqueReservation reservation)
        {
            var utcNow = DateTime.UtcNow;
            var toNotify = new List<(string OrderNumber, int UserId)>();

            foreach (var payment in reservation.Payments.Where(p =>
                         p.Status == SiteTouristiquePaymentStatus.PENDING
                         && string.Equals(
                             p.Provider,
                             SiteTouristiqueFlexPayConstants.Provider,
                             StringComparison.OrdinalIgnoreCase)))
            {
                payment.Status = SiteTouristiquePaymentStatus.FAILED;
                payment.DateModification = utcNow;

                if (!string.IsNullOrWhiteSpace(payment.ProviderTxRef)
                    && reservation.IdUtilisateur is > 0)
                {
                    toNotify.Add((payment.ProviderTxRef.Trim(), reservation.IdUtilisateur.Value));
                }
            }

            return toNotify;
        }

        private async Task TryNotifyFlexPayFailedAsync(
            IReadOnlyList<(string OrderNumber, int UserId)> orders,
            CancellationToken cancellationToken)
        {
            foreach (var (orderNumber, userId) in orders)
            {
                try
                {
                    await _flexPayRealtimeNotifier.NotifyPaymentFailedAsync(
                        userId,
                        orderNumber,
                        "Paiement annulé.",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "SignalR FlexPayPaymentFailed (cancel réservation) non envoyé pour order {OrderNumber}",
                        orderNumber);
                }
            }
        }

        private void DetachTrackedReservation(int idSiteTouristiqueReservation)
        {
            var tracked = _context.ChangeTracker
                .Entries<Models.SiteTouristique.SiteTouristiqueReservation>()
                .Where(e => e.Entity.IdSiteTouristiqueReservation == idSiteTouristiqueReservation)
                .ToList();

            foreach (var entry in tracked)
                entry.State = EntityState.Detached;
        }

        private IQueryable<Models.SiteTouristique.SiteTouristiqueReservation> BuildListQuery(
            int idSociete,
            SiteTouristiqueReservationListFilter? filter)
        {
            var query = _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter?.IdSiteTouristiqueJournee.HasValue == true)
                query = query.Where(r => r.IdSiteTouristiqueJournee == filter.IdSiteTouristiqueJournee.Value);

            if (!string.IsNullOrWhiteSpace(filter?.CustomerRef))
            {
                var customerRef = filter.CustomerRef.Trim();
                query = query.Where(r => r.CustomerRef == customerRef);
            }

            return query.OrderByDescending(r => r.DateCreation);
        }

        private async Task<Models.SiteTouristique.SiteTouristiqueReservation?> LoadReservationGraphAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private static SiteTouristiqueCancelReservationResponseDto BuildCancelResponse(
            Models.SiteTouristique.SiteTouristiqueReservation reservation,
            bool alreadyCancelled,
            int ticketsVoided) =>
            new()
            {
                Reservation = SiteTouristiqueReservationMapper.ToResponseDto(reservation),
                AlreadyCancelled = alreadyCancelled,
                TicketsVoided = ticketsVoided
            };
    }
}
