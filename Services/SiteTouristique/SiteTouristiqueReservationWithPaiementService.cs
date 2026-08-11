using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueReservationWithPaiementService : ISiteTouristiqueReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueHoldService _holdService;
        private readonly ISiteTouristiquePaymentService _paymentService;
        private readonly ISiteTouristiqueFlexPayInitiationService _flexPayInitiationService;
        private readonly ISiteTouristiqueReservationService _reservationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SiteTouristiqueReservationWithPaiementService> _logger;

        public SiteTouristiqueReservationWithPaiementService(
            CongoTravelDbContext context,
            ISiteTouristiqueHoldService holdService,
            ISiteTouristiquePaymentService paymentService,
            ISiteTouristiqueFlexPayInitiationService flexPayInitiationService,
            ISiteTouristiqueReservationService reservationService,
            ICurrentUserService currentUserService,
            ILogger<SiteTouristiqueReservationWithPaiementService> logger)
        {
            _context = context;
            _holdService = holdService;
            _paymentService = paymentService;
            _flexPayInitiationService = flexPayInitiationService;
            _reservationService = reservationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<SiteTouristiqueReservationWithPaiementResponseDto> CreateCashAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements Mobile Money et carte bancaire doivent utiliser " +
                    "POST /api/sites-touristiques/reservations/with-paiement-electronique. " +
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            if (!MethodePaiementHelper.IsCash(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(
                request.IdSiteTouristiqueJournee, cancellationToken);

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: false, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdSiteTouristiqueJournee,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            await AttachBuyerUserIdAsync(hold.IdSiteTouristiqueReservation, cancellationToken);

            try
            {
                var confirmed = await _paymentService.ConfirmPaymentAsync(
                    hold.IdSiteTouristiqueReservation,
                    idSociete,
                    new SiteTouristiqueConfirmPaymentRequestDto
                    {
                        MethodePaiement = MethodePaiementHelper.Cash,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request),
                        ReferenceTransaction = request.Paiement.ReferenceTransaction
                    },
                    cancellationToken);

                return new SiteTouristiqueReservationWithPaiementResponseDto
                {
                    Reservation = confirmed.Reservation,
                    Payment = confirmed.Payment,
                    Tickets = confirmed.Reservation.Tickets ?? new List<SiteTouristiqueTicketResponseDto>(),
                    TransactionStatut = "Succes",
                    Message = confirmed.AlreadyConfirmed
                        ? "Réservation déjà confirmée (idempotent)."
                        : "Réservation confirmée et tickets émis.",
                    AlreadyConfirmed = confirmed.AlreadyConfirmed,
                    ReservationExpiresAtUtc = null
                };
            }
            catch (Exception ex)
            {
                await TryRollbackHoldAsync(hold.IdSiteTouristiqueReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        public async Task<SiteTouristiqueReservationWithPaiementResponseDto> InitiateElectronicAsync(
            SiteTouristiqueReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (!MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "L'endpoint électronique accepte uniquement MOBILE_MONEY ou CARTE_BANCAIRE. " +
                    "Utilisez POST /api/sites-touristiques/reservations/with-paiement pour CASH.");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(
                request.IdSiteTouristiqueJournee, cancellationToken);

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: true, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdSiteTouristiqueJournee,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            await AttachBuyerUserIdAsync(hold.IdSiteTouristiqueReservation, cancellationToken);

            try
            {
                var initiated = await _flexPayInitiationService.InitiateAsync(
                    hold.IdSiteTouristiqueReservation,
                    idSociete,
                    new SiteTouristiqueInitiateFlexPayRequestDto
                    {
                        MethodePaiement = request.Paiement.MethodePaiement,
                        Phone = request.Paiement.Phone,
                        IdSite = effectiveIdSite!.Value,
                        CodeDevisePaiement = request.Paiement.CodeDevisePaiement,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request)
                    },
                    cancellationToken);

                var reservation = await _reservationService.GetByIdAsync(
                    hold.IdSiteTouristiqueReservation,
                    idSociete,
                    cancellationToken)
                    ?? throw new KeyNotFoundException(
                        $"Réservation site touristique {hold.IdSiteTouristiqueReservation} introuvable après initiation FlexPay.");

                return new SiteTouristiqueReservationWithPaiementResponseDto
                {
                    Reservation = reservation,
                    Payment = initiated.Payment,
                    Tickets = new List<SiteTouristiqueTicketResponseDto>(),
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
                await TryRollbackHoldAsync(hold.IdSiteTouristiqueReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Staff : société JWT (guichet). Client / non-staff : société organisatrice de la session Published
        /// (aligné catalogue public multi-sociétés).
        /// </summary>
        private async Task<int> ResolvePurchaseSocieteIdAsync(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.IsStaff && !_currentUserService.IsSuperAdmin)
            {
                var jwtSocieteId = SiteTouristiqueTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var belongs = await _context.SiteTouristiqueJournees
                    .AsNoTracking()
                    .AnyAsync(
                        s => s.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && s.IdSociete == jwtSocieteId,
                        cancellationToken);
                if (!belongs)
                {
                    throw new KeyNotFoundException(
                        $"Session site touristique {idSiteTouristiqueJournee} introuvable pour la société {jwtSocieteId}.");
                }

                return jwtSocieteId;
            }

            // Client, Super-Admin achat catalogue, ou non-staff : société de la session Published
            var journee = await _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Where(s => s.IdSiteTouristiqueJournee == idSiteTouristiqueJournee
                            && s.Status == SiteTouristiqueStatus.Published)
                .Select(s => new { s.IdSociete })
                .FirstOrDefaultAsync(cancellationToken);

            if (journee == null)
            {
                throw new KeyNotFoundException(
                    $"Session site touristique {idSiteTouristiqueJournee} introuvable ou non publiée.");
            }

            return journee.IdSociete;
        }

        private async Task<int?> ResolveEffectiveIdSiteAsync(
            int idSociete,
            SiteTouristiqueReservationWithPaiementRequestDto request,
            bool requireSite,
            CancellationToken cancellationToken)
        {
            int? fromPaiement = request.Paiement.IdSite is > 0
                ? request.Paiement.IdSite
                : null;

            var sessionRow = await (
                from j in _context.SiteTouristiqueJournees.AsNoTracking()
                join lieu in _context.SiteTouristiques.AsNoTracking()
                    on j.IdSiteTouristique equals lieu.IdSiteTouristique
                where j.IdSiteTouristiqueJournee == request.IdSiteTouristiqueJournee
                      && j.IdSociete == idSociete
                select new { lieu.IdSite }
            ).FirstOrDefaultAsync(cancellationToken);

            if (sessionRow == null)
            {
                throw new KeyNotFoundException(
                    $"Journée site touristique {request.IdSiteTouristiqueJournee} introuvable pour la société {idSociete}.");
            }

            var effective = fromPaiement ?? sessionRow.IdSite;

            if (requireSite && (!effective.HasValue || effective.Value <= 0))
            {
                throw new InvalidOperationException(
                    "IdSite est obligatoire pour le paiement électronique site touristique " +
                    "(fournir paiement.idSite ou définir idSite sur le lieu).");
            }

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, effective, idSociete, cancellationToken);

            return effective;
        }

        private static void ValidateSharedRequest(SiteTouristiqueReservationWithPaiementRequestDto request)
        {
            if (request.IdSiteTouristiqueJournee <= 0)
                throw new InvalidOperationException("IdSiteTouristiqueJournee est obligatoire.");

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins un item de hold est requis.");

            if (request.Paiement == null || string.IsNullOrWhiteSpace(request.Paiement.MethodePaiement))
                throw new InvalidOperationException("Paiement.MethodePaiement est obligatoire.");
        }

        private async Task AttachBuyerUserIdAsync(
            int idSiteTouristiqueReservation,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
                return;

            var reservation = await _context.SiteTouristiqueReservations
                .FirstOrDefaultAsync(r => r.IdSiteTouristiqueReservation == idSiteTouristiqueReservation, cancellationToken);
            if (reservation == null || reservation.IdUtilisateur == userId)
                return;

            reservation.IdUtilisateur = userId;
            reservation.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static SiteTouristiqueHoldRequestDto ToHoldRequest(
            SiteTouristiqueReservationWithPaiementRequestDto request,
            int? effectiveIdSite) =>
            new()
            {
                CustomerRef = request.CustomerRef,
                IdempotencyKey = request.IdempotencyKey,
                IdSite = effectiveIdSite,
                Items = request.Items
            };

        private static string? ResolvePaymentIdempotencyKey(SiteTouristiqueReservationWithPaiementRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey))
                return request.Paiement.IdempotencyKey.Trim();

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return request.IdempotencyKey.Trim() + ":pay";

            return null;
        }

        private async Task TryRollbackHoldAsync(
            int idSiteTouristiqueReservation,
            int idSociete,
            Exception cause,
            CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.CancelAsync(idSiteTouristiqueReservation, idSociete, cancellationToken);
                _logger.LogWarning(
                    cause,
                    "Rollback hold site touristique après échec 2e étape — IdReservation={Id}",
                    idSiteTouristiqueReservation);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Échec rollback hold site touristique IdReservation={Id} (cause initiale loguée séparément)",
                    idSiteTouristiqueReservation);
            }
        }
    }
}
