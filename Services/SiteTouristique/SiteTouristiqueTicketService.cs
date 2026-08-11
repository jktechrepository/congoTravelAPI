using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueTicketService : ISiteTouristiqueTicketService
    {
        private const string MarkUsedSql = @"
UPDATE `SiteTouristiqueTickets`
SET `Status` = 'USED',
    `UsedAtUtc` = UTC_TIMESTAMP(6)
WHERE `IdSiteTouristiqueTicket` = {0}
  AND `Status` = 'ISSUED'";

        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueTicketService> _logger;

        public SiteTouristiqueTicketService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueTicketService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueTicketDetailResponseDto?> GetByIdAsync(
            int idSiteTouristiqueTicket,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var ticket = await LoadTicketGraphByIdAsync(idSiteTouristiqueTicket, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<SiteTouristiqueTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = SiteTouristiqueTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
                return null;

            var ticket = await LoadTicketGraphAsync(normalizedCode, asNoTracking: true, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueTicketListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildTicketListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => SiteTouristiqueTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByReservationAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueTicketListFilter { IdSiteTouristiqueReservation = idSiteTouristiqueReservation },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idSiteTouristiqueReservation,
            CancellationToken cancellationToken = default)
        {
            var reservationExists = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .AnyAsync(
                    r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (!reservationExists)
                return null;

            return await ListByReservationAsync(idSiteTouristiqueReservation, idSociete, cancellationToken);
        }

        public Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListBySessionAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueTicketListFilter { IdSiteTouristiqueJournee = idSiteTouristiqueJournee },
                cancellationToken);

        public Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByStatusAsync(
            SiteTouristiqueTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueTicketListFilter { Status = status },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var rows = await (
                from t in _context.SiteTouristiqueTickets.AsNoTracking()
                join line in _context.SiteTouristiqueReservationLines.AsNoTracking()
                    on t.IdSiteTouristiqueReservationLine equals line.IdSiteTouristiqueReservationLine
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on line.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc.Date == day
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => SiteTouristiqueTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var rows = await (
                from t in _context.SiteTouristiqueTickets.AsNoTracking()
                join line in _context.SiteTouristiqueReservationLines.AsNoTracking()
                    on t.IdSiteTouristiqueReservationLine equals line.IdSiteTouristiqueReservationLine
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on line.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc >= start && t.IssuedAtUtc <= end
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => SiteTouristiqueTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<SiteTouristiqueTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = SiteTouristiqueTicketCodeGenerator.NormalizeTicketCode(ticketCode);
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

            return BuildCheckResult(ticket);
        }

        public async Task<SiteTouristiqueTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = SiteTouristiqueTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
            {
                return new SiteTouristiqueTicketUseResult
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

                return new SiteTouristiqueTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Ticket inconnu ou code invalide."
                };
            }

            if (ticket!.Status == SiteTouristiqueTicketStatus.USED)
            {
                return new SiteTouristiqueTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status200OK,
                    Response = SiteTouristiqueTicketMapper.ToUseResponse(ticket, alreadyUsed: true)
                };
            }

            var reservation = ticket.ReservationLine!.Reservation!;
            var journee = reservation.Journee!;
            var eligibility = SiteTouristiqueTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                journee,
                DateTime.UtcNow);

            if (!eligibility.EntreeAutorisee)
            {
                return new SiteTouristiqueTicketUseResult
                {
                    HttpStatusCode = eligibility.SuggestedHttpStatus,
                    ErrorMessage = eligibility.Message
                };
            }

            var marked = await TryMarkTicketUsedAsync(ticket.IdSiteTouristiqueTicket, cancellationToken);
            if (!marked)
            {
                var refreshed = await LoadTicketGraphAsync(normalizedCode, asNoTracking: false, cancellationToken);
                if (refreshed?.Status == SiteTouristiqueTicketStatus.USED)
                {
                    return new SiteTouristiqueTicketUseResult
                    {
                        HttpStatusCode = StatusCodes.Status200OK,
                        Response = SiteTouristiqueTicketMapper.ToUseResponse(refreshed, alreadyUsed: true)
                    };
                }

                return new SiteTouristiqueTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = "Impossible de valider l'entrée pour ce ticket."
                };
            }

            ticket.Status = SiteTouristiqueTicketStatus.USED;
            ticket.UsedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "Ticket site touristique utilisé — Id={Id}, Code={Code}",
                ticket.IdSiteTouristiqueTicket,
                ticket.TicketCode);

            return new SiteTouristiqueTicketUseResult
            {
                HttpStatusCode = StatusCodes.Status200OK,
                Response = SiteTouristiqueTicketMapper.ToUseResponse(ticket, alreadyUsed: false)
            };
        }

        private SiteTouristiqueTicketCheckResult BuildUnknownCheckResult()
        {
            var unknown = SiteTouristiqueTicketEligibilityHelper.Evaluate(null, null, null, DateTime.UtcNow);
            return new SiteTouristiqueTicketCheckResult
            {
                Response = SiteTouristiqueTicketMapper.ToCheckResponse(null, null, null, unknown),
                HttpStatusCode = unknown.SuggestedHttpStatus
            };
        }

        private SiteTouristiqueTicketCheckResult BuildCheckResult(
            SiteTouristiqueTicket? ticket)
        {
            var reservation = ticket?.ReservationLine?.Reservation;
            var journee = reservation?.Journee;
            var eligibility = SiteTouristiqueTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                journee,
                DateTime.UtcNow);

            return new SiteTouristiqueTicketCheckResult
            {
                Response = SiteTouristiqueTicketMapper.ToCheckResponse(ticket, reservation, journee, eligibility),
                HttpStatusCode = eligibility.SuggestedHttpStatus
            };
        }

        private static bool BelongsToSociete(SiteTouristiqueTicket? ticket, int idSociete) =>
            ticket?.ReservationLine?.Reservation != null
            && ticket.ReservationLine.Reservation.IdSociete == idSociete;

        private static SiteTouristiqueTicketDetailResponseDto? MapDetailIfBelongsToSociete(
            SiteTouristiqueTicket? ticket,
            int idSociete)
        {
            if (!BelongsToSociete(ticket, idSociete))
                return null;

            var reservation = ticket!.ReservationLine!.Reservation!;
            var journee = reservation.Journee
                ?? throw new InvalidOperationException("Session associée au ticket introuvable.");

            return SiteTouristiqueTicketMapper.ToDetailDto(ticket, reservation, journee);
        }

        private IQueryable<TicketListRow> BuildTicketListQuery(int idSociete, SiteTouristiqueTicketListFilter? filter)
        {
            var query =
                from t in _context.SiteTouristiqueTickets.AsNoTracking()
                join line in _context.SiteTouristiqueReservationLines.AsNoTracking()
                    on t.IdSiteTouristiqueReservationLine equals line.IdSiteTouristiqueReservationLine
                join r in _context.SiteTouristiqueReservations.AsNoTracking()
                    on line.IdSiteTouristiqueReservation equals r.IdSiteTouristiqueReservation
                where r.IdSociete == idSociete
                select new TicketListRow { Ticket = t, Reservation = r };

            if (filter?.Status.HasValue == true)
                query = query.Where(row => row.Ticket.Status == filter.Status.Value);

            if (filter?.IdSiteTouristiqueReservation.HasValue == true)
            {
                var idReservation = filter.IdSiteTouristiqueReservation.Value;
                query = query.Where(row => row.Reservation.IdSiteTouristiqueReservation == idReservation);
            }

            if (filter?.IdSiteTouristiqueJournee.HasValue == true)
            {
                var idJournee = filter.IdSiteTouristiqueJournee.Value;
                query = query.Where(row => row.Reservation.IdSiteTouristiqueJournee == idJournee);
            }

            return query.OrderByDescending(row => row.Ticket.IssuedAtUtc);
        }

        private async Task<SiteTouristiqueTicket?> LoadTicketGraphByIdAsync(
            int idSiteTouristiqueTicket,
            CancellationToken cancellationToken) =>
            await _context.SiteTouristiqueTickets
                .AsNoTracking()
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Journee!)
                            .ThenInclude(j => j.Lieu)
                .FirstOrDefaultAsync(t => t.IdSiteTouristiqueTicket == idSiteTouristiqueTicket, cancellationToken);

        private async Task<SiteTouristiqueTicket?> LoadTicketGraphAsync(
            string normalizedCode,
            bool asNoTracking,
            CancellationToken cancellationToken)
        {
            var query = _context.SiteTouristiqueTickets
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Journee!)
                            .ThenInclude(j => j.Lieu)
                .Where(t => t.TicketCode == normalizedCode);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> TryMarkTicketUsedAsync(int idSiteTouristiqueTicket, CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    MarkUsedSql,
                    new object[] { idSiteTouristiqueTicket },
                    cancellationToken);
                return rows > 0;
            }

            var ticket = await _context.SiteTouristiqueTickets
                .FirstOrDefaultAsync(t => t.IdSiteTouristiqueTicket == idSiteTouristiqueTicket, cancellationToken);

            if (ticket == null || ticket.Status != SiteTouristiqueTicketStatus.ISSUED)
                return false;

            var utcNow = DateTime.UtcNow;
            ticket.Status = SiteTouristiqueTicketStatus.USED;
            ticket.UsedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private sealed class TicketListRow
        {
            public SiteTouristiqueTicket Ticket { get; init; } = null!;

            public SiteTouristiqueReservation Reservation { get; init; } = null!;
        }
    }
}
