using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public class EvenementReservationWithPaiementService : IEvenementReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEvenementHoldService _holdService;
        private readonly IEvenementPaymentService _paymentService;
        private readonly IEvenementFlexPayInitiationService _flexPayInitiationService;
        private readonly IEvenementCommandeFlexPayService _commandeFlexPayService;
        private readonly IEvenementReservationService _reservationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<EvenementReservationWithPaiementService> _logger;

        public EvenementReservationWithPaiementService(
            CongoTravelDbContext context,
            IEvenementHoldService holdService,
            IEvenementPaymentService paymentService,
            IEvenementFlexPayInitiationService flexPayInitiationService,
            IEvenementCommandeFlexPayService commandeFlexPayService,
            IEvenementReservationService reservationService,
            ICurrentUserService currentUserService,
            ILogger<EvenementReservationWithPaiementService> logger)
        {
            _context = context;
            _holdService = holdService;
            _paymentService = paymentService;
            _flexPayInitiationService = flexPayInitiationService;
            _commandeFlexPayService = commandeFlexPayService;
            _reservationService = reservationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<EvenementReservationWithPaiementResponseDto> CreateCashAsync(
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements Mobile Money et carte bancaire doivent utiliser " +
                    "POST /api/events/reservations/with-paiement-electronique. " +
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            if (!MethodePaiementHelper.IsCash(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(
                request.IdEvenementSession, cancellationToken);

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: false, cancellationToken);

            var hold = await _holdService.CreateHoldAsync(
                request.IdEvenementSession,
                idSociete,
                ToHoldRequest(request, effectiveIdSite),
                cancellationToken);

            await AttachBuyerAsync(hold.IdEvenementReservation, cancellationToken);

            try
            {
                var confirmed = await _paymentService.ConfirmPaymentAsync(
                    hold.IdEvenementReservation,
                    idSociete,
                    new EvenementConfirmPaymentRequestDto
                    {
                        MethodePaiement = MethodePaiementHelper.Cash,
                        IdempotencyKey = ResolvePaymentIdempotencyKey(request),
                        ReferenceTransaction = request.Paiement.ReferenceTransaction
                    },
                    cancellationToken);

                return new EvenementReservationWithPaiementResponseDto
                {
                    Reservation = confirmed.Reservation,
                    Payment = confirmed.Payment,
                    Tickets = confirmed.Reservation.Tickets ?? new List<EvenementTicketResponseDto>(),
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
                await TryRollbackHoldAsync(hold.IdEvenementReservation, idSociete, ex, cancellationToken);
                throw;
            }
        }

        public async Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            EvenementReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            ValidateSharedRequest(request);
            if (!MethodePaiementHelper.IsElectronic(request.Paiement.MethodePaiement))
            {
                throw new InvalidOperationException(
                    "L'endpoint électronique accepte uniquement MOBILE_MONEY ou CARTE_BANCAIRE. " +
                    "Utilisez POST /api/events/reservations/with-paiement pour CASH.");
            }

            var idSociete = await ResolvePurchaseSocieteIdAsync(
                request.IdEvenementSession, cancellationToken);

            var effectiveIdSite = await ResolveEffectiveIdSiteAsync(
                idSociete, request, requireSite: true, cancellationToken);

            return await _commandeFlexPayService.InitiateElectronicAsync(
                request,
                idSociete,
                effectiveIdSite!.Value,
                cancellationToken);
        }

        /// <summary>
        /// Staff : société JWT (guichet). Client / non-staff : société organisatrice de la session Published
        /// (aligné catalogue public multi-sociétés).
        /// </summary>
        private async Task<int> ResolvePurchaseSocieteIdAsync(
            int idEvenementSession,
            CancellationToken cancellationToken)
        {
            if (_currentUserService.IsStaff && !_currentUserService.IsSuperAdmin)
            {
                var jwtSocieteId = EvenementTenancyGuard.ResolveEffectiveSocieteId(_currentUserService);
                var belongs = await _context.EvenementSessions
                    .AsNoTracking()
                    .AnyAsync(
                        s => s.IdEvenementSession == idEvenementSession && s.IdSociete == jwtSocieteId,
                        cancellationToken);
                if (!belongs)
                {
                    throw new KeyNotFoundException(
                        $"Session événement {idEvenementSession} introuvable pour la société {jwtSocieteId}.");
                }

                return jwtSocieteId;
            }

            // Client, Super-Admin achat catalogue, ou non-staff : société de la session Published
            var session = await _context.EvenementSessions
                .AsNoTracking()
                .Where(s => s.IdEvenementSession == idEvenementSession
                            && s.Status == EvenementSessionStatus.Published)
                .Select(s => new { s.IdSociete })
                .FirstOrDefaultAsync(cancellationToken);

            if (session == null)
            {
                throw new KeyNotFoundException(
                    $"Session événement {idEvenementSession} introuvable ou non publiée.");
            }

            return session.IdSociete;
        }

        private async Task<int?> ResolveEffectiveIdSiteAsync(
            int idSociete,
            EvenementReservationWithPaiementRequestDto request,
            bool requireSite,
            CancellationToken cancellationToken)
        {
            int? fromPaiement = request.Paiement.IdSite is > 0
                ? request.Paiement.IdSite
                : null;

            var sessionRow = await _context.EvenementSessions
                .AsNoTracking()
                .Where(s => s.IdEvenementSession == request.IdEvenementSession
                            && s.IdSociete == idSociete)
                .Select(s => new { s.IdSite })
                .FirstOrDefaultAsync(cancellationToken);

            if (sessionRow == null)
            {
                throw new KeyNotFoundException(
                    $"Session événement {request.IdEvenementSession} introuvable pour la société {idSociete}.");
            }

            var effective = fromPaiement ?? sessionRow.IdSite;

            if (requireSite && (!effective.HasValue || effective.Value <= 0))
            {
                throw new InvalidOperationException(
                    "IdSite est obligatoire pour le paiement électronique événement " +
                    "(fournir paiement.idSite ou définir idSite sur la session).");
            }

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                _context, effective, idSociete, cancellationToken);

            return effective;
        }

        private static void ValidateSharedRequest(EvenementReservationWithPaiementRequestDto request)
        {
            if (request.IdEvenementSession <= 0)
                throw new InvalidOperationException("IdEvenementSession est obligatoire.");

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins un item de hold est requis.");

            if (request.Paiement == null || string.IsNullOrWhiteSpace(request.Paiement.MethodePaiement))
                throw new InvalidOperationException("Paiement.MethodePaiement est obligatoire.");
        }

        private async Task AttachBuyerAsync(
            int idEvenementReservation,
            CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
                return;

            var reservation = await _context.EvenementReservations
                .FirstOrDefaultAsync(r => r.IdEvenementReservation == idEvenementReservation, cancellationToken);
            if (reservation == null)
                return;

            var changed = false;
            if (reservation.IdUtilisateur != userId)
            {
                reservation.IdUtilisateur = userId;
                changed = true;
            }

            // Ne pas écraser un IdClient déjà posé via le body (hold).
            if (reservation.IdClient is not > 0)
            {
                var idClient = await _context.Utilisateurs
                    .AsNoTracking()
                    .Where(u => u.IdUtilisateur == userId)
                    .Select(u => u.IdClient)
                    .FirstOrDefaultAsync(cancellationToken);

                if (idClient is > 0)
                {
                    reservation.IdClient = idClient;
                    changed = true;
                }
            }

            if (!changed)
                return;

            reservation.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static EvenementHoldRequestDto ToHoldRequest(
            EvenementReservationWithPaiementRequestDto request,
            int? effectiveIdSite) =>
            new()
            {
                CustomerRef = request.CustomerRef,
                IdempotencyKey = request.IdempotencyKey,
                IdSite = effectiveIdSite,
                IdClient = request.IdClient,
                Items = request.Items
            };

        private static string? ResolvePaymentIdempotencyKey(EvenementReservationWithPaiementRequestDto request)
        {
            if (!string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey))
                return request.Paiement.IdempotencyKey.Trim();

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return request.IdempotencyKey.Trim() + ":pay";

            return null;
        }

        private async Task TryRollbackHoldAsync(
            int idEvenementReservation,
            int idSociete,
            Exception cause,
            CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.CancelAsync(idEvenementReservation, idSociete, cancellationToken);
                _logger.LogWarning(
                    cause,
                    "Rollback hold événement après échec 2e étape — IdReservation={Id}",
                    idEvenementReservation);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Échec rollback hold événement IdReservation={Id} (cause initiale loguée séparément)",
                    idEvenementReservation);
            }
        }
    }
}
