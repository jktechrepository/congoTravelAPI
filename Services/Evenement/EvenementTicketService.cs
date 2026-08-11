using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementTicketService : IEvenementTicketService
    {
        private const string MarkUsedSql = @"
UPDATE `EvenementTickets`
SET `Status` = 'USED',
    `UsedAtUtc` = UTC_TIMESTAMP(6)
WHERE `IdEvenementTicket` = {0}
  AND `Status` = 'ISSUED'";

        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<EvenementTicketService> _logger;

        public EvenementTicketService(
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<EvenementTicketService> logger)
        {
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        public async Task<EvenementTicketDetailResponseDto?> GetByIdAsync(
            int idEvenementTicket,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var ticket = await LoadTicketGraphByIdAsync(idEvenementTicket, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<EvenementTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = EvenementTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
                return null;

            var ticket = await LoadTicketGraphAsync(normalizedCode, asNoTracking: true, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<IReadOnlyList<EvenementTicketListItemDto>> ListAsync(
            int idSociete,
            EvenementTicketListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildTicketListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => EvenementTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public Task<IReadOnlyList<EvenementTicketListItemDto>> ListByReservationAsync(
            int idEvenementReservation,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementTicketListFilter { IdEvenementReservation = idEvenementReservation },
                cancellationToken);

        public async Task<IReadOnlyList<EvenementTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idEvenementReservation,
            CancellationToken cancellationToken = default)
        {
            var reservationExists = await _context.EvenementReservations
                .AsNoTracking()
                .AnyAsync(
                    r => r.IdEvenementReservation == idEvenementReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (!reservationExists)
                return null;

            return await ListByReservationAsync(idEvenementReservation, idSociete, cancellationToken);
        }

        public Task<IReadOnlyList<EvenementTicketListItemDto>> ListBySessionAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementTicketListFilter { IdEvenementSession = idEvenementSession },
                cancellationToken);

        public Task<IReadOnlyList<EvenementTicketListItemDto>> ListByStatusAsync(
            EvenementTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new EvenementTicketListFilter { Status = status },
                cancellationToken);

        public async Task<IReadOnlyList<EvenementTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var rows = await (
                from t in _context.EvenementTickets.AsNoTracking()
                join line in _context.EvenementReservationLines.AsNoTracking()
                    on t.IdEvenementReservationLine equals line.IdEvenementReservationLine
                join r in _context.EvenementReservations.AsNoTracking()
                    on line.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc.Date == day
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => EvenementTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<IReadOnlyList<EvenementTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var rows = await (
                from t in _context.EvenementTickets.AsNoTracking()
                join line in _context.EvenementReservationLines.AsNoTracking()
                    on t.IdEvenementReservationLine equals line.IdEvenementReservationLine
                join r in _context.EvenementReservations.AsNoTracking()
                    on line.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc >= start && t.IssuedAtUtc <= end
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => EvenementTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<EvenementTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = EvenementTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
                return BuildUnknownCheckResult();

            var ticket = await LoadTicketGraphAsync(normalizedCode, asNoTracking: true, cancellationToken);
            if (!BelongsToSociete(ticket, idSociete))
            {
                if (ticket != null)
                {
                    _logger.LogWarning(
                        "Check ticket refusé (tenancy) — Code={Code}, SocieteDemandee={Societe}",
                        normalizedCode,
                        idSociete);
                }

                return BuildUnknownCheckResult();
            }

            return await BuildCheckResultAsync(ticket, idSociete, cancellationToken);
        }

        public async Task<EvenementTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = EvenementTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
            {
                return new EvenementTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Ticket inconnu ou code invalide."
                };
            }

            var ticket = await LoadTicketGraphAsync(normalizedCode, asNoTracking: false, cancellationToken);
            if (!BelongsToSociete(ticket, idSociete))
            {
                if (ticket != null)
                {
                    _logger.LogWarning(
                        "Use ticket refusé (tenancy) — Code={Code}, SocieteDemandee={Societe}",
                        normalizedCode,
                        idSociete);
                }

                return new EvenementTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Ticket inconnu ou code invalide."
                };
            }

            if (ticket!.Status == EvenementTicketStatus.USED)
            {
                return new EvenementTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status200OK,
                    Response = EvenementTicketMapper.ToUseResponse(ticket, alreadyUsed: true)
                };
            }

            var reservation = ticket.ReservationLine!.Reservation!;
            var session = reservation.Session!;
            var heuresAvant = await ResolveEntreeHeuresAvantAsync(idSociete, cancellationToken);
            var eligibility = EvenementTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                session,
                DateTime.UtcNow,
                heuresAvant);

            if (!eligibility.EntreeAutorisee)
            {
                return new EvenementTicketUseResult
                {
                    HttpStatusCode = eligibility.SuggestedHttpStatus,
                    ErrorMessage = eligibility.Message
                };
            }

            var marked = await TryMarkTicketUsedAsync(ticket.IdEvenementTicket, cancellationToken);
            if (!marked)
            {
                var refreshed = await LoadTicketGraphAsync(normalizedCode, asNoTracking: false, cancellationToken);
                if (refreshed?.Status == EvenementTicketStatus.USED)
                {
                    return new EvenementTicketUseResult
                    {
                        HttpStatusCode = StatusCodes.Status200OK,
                        Response = EvenementTicketMapper.ToUseResponse(refreshed, alreadyUsed: true)
                    };
                }

                return new EvenementTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = "Impossible de valider l'entrée pour ce ticket."
                };
            }

            ticket.Status = EvenementTicketStatus.USED;
            ticket.UsedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "Ticket événement utilisé — Id={Id}, Code={Code}",
                ticket.IdEvenementTicket,
                ticket.TicketCode);

            return new EvenementTicketUseResult
            {
                HttpStatusCode = StatusCodes.Status200OK,
                Response = EvenementTicketMapper.ToUseResponse(ticket, alreadyUsed: false)
            };
        }

        private EvenementTicketCheckResult BuildUnknownCheckResult()
        {
            var unknown = EvenementTicketEligibilityHelper.Evaluate(null, null, null, DateTime.UtcNow);
            return new EvenementTicketCheckResult
            {
                Response = EvenementTicketMapper.ToCheckResponse(null, null, null, unknown),
                HttpStatusCode = unknown.SuggestedHttpStatus
            };
        }

        private async Task<EvenementTicketCheckResult> BuildCheckResultAsync(
            EvenementTicket? ticket,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var reservation = ticket?.ReservationLine?.Reservation;
            var session = reservation?.Session;
            var heuresAvant = await ResolveEntreeHeuresAvantAsync(idSociete, cancellationToken);
            var eligibility = EvenementTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                session,
                DateTime.UtcNow,
                heuresAvant);

            return new EvenementTicketCheckResult
            {
                Response = EvenementTicketMapper.ToCheckResponse(ticket, reservation, session, eligibility),
                HttpStatusCode = eligibility.SuggestedHttpStatus
            };
        }

        private async Task<int> ResolveEntreeHeuresAvantAsync(int idSociete, CancellationToken cancellationToken)
        {
            var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
            return config.HeuresOuvertureEntreeEvenementAvantDebut;
        }

        private static bool BelongsToSociete(EvenementTicket? ticket, int idSociete) =>
            ticket?.ReservationLine?.Reservation != null
            && ticket.ReservationLine.Reservation.IdSociete == idSociete;

        private static EvenementTicketDetailResponseDto? MapDetailIfBelongsToSociete(
            EvenementTicket? ticket,
            int idSociete)
        {
            if (!BelongsToSociete(ticket, idSociete))
                return null;

            var reservation = ticket!.ReservationLine!.Reservation!;
            var session = reservation.Session
                ?? throw new InvalidOperationException("Session associée au ticket introuvable.");

            return EvenementTicketMapper.ToDetailDto(ticket, reservation, session);
        }

        private IQueryable<TicketListRow> BuildTicketListQuery(int idSociete, EvenementTicketListFilter? filter)
        {
            var query =
                from t in _context.EvenementTickets.AsNoTracking()
                join line in _context.EvenementReservationLines.AsNoTracking()
                    on t.IdEvenementReservationLine equals line.IdEvenementReservationLine
                join r in _context.EvenementReservations.AsNoTracking()
                    on line.IdEvenementReservation equals r.IdEvenementReservation
                where r.IdSociete == idSociete
                select new TicketListRow { Ticket = t, Reservation = r };

            if (filter?.Status.HasValue == true)
                query = query.Where(row => row.Ticket.Status == filter.Status.Value);

            if (filter?.IdEvenementReservation.HasValue == true)
            {
                var idReservation = filter.IdEvenementReservation.Value;
                query = query.Where(row => row.Reservation.IdEvenementReservation == idReservation);
            }

            if (filter?.IdEvenementSession.HasValue == true)
            {
                var idSession = filter.IdEvenementSession.Value;
                query = query.Where(row => row.Reservation.IdEvenementSession == idSession);
            }

            return query.OrderByDescending(row => row.Ticket.IssuedAtUtc);
        }

        private async Task<EvenementTicket?> LoadTicketGraphByIdAsync(
            int idEvenementTicket,
            CancellationToken cancellationToken) =>
            await _context.EvenementTickets
                .AsNoTracking()
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Session)
                .FirstOrDefaultAsync(t => t.IdEvenementTicket == idEvenementTicket, cancellationToken);

        private async Task<EvenementTicket?> LoadTicketGraphAsync(
            string normalizedCode,
            bool asNoTracking,
            CancellationToken cancellationToken)
        {
            var query = _context.EvenementTickets
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Session)
                .Where(t => t.TicketCode == normalizedCode);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> TryMarkTicketUsedAsync(int idEvenementTicket, CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    MarkUsedSql,
                    new object[] { idEvenementTicket },
                    cancellationToken);
                return rows > 0;
            }

            var ticket = await _context.EvenementTickets
                .FirstOrDefaultAsync(t => t.IdEvenementTicket == idEvenementTicket, cancellationToken);

            if (ticket == null || ticket.Status != EvenementTicketStatus.ISSUED)
                return false;

            var utcNow = DateTime.UtcNow;
            ticket.Status = EvenementTicketStatus.USED;
            ticket.UsedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private sealed class TicketListRow
        {
            public EvenementTicket Ticket { get; init; } = null!;

            public EvenementReservation Reservation { get; init; } = null!;
        }
    }
}
