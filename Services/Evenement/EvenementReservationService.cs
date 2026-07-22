using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;

namespace CongoTravel.Services.Evenement
{
    public class EvenementReservationService : IEvenementReservationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEvenementInventoryCancelStrategyFactory _cancelStrategyFactory;
        private readonly ILogger<EvenementReservationService> _logger;

        public EvenementReservationService(
            CongoTravelDbContext context,
            IEvenementInventoryCancelStrategyFactory cancelStrategyFactory,
            ILogger<EvenementReservationService> logger)
        {
            _context = context;
            _cancelStrategyFactory = cancelStrategyFactory;
            _logger = logger;
        }

        public async Task<EvenementReservationResponseDto?> GetByIdAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var reservation = await LoadReservationGraphAsync(idEvenementReservation, idSociete, cancellationToken);
            return reservation == null ? null : EvenementReservationMapper.ToResponseDto(reservation);
        }

        public async Task<IReadOnlyList<EvenementReservationListItemDto>> ListAsync(
            int idSociete,
            EvenementReservationListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var reservations = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(EvenementReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<EvenementReservationResponseDto?> GetByReferenceAsync(
            string reference,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new InvalidOperationException("Reference est obligatoire.");

            var normalized = reference.Trim();
            var reservation = await _context.EvenementReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdSociete == idSociete && r.ReferenceReservation == normalized,
                    cancellationToken);

            return reservation == null ? null : EvenementReservationMapper.ToResponseDto(reservation);
        }

        public Task<IReadOnlyList<EvenementReservationListItemDto>> ListBySessionAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementReservationListFilter { IdEvenementSession = idEvenementSession },
                cancellationToken);

        public async Task<IReadOnlyList<EvenementReservationListItemDto>?> ListBySocieteAndSessionAsync(
            int idSociete,
            int idEvenementSession,
            CancellationToken cancellationToken = default)
        {
            var sessionExists = await _context.EvenementSessions
                .AsNoTracking()
                .AnyAsync(
                    s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                    cancellationToken);

            if (!sessionExists)
                return null;

            return await ListBySessionAsync(idEvenementSession, idSociete, cancellationToken);
        }

        public Task<IReadOnlyList<EvenementReservationListItemDto>> ListByStatusAsync(
            EvenementReservationStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementReservationListFilter { Status = status },
                cancellationToken);

        public async Task<IReadOnlyList<EvenementReservationListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var reservations = await _context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.DateCreation.Date == day)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(EvenementReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<EvenementReservationListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var reservations = await _context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete && r.DateCreation >= start && r.DateCreation <= end)
                .OrderByDescending(r => r.DateCreation)
                .ToListAsync(cancellationToken);

            return reservations
                .Select(EvenementReservationMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<EvenementTicketResponseDto>?> GetTicketsByReservationAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _context.EvenementReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (reservation == null)
                return null;

            return reservation.Lines
                .SelectMany(l => l.Tickets)
                .OrderBy(t => t.IdEvenementTicket)
                .Select(EvenementReservationMapper.ToTicketResponse)
                .ToList();
        }

        public async Task<EvenementCancelReservationResponseDto> CancelAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var snapshot = await _context.EvenementReservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (snapshot == null)
            {
                throw new KeyNotFoundException(
                    $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");
            }

            if (snapshot.Status == EvenementReservationStatus.CANCELLED)
            {
                var cancelled = await LoadReservationGraphAsync(idEvenementReservation, idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");

                return BuildCancelResponse(cancelled, alreadyCancelled: true, ticketsVoided: 0);
            }

            if (snapshot.Status == EvenementReservationStatus.EXPIRED)
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
                    var reservation = await _context.EvenementReservations
                        .Include(r => r.Lines)
                            .ThenInclude(l => l.Tickets)
                        .Include(r => r.Payments)
                        .FirstOrDefaultAsync(
                            r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                            cancellationToken);

                    if (reservation == null)
                    {
                        throw new KeyNotFoundException(
                            $"Réservation événement {idEvenementReservation} introuvable pour la société {idSociete}.");
                    }

                    if (reservation.Status == EvenementReservationStatus.CANCELLED)
                    {
                        if (transaction != null)
                            await transaction.CommitAsync(cancellationToken);

                        return BuildCancelResponse(reservation, alreadyCancelled: true, ticketsVoided: 0);
                    }

                    EnsureCancellable(reservation);

                    var session = await _context.EvenementSessions
                        .FirstOrDefaultAsync(
                            s => s.IdEvenementSession == reservation.IdEvenementSession && s.IdSociete == idSociete,
                            cancellationToken);

                    if (session == null)
                    {
                        throw new InvalidOperationException(
                            "Session associée à la réservation introuvable.");
                    }

                    var fromConfirmed = reservation.Status == EvenementReservationStatus.CONFIRMED;
                    var cancelStrategy = _cancelStrategyFactory.GetStrategy(session.InventoryMode);
                    await cancelStrategy.ReleaseReservationAsync(
                        new EvenementInventoryCancelRequest
                        {
                            Reservation = reservation,
                            Session = session,
                            FromConfirmedSale = fromConfirmed
                        },
                        cancellationToken);

                    var ticketsVoided = VoidUnusedTickets(reservation);
                    MarkPaymentsRefunded(reservation);

                    reservation.Status = EvenementReservationStatus.CANCELLED;
                    reservation.ExpiresAtUtc = null;
                    reservation.DateModification = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Réservation événement annulée — Id={Id}, TicketsVoided={TicketsVoided}, FromConfirmed={FromConfirmed}",
                        reservation.IdEvenementReservation,
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

        private static void EnsureCancellable(Models.Evenement.EvenementReservation reservation)
        {
            if (reservation.Status is not (EvenementReservationStatus.HOLD or EvenementReservationStatus.CONFIRMED))
            {
                throw new InvalidOperationException(
                    $"Impossible d'annuler une réservation au statut {reservation.Status}.");
            }

            if (reservation.Status == EvenementReservationStatus.CONFIRMED)
            {
                var hasUsedTicket = reservation.Lines
                    .SelectMany(l => l.Tickets)
                    .Any(t => t.Status == EvenementTicketStatus.USED);

                if (hasUsedTicket)
                {
                    throw new InvalidOperationException(
                        "Impossible d'annuler : au moins un ticket a déjà été utilisé.");
                }
            }

            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");
        }

        private static int VoidUnusedTickets(Models.Evenement.EvenementReservation reservation)
        {
            var count = 0;
            foreach (var ticket in reservation.Lines.SelectMany(l => l.Tickets))
            {
                if (ticket.Status != EvenementTicketStatus.ISSUED)
                    continue;

                ticket.Status = EvenementTicketStatus.VOID;
                count++;
            }

            return count;
        }

        private static void MarkPaymentsRefunded(Models.Evenement.EvenementReservation reservation)
        {
            var utcNow = DateTime.UtcNow;
            foreach (var payment in reservation.Payments.Where(p => p.Status == EvenementPaymentStatus.SUCCEEDED))
            {
                payment.Status = EvenementPaymentStatus.REFUNDED;
                payment.DateModification = utcNow;
            }
        }

        private IQueryable<Models.Evenement.EvenementReservation> BuildListQuery(
            int idSociete,
            EvenementReservationListFilter? filter)
        {
            var query = _context.EvenementReservations
                .AsNoTracking()
                .Where(r => r.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (filter?.IdEvenementSession.HasValue == true)
                query = query.Where(r => r.IdEvenementSession == filter.IdEvenementSession.Value);

            if (!string.IsNullOrWhiteSpace(filter?.CustomerRef))
            {
                var customerRef = filter.CustomerRef.Trim();
                query = query.Where(r => r.CustomerRef == customerRef);
            }

            return query.OrderByDescending(r => r.DateCreation);
        }

        private async Task<Models.Evenement.EvenementReservation?> LoadReservationGraphAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken)
        {
            return await _context.EvenementReservations
                .AsNoTracking()
                .Include(r => r.Lines)
                    .ThenInclude(l => l.Tickets)
                .Include(r => r.Payments)
                .FirstOrDefaultAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);
        }

        private static EvenementCancelReservationResponseDto BuildCancelResponse(
            Models.Evenement.EvenementReservation reservation,
            bool alreadyCancelled,
            int ticketsVoided) =>
            new()
            {
                Reservation = EvenementReservationMapper.ToResponseDto(reservation),
                AlreadyCancelled = alreadyCancelled,
                TicketsVoided = ticketsVoided
            };
    }
}
