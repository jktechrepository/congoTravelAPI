using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueHoldService : ISiteTouristiqueHoldService
    {
        private const int MaxReferenceAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ILogger<SiteTouristiqueHoldService> _logger;

        public SiteTouristiqueHoldService(
            CongoTravelDbContext context,
            ISiteTouristiqueInventoryHoldStrategyFactory holdStrategyFactory,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<SiteTouristiqueHoldService> logger)
        {
            _context = context;
            _holdStrategyFactory = holdStrategyFactory;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
        }

        public async Task<SiteTouristiqueHoldResponseDto> CreateHoldAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            SiteTouristiqueHoldRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var idempotencyKey = SiteTouristiqueIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existing = await _context.SiteTouristiqueReservations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.IdSociete == idSociete && r.IdempotencyKey == idempotencyKey,
                        cancellationToken);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Hold site touristique idempotent — IdReservation={Id}, IdempotencyKey={Key}",
                        existing.IdSiteTouristiqueReservation,
                        idempotencyKey);
                    return SiteTouristiqueReservationMapper.ToHoldResponse(existing);
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
                    var journee = await _context.SiteTouristiqueJournees
                        .Include(j => j.GlobalQuota)
                        .Include(j => j.ClassQuotas)
                        .Include(j => j.Lieu)
                        .FirstOrDefaultAsync(
                            j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                            cancellationToken);

                    if (journee == null)
                    {
                        throw new KeyNotFoundException(
                            $"Journée site touristique {idSiteTouristiqueJournee} introuvable pour la société {idSociete}.");
                    }

                    SiteTouristiqueJourneeSalesEligibilityHelper.EnsureCanSell(journee, DateTime.UtcNow);

                    var holdStrategy = _holdStrategyFactory.GetStrategy(journee.InventoryMode);
                    var holdRequest = new SiteTouristiqueInventoryHoldRequest
                    {
                        Journee = journee,
                        Items = request.Items
                    };

                    if (journee.InventoryMode == SiteTouristiqueInventoryMode.GlobalQuota)
                    {
                        if (journee.GlobalQuota == null)
                        {
                            throw new InvalidOperationException(
                                "Inventaire global manquant pour cette journée.");
                        }

                        holdRequest.PrixUnitaire = journee.GlobalQuota.PrixUnitaire;
                        holdRequest.CodeDevise = journee.CodeDevise;
                    }
                    else if (journee.InventoryMode == SiteTouristiqueInventoryMode.ClassQuota
                             && journee.ClassQuotas.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Inventaire par classe manquant pour cette journée.");
                    }

                    var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
                    var utcNow = DateTime.UtcNow;
                    var expiresAt = SiteTouristiqueHoldDurationHelper.ComputeExpiresAtUtc(utcNow, config);
                    holdRequest.HoldExpiresAtUtc = expiresAt;

                    var strategyResult = await holdStrategy.ReserveHoldAsync(holdRequest, cancellationToken);
                    var reference = await GenerateUniqueReferenceAsync(idSociete, cancellationToken);

                    var reservation = new SiteTouristiqueReservation
                    {
                        IdSociete = idSociete,
                        IdSiteTouristiqueJournee = journee.IdSiteTouristiqueJournee,
                        IdSite = request.IdSite ?? journee.Lieu?.IdSite,
                        ReferenceReservation = reference,
                        CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef)
                            ? null
                            : request.CustomerRef.Trim(),
                        Status = SiteTouristiqueReservationStatus.HOLD,
                        ExpiresAtUtc = expiresAt,
                        MontantSousTotal = strategyResult.MontantSousTotal,
                        CodeDevise = strategyResult.Lines.First().CodeDevise,
                        IdempotencyKey = idempotencyKey,
                        DateCreation = utcNow
                    };

                    foreach (var line in strategyResult.Lines)
                    {
                        reservation.Lines.Add(new SiteTouristiqueReservationLine
                        {
                            LineType = line.LineType,
                            Quantite = line.Quantite,
                            PrixUnitaire = line.PrixUnitaire,
                            CodeDevise = line.CodeDevise,
                            IdSiteTouristiqueClassQuota = line.IdSiteTouristiqueClassQuota
                        });
                    }

                    _context.SiteTouristiqueReservations.Add(reservation);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Hold site touristique créé — IdReservation={Id}, Journee={JourneeId}, Montant={Montant} {Devise}",
                        reservation.IdSiteTouristiqueReservation,
                        idSiteTouristiqueJournee,
                        reservation.MontantSousTotal,
                        reservation.CodeDevise);

                    return SiteTouristiqueReservationMapper.ToHoldResponse(reservation);
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

        private async Task<string> GenerateUniqueReferenceAsync(int idSociete, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = SiteTouristiqueReferenceGenerator.GenerateReservationReferenceCandidate(idSociete);
                var exists = await _context.SiteTouristiqueReservations
                    .AsNoTracking()
                    .AnyAsync(
                        r => r.IdSociete == idSociete && r.ReferenceReservation == candidate,
                        cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de réservation site touristique unique.");
        }
    }
}
