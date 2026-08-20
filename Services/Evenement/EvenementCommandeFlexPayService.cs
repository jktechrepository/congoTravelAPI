using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementCommandeFlexPayService
    {
        Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            EvenementReservationWithPaiementRequestDto request,
            int idSociete,
            int effectiveIdSite,
            CancellationToken cancellationToken = default);

        Task<EvenementReservation> FinalizeCommandeSuccessAsync(
            EvenementCommandeEnAttente commande,
            EvenementPayment payment,
            CancellationToken cancellationToken = default);

        Task FailCommandeAsync(
            EvenementCommandeEnAttente commande,
            EvenementPayment? payment,
            CancellationToken cancellationToken = default);
    }

    public class EvenementCommandeFlexPayService : IEvenementCommandeFlexPayService
    {
        private const int MaxReferenceAttempts = 10;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly CongoTravelDbContext _context;
        private readonly IEvenementInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly IEvenementInventoryConfirmStrategyFactory _confirmStrategyFactory;
        private readonly IEvenementInventoryCancelStrategyFactory _cancelStrategyFactory;
        private readonly IEvenementReservationConfirmationService _confirmationService;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IFlexPayService _flexPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ICurrentUserService? _currentUserService;
        private readonly ILogger<EvenementCommandeFlexPayService> _logger;

        public EvenementCommandeFlexPayService(
            CongoTravelDbContext context,
            IEvenementInventoryHoldStrategyFactory holdStrategyFactory,
            IEvenementInventoryConfirmStrategyFactory confirmStrategyFactory,
            IEvenementInventoryCancelStrategyFactory cancelStrategyFactory,
            IEvenementReservationConfirmationService confirmationService,
            IConfigSocieteRepository configSocieteRepository,
            IFlexPayService flexPayService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            IDeviseMontantConverter deviseMontantConverter,
            ILogger<EvenementCommandeFlexPayService> logger,
            ICurrentUserService? currentUserService = null)
        {
            _context = context;
            _holdStrategyFactory = holdStrategyFactory;
            _confirmStrategyFactory = confirmStrategyFactory;
            _cancelStrategyFactory = cancelStrategyFactory;
            _confirmationService = confirmationService;
            _configSocieteRepository = configSocieteRepository;
            _flexPayService = flexPayService;
            _httpContextAccessor = httpContextAccessor;
            _flexPayOptions = flexPayOptions.Value;
            _infoPaiementResolution = infoPaiementResolution;
            _deviseMontantConverter = deviseMontantConverter;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<EvenementReservationWithPaiementResponseDto> InitiateElectronicAsync(
            EvenementReservationWithPaiementRequestDto request,
            int idSociete,
            int effectiveIdSite,
            CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(request.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(request.Paiement.MethodePaiement);

            if (!_flexPayOptions.IsEventEnabled)
            {
                throw new InvalidOperationException(
                    "Le paiement électronique FlexPay événement n'est pas activé sur cet environnement.");
            }

            if (methode == MethodePaiementHelper.MobileMoney && string.IsNullOrWhiteSpace(request.Paiement.Phone))
                throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                effectiveIdSite, idSociete, cancellationToken);

            if (methode == MethodePaiementHelper.MobileMoney && !infoPaiement.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money désactivé pour ce site.");

            if (methode == MethodePaiementHelper.CarteBancaire && !infoPaiement.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var idempotencyKey = EvenementIdempotencyHelper.NormalizeKey(request.Paiement.IdempotencyKey)
                ?? EvenementIdempotencyHelper.NormalizeKey(request.IdempotencyKey);

            if (idempotencyKey != null)
            {
                var existingCmd = await _context.EvenementCommandesEnAttente
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, cancellationToken);

                if (existingCmd != null)
                {
                    var existingPayment = existingCmd.IdPaiementEnAttente is int pid
                        ? await _context.EvenementPayments.AsNoTracking()
                            .FirstOrDefaultAsync(p => p.IdEvenementPayment == pid, cancellationToken)
                        : null;

                    return BuildEnAttenteResponse(existingCmd, existingPayment, alreadyInitiated: true);
                }
            }

            await _configSocieteRepository.EnsureReservationsActivesAsync(idSociete, cancellationToken);

            var dbStrategy = _context.Database.CreateExecutionStrategy();
            return await dbStrategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? transaction = null;
                if (_context.Database.IsRelational())
                    transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                EvenementCommandeEnAttente? commande = null;
                try
                {
                    var session = await LoadSessionForHoldAsync(
                        request.IdEvenementSession, idSociete, cancellationToken);

                    var config = await _configSocieteRepository.GetOrCreateAsync(idSociete, cancellationToken);
                    var utcNow = DateTime.UtcNow;
                    var expiresAt = EvenementHoldDurationHelper.ComputeExpiresAtUtc(utcNow, config);

                    var holdStrategy = _holdStrategyFactory.GetStrategy(session.InventoryMode);
                    var holdRequest = BuildHoldRequest(session, request, expiresAt);
                    var strategyResult = await holdStrategy.ReserveHoldAsync(holdRequest, cancellationToken);

                    var reference = await GenerateUniqueReservationReferenceAsync(idSociete, cancellationToken);
                    var snapshot = new EvenementCommandeSnapshotDto
                    {
                        Request = request,
                        Lines = strategyResult.Lines.Select(l => new EvenementCommandeSnapshotLineDto
                        {
                            LineType = l.LineType,
                            Quantite = l.Quantite,
                            PrixUnitaire = l.PrixUnitaire,
                            CodeDevise = l.CodeDevise,
                            IdEvenementSessionClassQuota = l.IdEvenementSessionClassQuota,
                            IdEvenementSessionSeat = l.IdEvenementSessionSeat
                        }).ToList(),
                        MontantSousTotal = strategyResult.MontantSousTotal,
                        CodeDevise = strategyResult.Lines.First().CodeDevise,
                        ReferenceReservation = reference,
                        CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef)
                            ? null
                            : request.CustomerRef.Trim()
                    };

                    var (idUtilisateur, idClient) = await ResolveBuyerAsync(request.IdClient, cancellationToken);

                    var codeDeviseTarif = snapshot.CodeDevise;
                    var montantTarif = snapshot.MontantSousTotal;
                    var codeDevisePaiementRaw = string.IsNullOrWhiteSpace(request.Paiement.CodeDevisePaiement)
                        ? codeDeviseTarif
                        : request.Paiement.CodeDevisePaiement.Trim().ToUpperInvariant();
                    var codeDevisePaiement = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(
                        codeDevisePaiementRaw, "FlexPay événement");
                    FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(
                        _flexPayOptions, methode, codeDevisePaiement, "FlexPay événement");

                    decimal montantFlexPay = montantTarif;
                    decimal taux = 1m;
                    if (!string.Equals(codeDeviseTarif, codeDevisePaiement, StringComparison.Ordinal))
                    {
                        var conversion = await _deviseMontantConverter.ConvertAsync(
                            idSociete, montantTarif, codeDeviseTarif, codeDevisePaiement,
                            DateTime.UtcNow, cancellationToken);
                        montantFlexPay = conversion.MontantCible;
                        taux = conversion.Taux;
                    }

                    if (codeDevisePaiement == "CDF")
                        montantFlexPay = Math.Round(montantFlexPay, 0, MidpointRounding.AwayFromZero);

                    commande = new EvenementCommandeEnAttente
                    {
                        IdSociete = idSociete,
                        IdEvenementSession = session.IdEvenementSession,
                        IdSite = effectiveIdSite,
                        IdUtilisateur = idUtilisateur,
                        IdClient = idClient,
                        MethodePaiement = methode,
                        MontantTarif = montantTarif,
                        CodeDeviseTarif = codeDeviseTarif,
                        MontantFlexPay = montantFlexPay,
                        CodeDevisePaiement = codeDevisePaiement,
                        TauxVersDevisePaiement = taux,
                        IdempotencyKey = idempotencyKey,
                        PayloadMetierJson = JsonSerializer.Serialize(snapshot, JsonOptions),
                        DateCreation = utcNow,
                        DateExpiration = expiresAt
                    };

                    _context.EvenementCommandesEnAttente.Add(commande);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (session.InventoryMode == EvenementInventoryMode.SeatNumbered)
                        await LinkSeatsToCommandeAsync(commande.IdEvenementCommandeEnAttente, strategyResult.Lines, cancellationToken);

                    var paymentReference = await GenerateUniquePaymentReferenceAsync(idSociete, cancellationToken);
                    var flexReference = EvenementFlexPayReferenceHelper.BuildMerchantReferenceForCommande(
                        commande.IdEvenementCommandeEnAttente);
                    var pendingOrder = EvenementFlexPayReferenceHelper.BuildPendingOrderNumberForCommande(
                        commande.IdEvenementCommandeEnAttente);

                    var payment = new EvenementPayment
                    {
                        IdEvenementReservation = null,
                        IdEvenementCommandeEnAttente = commande.IdEvenementCommandeEnAttente,
                        IdSite = effectiveIdSite,
                        ReferencePaiement = paymentReference,
                        Provider = EvenementFlexPayConstants.Provider,
                        ProviderTxRef = pendingOrder,
                        Status = EvenementPaymentStatus.PENDING,
                        Montant = montantFlexPay,
                        CodeDevise = codeDevisePaiement,
                        MontantTarif = montantTarif,
                        CodeDeviseTarif = codeDeviseTarif,
                        TauxVersDevisePaiement = taux,
                        IdempotencyKey = idempotencyKey,
                        DateCreation = utcNow
                    };

                    _context.EvenementPayments.Add(payment);
                    await _context.SaveChangesAsync(cancellationToken);

                    commande.IdPaiementEnAttente = payment.IdEvenementPayment;
                    commande.OrderNumberFlexPay = pendingOrder;
                    commande.ReferenceFlexPay = flexReference;
                    await _context.SaveChangesAsync(cancellationToken);

                    if (transaction != null)
                        await transaction.CommitAsync(cancellationToken);

                    // FlexPay hors transaction DB (réseau)
                    var callbackUrl = FlexPayUrlHelper.ResolveEvenementCallbackUrl(
                        _httpContextAccessor.HttpContext,
                        _flexPayOptions.CallbackBaseUrl,
                        _flexPayOptions.EventCallbackRelativePath,
                        _flexPayOptions.ForceProductionCallbackInDev);

                    FlexPayPaymentResponseDto flexResponse;
                    if (methode == MethodePaiementHelper.CarteBancaire)
                    {
                        flexResponse = await _flexPayService.InitierPaiementCarteV1Async(
                            infoPaiement.CodeMarchand,
                            infoPaiement.ApiToken,
                            flexReference,
                            montantFlexPay,
                            codeDevisePaiement,
                            $"Réservation événement {reference}",
                            callbackUrl,
                            FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "approve"),
                            FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "cancel"),
                            FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "decline"),
                            cancellationToken);
                    }
                    else
                    {
                        flexResponse = await _flexPayService.InitierPaiementMobileMoneyAsync(
                            infoPaiement.CodeMarchand,
                            infoPaiement.ApiToken,
                            flexReference,
                            request.Paiement.Phone!.Trim(),
                            montantFlexPay,
                            codeDevisePaiement,
                            callbackUrl,
                            cancellationToken);
                    }

                    var orderNumber = string.IsNullOrWhiteSpace(flexResponse.OrderNumber)
                        ? pendingOrder
                        : flexResponse.OrderNumber.Trim();

                    payment.ProviderTxRef = orderNumber;
                    payment.DateModification = DateTime.UtcNow;
                    commande.OrderNumberFlexPay = orderNumber;
                    await _context.SaveChangesAsync(cancellationToken);

                    if (!flexResponse.IsSuccess)
                    {
                        await FailCommandeAsync(commande, payment, cancellationToken);
                        throw new InvalidOperationException(
                            $"FlexPay a refusé l'initiation : {flexResponse.Message ?? flexResponse.Code ?? "erreur"}");
                    }

                    var message = methode == MethodePaiementHelper.CarteBancaire
                        ? "Redirigez le client vers paymentUrl pour finaliser le paiement carte."
                        : "Validez le paiement sur votre téléphone Mobile Money. La réservation sera confirmée après callback.";

                    _logger.LogInformation(
                        "FlexPay événement commande initiée — Commande={Id}, Order={OrderNumber}",
                        commande.IdEvenementCommandeEnAttente,
                        orderNumber);

                    return BuildEnAttenteResponse(
                        commande,
                        payment,
                        alreadyInitiated: false,
                        orderNumber,
                        flexResponse.ResolvePaymentUrl(),
                        message,
                        flexPayAccepted: true);
                }
                catch
                {
                    if (transaction != null)
                        await transaction.RollbackAsync(cancellationToken);

                    if (commande != null
                        && await _context.EvenementCommandesEnAttente.AnyAsync(
                            c => c.IdEvenementCommandeEnAttente == commande.IdEvenementCommandeEnAttente,
                            cancellationToken))
                    {
                        var payment = commande.IdPaiementEnAttente is int pid
                            ? await _context.EvenementPayments.FirstOrDefaultAsync(
                                p => p.IdEvenementPayment == pid, cancellationToken)
                            : null;
                        await FailCommandeAsync(commande, payment, cancellationToken);
                    }

                    throw;
                }
                finally
                {
                    if (transaction != null)
                        await transaction.DisposeAsync();
                }
            });
        }

        public async Task<EvenementReservation> FinalizeCommandeSuccessAsync(
            EvenementCommandeEnAttente commande,
            EvenementPayment payment,
            CancellationToken cancellationToken = default)
        {
            var snapshot = JsonSerializer.Deserialize<EvenementCommandeSnapshotDto>(
                commande.PayloadMetierJson, JsonOptions)
                ?? throw new InvalidOperationException("Payload commande événement invalide.");

            var session = await LoadSessionForHoldAsync(
                commande.IdEvenementSession, commande.IdSociete, cancellationToken);

            var utcNow = DateTime.UtcNow;
            var reservation = new EvenementReservation
            {
                IdSociete = commande.IdSociete,
                IdEvenementSession = commande.IdEvenementSession,
                IdSite = commande.IdSite,
                ReferenceReservation = snapshot.ReferenceReservation,
                CustomerRef = snapshot.CustomerRef,
                IdUtilisateur = commande.IdUtilisateur,
                IdClient = commande.IdClient,
                Status = EvenementReservationStatus.HOLD,
                ExpiresAtUtc = commande.DateExpiration,
                MontantSousTotal = snapshot.MontantSousTotal,
                CodeDevise = snapshot.CodeDevise,
                DateCreation = utcNow
            };

            foreach (var line in snapshot.Lines)
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
                await TransferSeatOwnershipToReservationAsync(
                    commande.IdEvenementCommandeEnAttente,
                    reservation.IdEvenementReservation,
                    snapshot.Lines
                        .Where(l => l.IdEvenementSessionSeat.HasValue)
                        .Select(l => l.IdEvenementSessionSeat!.Value)
                        .ToList(),
                    cancellationToken);
            }

            // Recharge avec Lines pour confirm
            reservation = await _context.EvenementReservations
                .Include(r => r.Lines)
                .FirstAsync(r => r.IdEvenementReservation == reservation.IdEvenementReservation, cancellationToken);

            var trackedPayment = await _context.EvenementPayments
                .FirstAsync(p => p.IdEvenementPayment == payment.IdEvenementPayment, cancellationToken);

            await _confirmationService.ConfirmHoldAndEmitTicketsAsync(
                reservation, trackedPayment, commande.IdSociete, cancellationToken);

            trackedPayment.IdEvenementReservation = reservation.IdEvenementReservation;
            trackedPayment.IdEvenementCommandeEnAttente = null;
            trackedPayment.DateModification = DateTime.UtcNow;

            DetachCommande(commande.IdEvenementCommandeEnAttente);
            var trackedCmd = await _context.EvenementCommandesEnAttente
                .FirstAsync(c => c.IdEvenementCommandeEnAttente == commande.IdEvenementCommandeEnAttente, cancellationToken);
            trackedCmd.IdPaiementEnAttente = null;
            await _context.SaveChangesAsync(cancellationToken);

            _context.EvenementCommandesEnAttente.Remove(trackedCmd);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Commande événement finalisée — Reservation={IdReservation}, Order={Order}",
                reservation.IdEvenementReservation,
                trackedPayment.ProviderTxRef);

            return reservation;
        }

        public async Task FailCommandeAsync(
            EvenementCommandeEnAttente commande,
            EvenementPayment? payment,
            CancellationToken cancellationToken = default)
        {
            var trackedCmd = await _context.EvenementCommandesEnAttente
                .FirstOrDefaultAsync(
                    c => c.IdEvenementCommandeEnAttente == commande.IdEvenementCommandeEnAttente,
                    cancellationToken);

            if (trackedCmd == null)
                return;

            EvenementCommandeSnapshotDto? snapshot = null;
            try
            {
                snapshot = JsonSerializer.Deserialize<EvenementCommandeSnapshotDto>(
                    trackedCmd.PayloadMetierJson, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload commande événement illisible — Commande={Id}", trackedCmd.IdEvenementCommandeEnAttente);
            }

            if (snapshot?.Lines.Count > 0)
            {
                var session = await _context.EvenementSessions
                    .FirstOrDefaultAsync(
                        s => s.IdEvenementSession == trackedCmd.IdEvenementSession
                             && s.IdSociete == trackedCmd.IdSociete,
                        cancellationToken);

                if (session != null)
                {
                    var transient = new EvenementReservation
                    {
                        IdEvenementReservation = 0,
                        IdSociete = trackedCmd.IdSociete,
                        IdEvenementSession = trackedCmd.IdEvenementSession,
                        Status = EvenementReservationStatus.HOLD,
                        Lines = snapshot.Lines.Select(l => new EvenementReservationLine
                        {
                            LineType = l.LineType,
                            Quantite = l.Quantite,
                            PrixUnitaire = l.PrixUnitaire,
                            CodeDevise = l.CodeDevise,
                            IdEvenementSessionClassQuota = l.IdEvenementSessionClassQuota,
                            IdEvenementSessionSeat = l.IdEvenementSessionSeat
                        }).ToList()
                    };

                    try
                    {
                        var cancelStrategy = _cancelStrategyFactory.GetStrategy(session.InventoryMode);
                        await cancelStrategy.ReleaseReservationAsync(
                            new EvenementInventoryCancelRequest
                            {
                                Reservation = transient,
                                Session = session,
                                FromConfirmedSale = false
                            },
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Release inventaire commande événement échoué — Commande={Id}",
                            trackedCmd.IdEvenementCommandeEnAttente);
                    }

                    if (session.InventoryMode == EvenementInventoryMode.SeatNumbered)
                    {
                        var seatIds = snapshot.Lines
                            .Where(l => l.IdEvenementSessionSeat.HasValue)
                            .Select(l => l.IdEvenementSessionSeat!.Value)
                            .ToList();
                        if (seatIds.Count > 0)
                        {
                            var seats = await _context.EvenementSessionSeats
                                .Where(s => seatIds.Contains(s.IdEvenementSessionSeat)
                                            && s.IdEvenementCommandeEnAttenteCourante
                                                == trackedCmd.IdEvenementCommandeEnAttente)
                                .ToListAsync(cancellationToken);
                            foreach (var seat in seats)
                                seat.IdEvenementCommandeEnAttenteCourante = null;
                        }
                    }
                }
            }

            EvenementPayment? trackedPayment = payment;
            if (trackedPayment == null && trackedCmd.IdPaiementEnAttente is int pid)
            {
                trackedPayment = await _context.EvenementPayments
                    .FirstOrDefaultAsync(p => p.IdEvenementPayment == pid, cancellationToken);
            }
            else if (trackedPayment != null)
            {
                trackedPayment = await _context.EvenementPayments
                    .FirstOrDefaultAsync(
                        p => p.IdEvenementPayment == trackedPayment.IdEvenementPayment,
                        cancellationToken);
            }

            if (trackedPayment != null
                && trackedPayment.Status is not (EvenementPaymentStatus.SUCCEEDED or EvenementPaymentStatus.REFUNDED))
            {
                trackedPayment.Status = EvenementPaymentStatus.FAILED;
                trackedPayment.IdEvenementCommandeEnAttente = null;
                trackedPayment.DateModification = DateTime.UtcNow;
            }

            trackedCmd.IdPaiementEnAttente = null;
            await _context.SaveChangesAsync(cancellationToken);

            _context.EvenementCommandesEnAttente.Remove(trackedCmd);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Commande événement échouée / purgée — Commande={Id}, Payment={PaymentId}",
                commande.IdEvenementCommandeEnAttente,
                trackedPayment?.IdEvenementPayment);
        }

        private async Task<EvenementSession> LoadSessionForHoldAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken)
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

            EvenementSessionSalesEligibilityHelper.EnsureCanSell(session, DateTime.UtcNow);
            return session;
        }

        private static EvenementInventoryHoldRequest BuildHoldRequest(
            EvenementSession session,
            EvenementReservationWithPaiementRequestDto request,
            DateTime expiresAt)
        {
            var holdRequest = new EvenementInventoryHoldRequest
            {
                Session = session,
                Items = request.Items,
                HoldExpiresAtUtc = expiresAt
            };

            if (session.InventoryMode == EvenementInventoryMode.GlobalQuota)
            {
                if (session.GlobalQuota == null)
                    throw new InvalidOperationException("Inventaire global manquant pour cette session.");
                holdRequest.PrixUnitaire = session.GlobalQuota.PrixUnitaire;
                holdRequest.CodeDevise = session.GlobalQuota.CodeDevise;
            }

            return holdRequest;
        }

        private async Task LinkSeatsToCommandeAsync(
            Guid idCommande,
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
                    throw new InvalidOperationException($"Siège {line.IdEvenementSessionSeat} introuvable après hold.");

                seat.IdEvenementCommandeEnAttenteCourante = idCommande;
                seat.IdEvenementReservationCourante = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task TransferSeatOwnershipToReservationAsync(
            Guid idCommande,
            int idReservation,
            IReadOnlyList<int> seatIds,
            CancellationToken cancellationToken)
        {
            if (seatIds.Count == 0)
                return;

            var seats = await _context.EvenementSessionSeats
                .Where(s => seatIds.Contains(s.IdEvenementSessionSeat))
                .ToListAsync(cancellationToken);

            foreach (var seat in seats)
            {
                seat.IdEvenementCommandeEnAttenteCourante = null;
                seat.IdEvenementReservationCourante = idReservation;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<(int? IdUtilisateur, int? IdClient)> ResolveBuyerAsync(
            int? idClientFromRequest,
            CancellationToken cancellationToken)
        {
            int? idUtilisateur = null;
            int? idClient = idClientFromRequest is > 0 ? idClientFromRequest : null;

            var userId = _currentUserService?.UserId ?? 0;
            if (userId > 0)
            {
                idUtilisateur = userId;
                if (idClient is null or <= 0)
                {
                    idClient = await _context.Utilisateurs
                        .AsNoTracking()
                        .Where(u => u.IdUtilisateur == userId)
                        .Select(u => u.IdClient)
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }

            return (idUtilisateur, idClient);
        }

        private EvenementReservationWithPaiementResponseDto BuildEnAttenteResponse(
            EvenementCommandeEnAttente commande,
            EvenementPayment? payment,
            bool alreadyInitiated,
            string? orderNumber = null,
            string? paymentUrl = null,
            string? message = null,
            bool? flexPayAccepted = null)
        {
            var snapshot = JsonSerializer.Deserialize<EvenementCommandeSnapshotDto>(
                commande.PayloadMetierJson, JsonOptions);

            return new EvenementReservationWithPaiementResponseDto
            {
                Reservation = new EvenementReservationResponseDto
                {
                    IdEvenementReservation = 0,
                    IdSociete = commande.IdSociete,
                    IdEvenementSession = commande.IdEvenementSession,
                    IdSite = commande.IdSite,
                    ReferenceReservation = snapshot?.ReferenceReservation ?? string.Empty,
                    CustomerRef = snapshot?.CustomerRef,
                    IdUtilisateur = commande.IdUtilisateur,
                    IdClient = commande.IdClient,
                    Status = "EN_ATTENTE_PAIEMENT",
                    ExpiresAtUtc = commande.DateExpiration,
                    MontantSousTotal = commande.MontantTarif,
                    CodeDevise = commande.CodeDeviseTarif,
                    DateCreation = commande.DateCreation
                },
                Payment = payment == null
                    ? null
                    : new EvenementPaymentResponseDto
                    {
                        IdEvenementPayment = payment.IdEvenementPayment,
                        IdSite = payment.IdSite,
                        ReferencePaiement = payment.ReferencePaiement,
                        Provider = payment.Provider,
                        ProviderTxRef = payment.ProviderTxRef,
                        Status = payment.Status.ToString(),
                        Montant = payment.Montant,
                        CodeDevise = payment.CodeDevise,
                        MontantTarif = payment.MontantTarif,
                        CodeDeviseTarif = payment.CodeDeviseTarif,
                        TauxVersDevisePaiement = payment.TauxVersDevisePaiement,
                        DateCreation = payment.DateCreation
                    },
                Tickets = new List<EvenementTicketResponseDto>(),
                TransactionStatut = "EnAttente",
                Message = message
                    ?? (alreadyInitiated
                        ? "Paiement FlexPay déjà initié pour cette clé d'idempotence."
                        : "Paiement FlexPay initié. Aucune réservation tant que le paiement n'est pas confirmé."),
                OrderNumber = orderNumber ?? commande.OrderNumberFlexPay ?? payment?.ProviderTxRef,
                PaymentUrl = paymentUrl,
                ReservationExpiresAtUtc = commande.DateExpiration,
                MontantFlexPay = commande.MontantFlexPay,
                CodeDevisePaiement = commande.CodeDevisePaiement,
                MontantTarif = commande.MontantTarif,
                CodeDeviseTarif = commande.CodeDeviseTarif,
                TauxApplique = commande.TauxVersDevisePaiement,
                FlexPayAccepted = flexPayAccepted ?? true,
                AlreadyInitiated = alreadyInitiated
            };
        }

        private void DetachCommande(Guid id)
        {
            foreach (var entry in _context.ChangeTracker.Entries<EvenementCommandeEnAttente>()
                         .Where(e => e.Entity.IdEvenementCommandeEnAttente == id)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        private async Task<string> GenerateUniqueReservationReferenceAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = EvenementReferenceGenerator.GenerateReservationReferenceCandidate(idSociete);
                var exists = await _context.EvenementReservations
                    .AsNoTracking()
                    .AnyAsync(r => r.IdSociete == idSociete && r.ReferenceReservation == candidate, cancellationToken);
                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException("Impossible de générer une référence de réservation événement unique.");
        }

        private async Task<string> GenerateUniquePaymentReferenceAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxReferenceAttempts; attempt++)
            {
                var candidate = EvenementReferenceGenerator.GeneratePaymentReferenceCandidate(idSociete);
                var exists = await _context.EvenementPayments
                    .AsNoTracking()
                    .AnyAsync(p => p.ReferencePaiement == candidate, cancellationToken);
                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException("Impossible de générer une référence de paiement événement unique.");
        }
    }
}
