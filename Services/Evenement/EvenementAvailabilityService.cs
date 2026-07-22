using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public class EvenementAvailabilityService : IEvenementAvailabilityService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<EvenementAvailabilityService> _logger;

        public EvenementAvailabilityService(
            CongoTravelDbContext context,
            ILogger<EvenementAvailabilityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EvenementAvailabilityResponseDto?> GetSessionAvailabilityAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var session = await _context.EvenementSessions
                .AsNoTracking()
                .Include(s => s.GlobalQuota)
                .Include(s => s.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Section)
                .Include(s => s.Seats)
                    .ThenInclude(seat => seat.Classe)
                .FirstOrDefaultAsync(
                    s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                    cancellationToken);

            if (session == null)
                return null;

            var response = new EvenementAvailabilityResponseDto
            {
                IdEvenementSession = session.IdEvenementSession,
                InventoryMode = session.InventoryMode.ToString(),
                Status = session.Status.ToString()
            };

            switch (session.InventoryMode)
            {
                case EvenementInventoryMode.GlobalQuota:
                    if (session.GlobalQuota == null)
                    {
                        throw new InvalidOperationException(
                            "Inventaire global manquant pour cette session.");
                    }

                    response.GlobalQuota =
                        EvenementSessionMapper.ToGlobalQuotaAvailability(session.GlobalQuota);
                    break;

                case EvenementInventoryMode.ClassQuota:
                    if (session.ClassQuotas.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Inventaire par classe manquant pour cette session.");
                    }

                    response.ClassQuotas = session.ClassQuotas
                        .OrderBy(q => q.IdEvenementSessionClassQuota)
                        .Select(EvenementSessionMapper.ToClassQuotaAvailability)
                        .ToList();
                    break;

                case EvenementInventoryMode.SeatNumbered:
                    if (session.Seats.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Plan de salle manquant pour cette session.");
                    }

                    response.Seats = session.Seats
                        .OrderBy(s => s.SeatCode)
                        .Select(EvenementSessionMapper.ToSeatAvailability)
                        .ToList();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(session.InventoryMode),
                        session.InventoryMode,
                        "Mode d'inventaire inconnu.");
            }

            _logger.LogDebug(
                "Availability session événement — Id={Id}, Mode={Mode}, DisponibleGlobal={DisponibleGlobal}, Classes={ClassCount}, Seats={SeatCount}",
                session.IdEvenementSession,
                session.InventoryMode,
                response.GlobalQuota?.QuantiteDisponible,
                response.ClassQuotas?.Count,
                response.Seats?.Count);

            return response;
        }
    }
}
