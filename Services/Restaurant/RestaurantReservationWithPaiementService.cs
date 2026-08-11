using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Restaurant
{
    public class RestaurantReservationWithPaiementService : IRestaurantReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IRestaurantHoldService _holdService;
        private readonly IRestaurantPaymentService _paymentService;
        private readonly IRestaurantFlexPayInitiationService _flexPayInitiationService;
        private readonly IRestaurantReservationService _reservationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<RestaurantReservationWithPaiementService> _logger;

        public RestaurantReservationWithPaiementService(
            CongoTravelDbContext context,
            IRestaurantHoldService holdService,
            IRestaurantPaymentService paymentService,
            IRestaurantFlexPayInitiationService flexPayInitiationService,
            IRestaurantReservationService reservationService,
            ICurrentUserService currentUserService,
            ILogger<RestaurantReservationWithPaiementService> logger)
        {
            _context = context;
            _holdService = holdService;
            _paymentService = paymentService;
            _flexPayInitiationService = flexPayInitiationService;
            _reservationService = reservationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<RestaurantReservationWithPaiementResponseDto> CreateCashAsync(
            RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements Mobile Money et carte bancaire doivent utiliser " +
                    "POST /api/restaurants/reservations/with-paiement-electronique. " +
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            if (!MethodePaiementHelper.IsCash(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(request.IdRestaurantCreneau, cancellationToken);
            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: false, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdRestaurantCreneau,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            await AttachBuyerUserIdAsync(hold.IdRestaurantReservation, cancellationToken);

            try
            {
                var confirmed = await _paymentService.ConfirmPaymentAsync(
                    hold.IdRestaurantReservation,
                    idSociete,
                    new RestaurantConfirmPaymentRequestDto
                    {
                        MethodePaiement = MethodePaiementHelper.Cash,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request),
                        ReferenceTransaction = request.Paiement.ReferenceTransaction
                    },
                    cancellationToken);

                return new RestaurantReservationWithPaiementResponseDto
                {
                    Reservation = confirmed.Reservation,
                    Payment = confirmed.Payment,
                    TransactionStatut = "Succes",
                    Message = confirmed.AlreadyConfirmed
                        ? "Réservation déjà confirmée (idempotent)."
                        : "Réservation confirmée et acompte encaissé.",
                    AlreadyConfirmed = confirmed.AlreadyConfirmed,
                    ReservationExpiresAtUtc = null
                };
            }
            catch (Exception ex)
            {
                await TryRollbackHoldAsync(hold.IdRestaurantReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        public async Task<RestaurantReservationWithPaiementResponseDto> InitiateElectronicAsync(
            RestaurantReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (!MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "L'endpoint électronique accepte uniquement MOBILE_MONEY ou CARTE_BANCAIRE. " +
                    "Utilisez POST /api/restaurants/reservations/with-paiement pour CASH.");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(request.IdRestaurantCreneau, cancellationToken);
            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: true, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdRestaurantCreneau,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            await AttachBuyerUserIdAsync(hold.IdRestaurantReservation, cancellationToken);

            try
            {
                var initiated = await _flexPayInitiationService.InitiateAsync(
                    hold.IdRestaurantReservation,
                    idSociete,
                    new RestaurantInitiateFlexPayRequestDto
                    {
                        MethodePaiement = request.Paiement.MethodePaiement,
                        Phone = request.Paiement.Phone,
                        IdSite = effectiveIdSite!.Value,
                        CodeDevisePaiement = request.Paiement.CodeDevisePaiement,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request)
                    },
                    cancellationToken);

                var reservation = await _reservationService.GetByIdAsync(
                    hold.IdRestaurantReservation,
                    idSociete,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation restaurant {hold.IdRestaurantReservation} introuvable après initiation FlexPay.");

                return new RestaurantReservationWithPaiementResponseDto
                {
                    Reservation = reservation,
                    Payment = initiated.Payment,
                    TransactionStatut = "EnAttente",
                    Message = string.IsNullOrWhiteSpace(initiated.Message)
                        ? "Paiement FlexPay initié. Hold conservé jusqu'à confirmation ou expiration."
                        : initiated.Message,
                    OrderNumber = initiated.OrderNumber,
                    PaymentUrl = initiated.PaymentUrl,
                    ReservationExpiresAtUtc = initiated.ReservationExpiresAtUtc,
                    MontantFlexPay = initiated.MontantFlexPay,
                    CodeDevisePaiement = initiated.CodeDevisePaiement,
                    MontantTarif = initiated.MontantTarif,
                    CodeDeviseTarif = initiated.CodeDeviseTarif,
                    TauxApplique = initiated.TauxApplique,
                    FlexPayAccepted = initiated.FlexPayAccepted,
                    AlreadyInitiated = initiated.AlreadyInitiated
                };
            }
            catch (Exception ex)
            {
                await TryRollbackHoldAsync(hold.IdRestaurantReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        private async Task<int> ResolvePurchaseSocieteIdAsync(
            int idRestaurantCreneau,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.IsStaff && !_currentUserService.IsSuperAdmin)
            {
                var jwtSocieteId = RestaurantTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var belongs = await _context.RestaurantCreneaux
                    .AsNoTracking()
                    .AnyAsync(
                        c => c.IdRestaurantCreneau == idRestaurantCreneau && c.IdSociete == jwtSocieteId,
                        cancellationToken);
                if (!belongs)
                {
                    throw new KeyNotFoundException(
                        $"Créneau restaurant {idRestaurantCreneau} introuvable pour la société {jwtSocieteId}.");
                }

                return jwtSocieteId;
            }

            var creneau = await _context.RestaurantCreneaux
                .AsNoTracking()
                .Where(c => c.IdRestaurantCreneau == idRestaurantCreneau
                            && c.Status == RestaurantStatus.Published)
                .Select(c => new { c.IdSociete })
                .FirstOrDefaultAsync(cancellationToken);

            if (creneau == null)
            {
                throw new KeyNotFoundException(
                    $"Créneau restaurant {idRestaurantCreneau} introuvable ou non publié.");
            }

            return creneau.IdSociete;
        }

        private async Task<int?> ResolveEffectiveIdSiteAsync(
            int idSociete,
            RestaurantReservationWithPaiementRequestDto request,
            bool requireSite,
            CancellationToken cancellationToken)
        {
            int? fromPaiement = request.Paiement.IdSite is > 0
                ? request.Paiement.IdSite
                : null;

            var row = await (
                from c in _context.RestaurantCreneaux.AsNoTracking()
                join r in _context.Restaurants.AsNoTracking()
                    on c.IdRestaurant equals r.IdRestaurant
                where c.IdRestaurantCreneau == request.IdRestaurantCreneau
                      && c.IdSociete == idSociete
                select new { r.IdSite }
            ).FirstOrDefaultAsync(cancellationToken);

            if (row == null)
            {
                throw new KeyNotFoundException(
                    $"Créneau restaurant {request.IdRestaurantCreneau} introuvable pour la société {idSociete}.");
            }

            var effective = fromPaiement ?? row.IdSite;

            if (requireSite && (!effective.HasValue || effective.Value <= 0))
            {
                throw new InvalidOperationException(
                    "IdSite est obligatoire pour le paiement électronique restaurant " +
                    "(fournir paiement.idSite ou définir idSite sur l'établissement).");
            }

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, effective, idSociete, cancellationToken);

            return effective;
        }

        private static void ValidateSharedRequest(RestaurantReservationWithPaiementRequestDto request)
        {
            if (request.IdRestaurantCreneau <= 0)
                throw new InvalidOperationException("IdRestaurantCreneau est obligatoire.");

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins un item de hold est requis.");

            if (request.Paiement == null || string.IsNullOrWhiteSpace(request.Paiement.MethodePaiement))
                throw new InvalidOperationException("Paiement.MethodePaiement est obligatoire.");
        }

        private async Task AttachBuyerUserIdAsync(
            int idRestaurantReservation,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
                return;

            var reservation = await _context.RestaurantReservations
                .FirstOrDefaultAsync(r => r.IdRestaurantReservation == idRestaurantReservation, cancellationToken);
            if (reservation == null || reservation.IdUtilisateur == userId)
                return;

            reservation.IdUtilisateur = userId;
            reservation.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static RestaurantHoldRequestDto ToHoldRequest(
            RestaurantReservationWithPaiementRequestDto request,
            int? effectiveIdSite) =>
            new()
            {
                CustomerRef = request.CustomerRef,
                IdempotencyKey = request.IdempotencyKey,
                IdSite = effectiveIdSite,
                Items = request.Items
            };

        private static string? ResolvePaymentIdempotencyKey(RestaurantReservationWithPaiementRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey))
                return request.Paiement.IdempotencyKey.Trim();

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return request.IdempotencyKey.Trim() + ":pay";

            return null;
        }

        private async Task TryRollbackHoldAsync(
            int idRestaurantReservation,
            int idSociete,
            Exception cause,
            CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.CancelAsync(idRestaurantReservation, idSociete, cancellationToken);
                _logger.LogWarning(
                    cause,
                    "Rollback hold restaurant après échec 2e étape — IdReservation={Id}",
                    idRestaurantReservation);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Échec rollback hold restaurant IdReservation={Id} (cause initiale loguée séparément)",
                    idRestaurantReservation);
            }
        }
    }
}
