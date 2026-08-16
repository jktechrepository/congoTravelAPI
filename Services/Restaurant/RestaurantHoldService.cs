using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CongoTravel.Data;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Restaurant.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantHoldService : IRestaurantHoldService
    {
        private const int MaxReferenceAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<RestaurantHoldService> _logger;

        public RestaurantHoldService(
            CongoTravelDbContext context,
            IRestaurantInventoryHoldStrategyFactory holdStrategyFactory,
            IConfigSocieteRepository configSocieteRepository,
            ILogger<RestaurantHoldService> logger,
            ICurrentUserService? currentUserService = null)
        {
            _context = context;
            _holdStrategyFactory = holdStrategyFactory;
            _configSocieteRepository = configSocieteRepository;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<RestaurantHoldResponseDto> CreateHoldAsync(
            int idRestaurantCreneau,
            int idSociete,
            RestaurantHoldRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var idempotencyKey = RestaurantIdempotencyHelper.NormalizeKey(request.IdempotencyKey);
            if (idempotencyKey != null)
            {
                var existing = await _context.RestaurantReservations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.IdSociete == idSociete && r.IdempotencyKey == idempotencyKey,
                        cancellationToken);

                if (existing != null)
                {
                    _logger.LogInformation(
                        "Hold restaurant idempotent — IdReservation={Id}, IdempotencyKey={Key}",
                        existing.IdRestaurantReservation,
                        idempotencyKey);
                    return RestaurantReservationMapper.ToHoldResponse(existing);
                }
            }

            await _configSocieteRepository.EnsureReservationsActivesAsync(idSociete, cancellationToken);

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var creneau = await _context.RestaurantCreneaux
                        .Include(c => c.GlobalQuota)
                        .Include(c => c.ZoneQuotas)
                        .Include(c => c.Restaurant)
                        .FirstOrDefaultAsync(
                            c => c.IdRestaurantCreneau == idRestaurantCreneau && c.IdSociete == idSociete,
                            cancellationToken);

                    if (creneau == null)
                    {
                        throw new KeyNotFoundException(
                            $"Créneau restaurant {idRestaurantCreneau} introuvable pour la société {idSociete}.");
                    }

                    if (creneau.Status != RestaurantStatus.Published)
                    {
                        throw new InvalidOperationException(
                            "Le créneau doit être publié pour créer un hold.");
                    }

                    if (creneau.Restaurant == null || creneau.Restaurant.Status != RestaurantStatus.Published)
                    {
                        throw new InvalidOperationException(
                            "L'établissement doit être publié pour créer un hold.");
                    }

                    var holdStrategy = _holdStrategyFactory.GetStrategy(creneau.InventoryMode);
                    var holdRequest = new RestaurantInventoryHoldRequest
                    {
                        Creneau = creneau,
                        Items = request.Items,
                        CodeDevise = creneau.CodeDevise
                    };

                    if (creneau.InventoryMode == RestaurantInventoryMode.GlobalQuota)
                    {
                        if (creneau.GlobalQuota == null)
                        {
                            throw new InvalidOperationException(
                                "Inventaire global manquant pour ce créneau.");
                        }

                        holdRequest.PrixUnitaire = RestaurantAcompteHelper.ComputeAcompteUnitaire(
                            creneau.MontantAcompte,
                            creneau.GlobalQuota.PrixUnitaire,
                            creneau.Restaurant.AcomptePourcentDefaut);
                    }
                    else if (creneau.InventoryMode == RestaurantInventoryMode.ClassQuota
                             && creneau.ZoneQuotas.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Inventaire zones manquant pour ce créneau ClassQuota.");
                    }

                    var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
                    var utcNow = DateTime.UtcNow;
                    var expiresAt = RestaurantHoldDurationHelper.ComputeExpiresAtUtc(utcNow, config);
                    holdRequest.HoldExpiresAtUtc = expiresAt;

                    var strategyResult = await holdStrategy.ReserveHoldAsync(holdRequest, cancellationToken);
                    var reference = await GenerateUniqueReferenceAsync(idSociete, cancellationToken);

                    var reservation = new RestaurantReservation
                    {
                        IdSociete = idSociete,
                        IdRestaurant = creneau.IdRestaurant,
                        IdRestaurantCreneau = creneau.IdRestaurantCreneau,
                        IdSite = request.IdSite ?? creneau.Restaurant.IdSite,
                        ReferenceReservation = reference,
                        CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef)
                            ? null
                            : request.CustomerRef.Trim(),
                        Status = RestaurantReservationStatus.HOLD,
                        ExpiresAtUtc = expiresAt,
                        MontantSousTotal = strategyResult.MontantSousTotal,
                        CodeDevise = strategyResult.Lines.First().CodeDevise,
                        NombreCouverts = strategyResult.NombreCouverts,
                        IdempotencyKey = idempotencyKey,
                        DateCreation = utcNow
                    };

                    await ApplyIdClientFromRequestAsync(reservation, request.IdClient, cancellationToken);
                    await ApplyBuyerFromCurrentUserAsync(reservation, cancellationToken);

                    foreach (var line in strategyResult.Lines)
                    {
                        reservation.Lines.Add(new RestaurantReservationLine
                        {
                            LineType = line.LineType,
                            Quantite = line.Quantite,
                            PrixUnitaire = line.PrixUnitaire,
                            MontantLigne = line.MontantLigne,
                            CodeDevise = line.CodeDevise,
                            IdRestaurantCreneauGlobalQuota = line.IdRestaurantCreneauGlobalQuota,
                            IdRestaurantCreneauZoneQuota = line.IdRestaurantCreneauZoneQuota
                        });
                    }

                    _context.RestaurantReservations.Add(reservation);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "Hold restaurant créé — IdReservation={Id}, Creneau={CreneauId}, Montant={Montant} {Devise}, Couverts={Couverts}",
                        reservation.IdRestaurantReservation,
                        idRestaurantCreneau,
                        reservation.MontantSousTotal,
                        reservation.CodeDevise,
                        reservation.NombreCouverts);

                    return RestaurantReservationMapper.ToHoldResponse(reservation);
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
                var candidate = RestaurantReferenceGenerator.GenerateReservationReferenceCandidate(idSociete);
                var exists = await _context.RestaurantReservations
                    .AsNoTracking()
                    .AnyAsync(
                        r => r.IdSociete == idSociete && r.ReferenceReservation == candidate,
                        cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer une référence de réservation restaurant unique.");
        }

        private async Task ApplyBuyerFromCurrentUserAsync(
            RestaurantReservation reservation,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService?.UserId ?? 0;
            if (userId <= 0)
                return;

            reservation.IdUtilisateur = userId;

            // Ne pas écraser un IdClient déjà fourni dans le body.
            if (reservation.IdClient is > 0)
                return;

            reservation.IdClient = await _context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == userId)
                .Select(u => u.IdClient)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task ApplyIdClientFromRequestAsync(
            RestaurantReservation reservation,
            int? idClientFromRequest,
            CancellationToken cancellationToken)
        {
            if (idClientFromRequest is not > 0)
                return;

            var exists = await _context.Clients
                .AsNoTracking()
                .AnyAsync(c => c.IdClient == idClientFromRequest.Value, cancellationToken);
            if (!exists)
            {
                throw new InvalidOperationException(
                    $"Client {idClientFromRequest.Value} introuvable.");
            }

            reservation.IdClient = idClientFromRequest.Value;
        }
    }
}
