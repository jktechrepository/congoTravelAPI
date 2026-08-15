using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantTicketService : IRestaurantTicketService
    {
        private const string MarkUsedSql = @"
UPDATE `RestaurantTickets`
SET `Status` = 'USED',
    `UsedAtUtc` = UTC_TIMESTAMP(6)
WHERE `IdRestaurantTicket` = {0}
  AND `Status` = 'ISSUED'";

        private readonly CongoTravelDbContext _context;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<RestaurantTicketService> _logger;

        public RestaurantTicketService(
            CongoTravelDbContext context,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<RestaurantTicketService> logger)
        {
            _context = context;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        public async Task<RestaurantTicketDetailResponseDto?> GetByIdAsync(
            int idRestaurantTicket,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var ticket = await LoadTicketGraphByIdAsync(idRestaurantTicket, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<RestaurantTicketDetailResponseDto?> GetByTicketCodeAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = RestaurantTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
                return null;

            var ticket = await LoadTicketGraphAsync(normalizedCode, asNoTracking: true, cancellationToken);
            return MapDetailIfBelongsToSociete(ticket, idSociete);
        }

        public async Task<IReadOnlyList<RestaurantTicketListItemDto>> ListAsync(
            int idSociete,
            RestaurantTicketListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var rows = await BuildTicketListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => RestaurantTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByReservationAsync(
            int idRestaurantReservation,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new RestaurantTicketListFilter { IdRestaurantReservation = idRestaurantReservation },
                cancellationToken);

        public async Task<IReadOnlyList<RestaurantTicketListItemDto>?> ListBySocieteAndReservationAsync(
            int idSociete,
            int idRestaurantReservation,
            CancellationToken cancellationToken = default)
        {
            var reservationExists = await _context.RestaurantReservations
                .AsNoTracking()
                .AnyAsync(
                    r => r.IdRestaurantReservation == idRestaurantReservation && r.IdSociete == idSociete,
                    cancellationToken);

            if (!reservationExists)
                return null;

            return await ListByReservationAsync(idRestaurantReservation, idSociete, cancellationToken);
        }

        public Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByCreneauAsync(
            int idRestaurantCreneau,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new RestaurantTicketListFilter { IdRestaurantCreneau = idRestaurantCreneau },
                cancellationToken);

        public Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByStatusAsync(
            RestaurantTicketStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new RestaurantTicketListFilter { Status = status },
                cancellationToken);

        public async Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var day = date.Date;
            var rows = await (
                from t in _context.RestaurantTickets.AsNoTracking()
                join line in _context.RestaurantReservationLines.AsNoTracking()
                    on t.IdRestaurantReservationLine equals line.IdRestaurantReservationLine
                join r in _context.RestaurantReservations.AsNoTracking()
                    on line.IdRestaurantReservation equals r.IdRestaurantReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc.Date == day
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => RestaurantTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<IReadOnlyList<RestaurantTicketListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var start = dateDebut.Date;
            var end = dateFin.Date.AddDays(1).AddTicks(-1);

            var rows = await (
                from t in _context.RestaurantTickets.AsNoTracking()
                join line in _context.RestaurantReservationLines.AsNoTracking()
                    on t.IdRestaurantReservationLine equals line.IdRestaurantReservationLine
                join r in _context.RestaurantReservations.AsNoTracking()
                    on line.IdRestaurantReservation equals r.IdRestaurantReservation
                where r.IdSociete == idSociete && t.IssuedAtUtc >= start && t.IssuedAtUtc <= end
                orderby t.IssuedAtUtc descending
                select new TicketListRow { Ticket = t, Reservation = r })
                .ToListAsync(cancellationToken);

            return rows
                .Select(row => RestaurantTicketMapper.ToListItemDto(row.Ticket, row.Reservation))
                .ToList();
        }

        public async Task<RestaurantTicketCheckResult> CheckTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = RestaurantTicketCodeGenerator.NormalizeTicketCode(ticketCode);
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

        public async Task<RestaurantTicketUseResult> UseTicketAsync(
            string ticketCode,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var normalizedCode = RestaurantTicketCodeGenerator.NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalizedCode))
            {
                return new RestaurantTicketUseResult
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

                return new RestaurantTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status404NotFound,
                    ErrorMessage = "Ticket inconnu ou code invalide."
                };
            }

            if (ticket!.Status == RestaurantTicketStatus.USED)
            {
                return new RestaurantTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status200OK,
                    Response = RestaurantTicketMapper.ToUseResponse(ticket, alreadyUsed: true)
                };
            }

            var reservation = ticket.ReservationLine!.Reservation!;
            var creneau = reservation.Creneau!;
            var heuresAvant = await ResolveEntreeHeuresAvantAsync(idSociete, cancellationToken);
            var eligibility = RestaurantTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                creneau,
                DateTime.UtcNow,
                heuresAvant);

            if (!eligibility.EntreeAutorisee)
            {
                return new RestaurantTicketUseResult
                {
                    HttpStatusCode = eligibility.SuggestedHttpStatus,
                    ErrorMessage = eligibility.Message
                };
            }

            var marked = await TryMarkTicketUsedAsync(ticket.IdRestaurantTicket, cancellationToken);
            if (!marked)
            {
                var refreshed = await LoadTicketGraphAsync(normalizedCode, asNoTracking: false, cancellationToken);
                if (refreshed?.Status == RestaurantTicketStatus.USED)
                {
                    return new RestaurantTicketUseResult
                    {
                        HttpStatusCode = StatusCodes.Status200OK,
                        Response = RestaurantTicketMapper.ToUseResponse(refreshed, alreadyUsed: true)
                    };
                }

                return new RestaurantTicketUseResult
                {
                    HttpStatusCode = StatusCodes.Status409Conflict,
                    ErrorMessage = "Impossible de valider l'entrée pour ce ticket."
                };
            }

            ticket.Status = RestaurantTicketStatus.USED;
            ticket.UsedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "Ticket restaurant utilisé — Id={Id}, Code={Code}",
                ticket.IdRestaurantTicket,
                ticket.TicketCode);

            return new RestaurantTicketUseResult
            {
                HttpStatusCode = StatusCodes.Status200OK,
                Response = RestaurantTicketMapper.ToUseResponse(ticket, alreadyUsed: false)
            };
        }

        private RestaurantTicketCheckResult BuildUnknownCheckResult()
        {
            var unknown = RestaurantTicketEligibilityHelper.Evaluate(null, null, null, DateTime.UtcNow);
            return new RestaurantTicketCheckResult
            {
                Response = RestaurantTicketMapper.ToCheckResponse(null, null, null, unknown),
                HttpStatusCode = unknown.SuggestedHttpStatus
            };
        }

        private async Task<RestaurantTicketCheckResult> BuildCheckResultAsync(
            RestaurantTicket? ticket,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var reservation = ticket?.ReservationLine?.Reservation;
            var creneau = reservation?.Creneau;
            var heuresAvant = await ResolveEntreeHeuresAvantAsync(idSociete, cancellationToken);
            var eligibility = RestaurantTicketEligibilityHelper.Evaluate(
                ticket,
                reservation,
                creneau,
                DateTime.UtcNow,
                heuresAvant);

            return new RestaurantTicketCheckResult
            {
                Response = RestaurantTicketMapper.ToCheckResponse(ticket, reservation, creneau, eligibility),
                HttpStatusCode = eligibility.SuggestedHttpStatus
            };
        }

        private async Task<int> ResolveEntreeHeuresAvantAsync(int idSociete, CancellationToken cancellationToken)
        {
            var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
            return config.HeuresOuvertureEntreeRestaurantAvantDebut;
        }

        private static bool BelongsToSociete(RestaurantTicket? ticket, int idSociete) =>
            ticket?.ReservationLine?.Reservation != null
            && ticket.ReservationLine.Reservation.IdSociete == idSociete;

        private static RestaurantTicketDetailResponseDto? MapDetailIfBelongsToSociete(
            RestaurantTicket? ticket,
            int idSociete)
        {
            if (!BelongsToSociete(ticket, idSociete))
                return null;

            var reservation = ticket!.ReservationLine!.Reservation!;
            var creneau = reservation.Creneau
                ?? throw new InvalidOperationException("Créneau associé au ticket introuvable.");

            return RestaurantTicketMapper.ToDetailDto(ticket, reservation, creneau);
        }

        private IQueryable<TicketListRow> BuildTicketListQuery(int idSociete, RestaurantTicketListFilter? filter)
        {
            var query =
                from t in _context.RestaurantTickets.AsNoTracking()
                join line in _context.RestaurantReservationLines.AsNoTracking()
                    on t.IdRestaurantReservationLine equals line.IdRestaurantReservationLine
                join r in _context.RestaurantReservations.AsNoTracking()
                    on line.IdRestaurantReservation equals r.IdRestaurantReservation
                where r.IdSociete == idSociete
                select new TicketListRow { Ticket = t, Reservation = r };

            if (filter?.Status.HasValue == true)
                query = query.Where(row => row.Ticket.Status == filter.Status.Value);

            if (filter?.IdRestaurantReservation.HasValue == true)
            {
                var idReservation = filter.IdRestaurantReservation.Value;
                query = query.Where(row => row.Reservation.IdRestaurantReservation == idReservation);
            }

            if (filter?.IdRestaurantCreneau.HasValue == true)
            {
                var idSession = filter.IdRestaurantCreneau.Value;
                query = query.Where(row => row.Reservation.IdRestaurantCreneau == idSession);
            }

            return query.OrderByDescending(row => row.Ticket.IssuedAtUtc);
        }

        private async Task<RestaurantTicket?> LoadTicketGraphByIdAsync(
            int idRestaurantTicket,
            CancellationToken cancellationToken) =>
            await _context.RestaurantTickets
                .AsNoTracking()
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Creneau)
                .FirstOrDefaultAsync(t => t.IdRestaurantTicket == idRestaurantTicket, cancellationToken);

        private async Task<RestaurantTicket?> LoadTicketGraphAsync(
            string normalizedCode,
            bool asNoTracking,
            CancellationToken cancellationToken)
        {
            var query = _context.RestaurantTickets
                .Include(t => t.ReservationLine!)
                    .ThenInclude(l => l.Reservation!)
                        .ThenInclude(r => r.Creneau)
                .Where(t => t.TicketCode == normalizedCode);

            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<bool> TryMarkTicketUsedAsync(int idRestaurantTicket, CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    MarkUsedSql,
                    new object[] { idRestaurantTicket },
                    cancellationToken);
                return rows > 0;
            }

            var ticket = await _context.RestaurantTickets
                .FirstOrDefaultAsync(t => t.IdRestaurantTicket == idRestaurantTicket, cancellationToken);

            if (ticket == null || ticket.Status != RestaurantTicketStatus.ISSUED)
                return false;

            var utcNow = DateTime.UtcNow;
            ticket.Status = RestaurantTicketStatus.USED;
            ticket.UsedAtUtc = utcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private sealed class TicketListRow
        {
            public RestaurantTicket Ticket { get; init; } = null!;

            public RestaurantReservation Reservation { get; init; } = null!;
        }
    }
}
