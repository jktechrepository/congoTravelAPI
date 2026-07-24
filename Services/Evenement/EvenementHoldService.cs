using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementHoldService : IEvenementHoldService
    {
        private const int MaxReferenceAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly IEvenementInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<EvenementHoldService> _logger;

        public EvenementHoldService(
            CongoTravelDbContext context,
            IEvenementInventoryHoldStrategyFactory holdStrategyFactory,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<EvenementHoldService> logger)
        {
            _context = context;
            _holdStrategyFactory = holdStrategyFactory;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        public async Task<EvenementHoldResponseDto> CreateHoldAsync(
            int idEvenementSession,
            int idSociete,
            EvenementHoldRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var idempotencyKey = EvenementIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existing = await _context.EvenementReservations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.IdSociete == idSociete && r.IdempotencyKey == idempotencyKey,
                        cancellationToken);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Hold événement idempotent — IdReservation={Id}, IdempotencyKey={Key}",
                        existing.IdEvenementReservation,
                        idempotencyKey);
                    return EvenementReservationMapper.ToHoldResponse(existing);
                }
            }

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var session = await _context.EvenementSessions
                        .Include(s => s.GlobalQuota)
                        .Include(s => s.ClassQuotas)
                        .Include(s => s.Seats)
                        .FirstOrDefaultAsync(
                            s => s.IdEvenementSession == idEvenementSession && s.IdSociete == idSociete,
                            cancellationToken);

                    if (session == null)
                    {
                        throw new KeyNotFoundException(
                            $"Session événement {idEvenementSession} introuvable pour la société {idSociete}.");
                    }

                    var holdStrategy = _holdStrategyFactory.GetStrategy(session.InventoryMode);
                    var holdRequest = new EvenementInventoryHoldRequest
                    {
                        Session = session,
                        Items = request.Items
                    };

                    if (session.InventoryMode == EvenementInventoryMode.GlobalQuota)
                    {
                        if (session.GlobalQuota == null)
                        {
                            throw new InvalidOperationException(
                                "Inventaire global manquant pour cette session.");
                        }

                        holdRequest.PrixUnitaire = session.GlobalQuota.PrixUnitaire;
                        holdRequest.CodeDevise = session.GlobalQuota.CodeDevise;
                    }
                    else if (session.InventoryMode == EvenementInventoryMode.ClassQuota
                             && session.ClassQuotas.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Inventaire par classe manquant pour cette session.");
                    }
                    else if (session.InventoryMode == EvenementInventoryMode.SeatNumbered
                             && session.Seats.Count == 0)
                    {
                        var seatCount = await _context.EvenementSessionSeats
                            .CountAsync(s => s.IdEvenementSession == session.IdEvenementSession, cancellationToken);
                        if (seatCount == 0)
                        {
                            throw new InvalidOperationException(
                                "Plan de salle manquant pour cette session.");
                        }
                    }

                    var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
                    var utcNow = DateTime.UtcNow;
                    var expiresAt = EvenementHoldDurationHelper.ComputeExpiresAtUtc(utcNow, config);
                    holdRequest.HoldExpiresAtUtc = expiresAt;

                    var strategyResult = await holdStrategy.ReserveHoldAsync(holdRequest, cancellationToken);

                    var reference = await GenerateUniqueReferenceAsync(idSociete, cancellationToken);

                    var reservation = new EvenementReservation
                    {
                        IdSociete = idSociete,
                        IdEvenementSession = session.IdEvenementSession,
                        IdSite = request.IdSite ?? session.IdSite,
                        ReferenceReservation = reference,
                        CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef)
                            ? null
                            : request.CustomerRef.Trim(),
                        Status = EvenementReservationStatus.HOLD,
                        ExpiresAtUtc = expiresAt,
                        MontantSousTotal = strategyResult.MontantSousTotal,
                        CodeDevise = strategyResult.Lines.First().CodeDevise,
                        IdempotencyKey = idempotencyKey,
                        DateCreation = utcNow
                    };

                    foreach (var line in strategyResult.Lines)
                    {
                        reservation.Lines.Add(new EvenementReservationLine
                        {
                            LineType = line.LineType,
                            Quantite = line.Quantite,
                            PrixUnitaire = line.PrixUnitaire,
                            CodeDevise = line.CodeDevise,
                            IdEvenementSessionClassQuota = line.IdEvenementSessionClassQuota,
                            IdEvenementSessionSeat = line.IdEvenementSessionSeat
                        });
                    }

                    _context.EvenementReservations.Add(reservation);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (session.InventoryMode == EvenementInventoryMode.SeatNumbered)
                    {
                        await LinkSeatsToReservationAsync(
                            reservation.IdEvenementReservation,
                            strategyResult.Lines,
                            cancellationToken);
                    }

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Hold événement créé — IdReservation={Id}, Session={SessionId}, Montant={Montant} {Devise}",
                        reservation.IdEvenementReservation,
                        idEvenementSession,
                        reservation.MontantSousTotal,
                        reservation.CodeDevise);

                    return EvenementReservationMapper.ToHoldResponse(reservation);
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

        private async Task LinkSeatsToReservationAsync(
            int idEvenementReservation,
            IReadOnlyList<EvenementHoldLineResult> lines,
            CancellationToken cancellationToken)
        {
            foreach (var line in lines)
            {
                if (!line.IdEvenementSessionSeat.HasValue)
                    continue;

                var seat = await _context.EvenementSessionSeats
                    .FirstOrDefaultAsync(
                        s => s.IdEvenementSessionSeat == line.IdEvenementSessionSeat.Value,
                        cancellationToken);

                if (seat == null)
                {
                    throw new InvalidOperationException(
                        $"Siège {line.IdEvenementSessionSeat.Value} introuvable après hold.");
                }

                seat.IdEvenementReservationCourante = idEvenementReservation;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> GenerateUniqueReferenceAsync(int idSociete, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = EvenementReferenceGenerator.GenerateReservationReferenceCandidate(idSociete);
                var exists = await _context.EvenementReservations
                    .AsNoTracking()
                    .AnyAsync(
                        r => r.IdSociete == idSociete && r.ReferenceReservation == candidate,
                        cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de réservation événement unique.");
        }
    }
}
