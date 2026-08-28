using System.Data;
using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Transport;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services
{
    /// <summary>
    /// Réservation aller-retour Transport V1 : cash, FlexPay initiate, lecture, annulation atomique.
    /// </summary>
    public class AllerRetourReservationService : IAllerRetourReservationService
    {
        private const decimal ToleranceMontant = 0.05m;

        private readonly CongoTravelDbContext _context;
        private readonly IVoyageSeatAllocationService _seatAllocationService;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly BilletEmissionService _billetEmissionService;
        private readonly IBilletPricingEnrichmentService _billetPricingEnrichment;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISiegeDisponibiliteService _siegeDisponibilite;
        private readonly IFlexPayService _flexPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ILogger<AllerRetourReservationService> _logger;

        public AllerRetourReservationService(
            CongoTravelDbContext context,
            IVoyageSeatAllocationService seatAllocationService,
            IVoyageTarifService voyageTarifService,
            BilletEmissionService billetEmissionService,
            IBilletPricingEnrichmentService billetPricingEnrichment,
            IConfigSocieteRepository configSocieteRepository,
            ICurrentUserService currentUserService,
            ISiegeDisponibiliteService siegeDisponibilite,
            IFlexPayService flexPayService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            IDeviseMontantConverter deviseMontantConverter,
            ILogger<AllerRetourReservationService> logger)
        {
            _context = context;
            _seatAllocationService = seatAllocationService;
            _voyageTarifService = voyageTarifService;
            _billetEmissionService = billetEmissionService;
            _billetPricingEnrichment = billetPricingEnrichment;
            _configSocieteRepository = configSocieteRepository;
            _currentUserService = currentUserService;
            _siegeDisponibilite = siegeDisponibilite;
            _flexPayService = flexPayService;
            _httpContextAccessor = httpContextAccessor;
            _flexPayOptions = flexPayOptions.Value;
            _infoPaiementResolution = infoPaiementResolution;
            _deviseMontantConverter = deviseMontantConverter;
            _logger = logger;
        }

        public async Task<ReservationAllerRetourWithPaiementResponseDto> CreateCashAsync(
            CreateReservationAllerRetourWithPaiementDto dto)
        {
            MethodePaiementHelper.EnsureCashOnlyForGuichetEndpoint(dto.Paiement.MethodePaiement);
            dto.Paiement.MethodePaiement = MethodePaiementHelper.NormalizeForStorage(dto.Paiement.MethodePaiement);

            var transactionId = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? unitOfWork = null;
                if (_context.Database.IsRelational())
                    unitOfWork = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                try
                {
                    var (voyageAller, voyageRetour) = await LoadAndValidateVoyagesAsync(
                        dto.IdVoyageAller,
                        dto.IdVoyageRetour,
                        dto.Passagers,
                        dto.NombreDePlace,
                        dto.IdClient,
                        dto.IdSociete,
                        dto.IdSite,
                        dto.Paiement.IdSite,
                        dto.Paiement.IdSociete);

                    if (dto.Paiement.MontantPaye > dto.Paiement.MontantAPaye)
                        throw new InvalidOperationException("Le montant payé ne peut pas dépasser le montant à payer");

                    var origine = OrigineOperationResolver.Resolve(_currentUserService);
                    var idSociete = voyageAller.IdSociete;
                    var idUtilisateur = dto.IdUtilisateur > 0 ? dto.IdUtilisateur : 1;
                    var idSite = dto.IdSite ?? dto.Paiement.IdSite;

                    var agregat = new ReservationAllerRetour
                    {
                        IdVoyageAller = voyageAller.Id,
                        IdVoyageRetour = voyageRetour.Id,
                        Statut = ReservationAllerRetourStatut.EnAttentePaiement,
                        IdSociete = idSociete,
                        IdClient = dto.IdClient,
                        IdUtilisateur = idUtilisateur,
                        IdSite = idSite,
                        Origine = origine,
                        DateCreation = DateTime.UtcNow
                    };
                    _context.ReservationsAllerRetour.Add(agregat);
                    await _context.SaveChangesAsync();

                    var resAller = await CreateLegReservationAsync(
                        voyageAller.Id,
                        dto.IdClient,
                        idUtilisateur,
                        idSociete,
                        idSite,
                        dto.NombreDePlace,
                        origine,
                        agregat.IdReservationAllerRetour,
                        ReservationAllerRetourLeg.Aller);

                    var resRetour = await CreateLegReservationAsync(
                        voyageRetour.Id,
                        dto.IdClient,
                        idUtilisateur,
                        idSociete,
                        idSite,
                        dto.NombreDePlace,
                        origine,
                        agregat.IdReservationAllerRetour,
                        ReservationAllerRetourLeg.Retour);

                    agregat.IdReservationAller = resAller.IdReservation;
                    agregat.IdReservationRetour = resRetour.IdReservation;

                    var passagersAller = dto.Passagers;
                    var passagersRetour = AllerRetourVoyageCompatibilityHelper.ClonePassagers(dto.Passagers);

                    var passengerIdsAller = await CreatePassengersAsync(passagersAller, resAller);
                    var passengerIdsRetour = await CreatePassengersAsync(passagersRetour, resRetour);

                    var allocAller = await _seatAllocationService.AllocateSeatsForPassengersAsync(
                        voyageAller.Id,
                        resAller.IdReservation,
                        passagersAller.Select((p, i) => (passengerIdsAller[i], p.IdCategorieSiege)).ToList());

                    var allocRetour = await _seatAllocationService.AllocateSeatsForPassengersAsync(
                        voyageRetour.Id,
                        resRetour.IdReservation,
                        passagersRetour.Select((p, i) => (passengerIdsRetour[i], p.IdCategorieSiege)).ToList());

                    var montantAller = await _voyageTarifService.ComputeTotalForSiegesAsync(
                        voyageAller.Id,
                        allocAller.Select(a => a.IdSiege).ToList(),
                        voyageAller.Prix);

                    var montantRetour = await _voyageTarifService.ComputeTotalForSiegesAsync(
                        voyageRetour.Id,
                        allocRetour.Select(a => a.IdSiege).ToList(),
                        voyageRetour.Prix);

                    var montantAttendu = montantAller + montantRetour;
                    if (Math.Abs(dto.Paiement.MontantAPaye - montantAttendu) > ToleranceMontant)
                    {
                        throw new InvalidOperationException(
                            $"Montant à payer incohérent : attendu {montantAttendu} (aller {montantAller} + retour {montantRetour}), reçu {dto.Paiement.MontantAPaye}.");
                    }

                    var paiement = new Paiement
                    {
                        MontantAPaye = dto.Paiement.MontantAPaye,
                        MontantPaye = dto.Paiement.MontantPaye,
                        MethodePaiement = MethodePaiementHelper.NormalizeForStorage(dto.Paiement.MethodePaiement),
                        ReferenceTransaction = dto.Paiement.ReferenceTransaction,
                        Statut = true,
                        StatutPaiementMetier = (int)StatutPaiementMetier.Reussi,
                        IdUtilisateur = dto.Paiement.IdUtilisateur > 0 ? dto.Paiement.IdUtilisateur : idUtilisateur,
                        IdReservation = resAller.IdReservation,
                        IdReservationAllerRetour = agregat.IdReservationAllerRetour,
                        IdSociete = idSociete,
                        IdSite = dto.Paiement.IdSite ?? idSite,
                        DateCreation = DateTime.UtcNow,
                        Origine = origine
                    };
                    paiement.MettreAJourResteAPaye();
                    _context.Paiements.Add(paiement);
                    await _context.SaveChangesAsync();

                    agregat.IdPaiement = paiement.IdPaiement;

                    var billetsAller = new List<Billet>();
                    var billetsRetour = new List<Billet>();

                    if (paiement.EstComplet)
                    {
                        try
                        {
                            billetsAller = (await _billetEmissionService.EmitBilletsPourPaiementAsync(paiement)).ToList();
                            if (billetsAller.Count > 0)
                            {
                                paiement.DateEmissionBillet = DateTime.UtcNow;
                                paiement.IdBilletEmis = billetsAller[0].IdBillet;
                            }

                            billetsRetour = (await _billetEmissionService.EmitBilletsPourReservationAsync(
                                resRetour.IdReservation,
                                paiement.IdSociete,
                                paiement.IdSite)).ToList();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Émission billets AR partielle — TransactionID={TransactionId}, AR={IdAR}",
                                transactionId,
                                agregat.IdReservationAllerRetour);
                        }

                        resAller.StatutReservation = "CONFIRMEE";
                        resRetour.StatutReservation = "CONFIRMEE";
                        agregat.Statut = ReservationAllerRetourStatut.Confirmee;
                    }
                    else
                    {
                        resAller.StatutReservation = "EN_ATTENTE";
                        resRetour.StatutReservation = "EN_ATTENTE";
                        agregat.Statut = ReservationAllerRetourStatut.EnAttentePaiement;
                    }

                    agregat.DateModification = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    if (unitOfWork != null)
                        await unitOfWork.CommitAsync();

                    var detail = await BuildDetailAsync(
                        agregat.IdReservationAllerRetour,
                        billetsAller,
                        billetsRetour);

                    return new ReservationAllerRetourWithPaiementResponseDto
                    {
                        TransactionId = transactionId,
                        Statut = TransactionStatut.Succes,
                        Message = "Réservation aller-retour créée avec succès",
                        DateCreation = DateTime.UtcNow,
                        AllerRetour = detail
                    };
                }
                catch (Exception ex)
                {
                    if (unitOfWork != null)
                        await unitOfWork.RollbackAsync();

                    _logger.LogError(ex, "Échec cash aller-retour — TransactionID={TransactionId}", transactionId);
                    return new ReservationAllerRetourWithPaiementResponseDto
                    {
                        TransactionId = transactionId,
                        Statut = TransactionStatut.Echec,
                        Message = "La transaction a échoué: " + ex.Message,
                        DateCreation = DateTime.UtcNow
                    };
                }
                finally
                {
                    if (unitOfWork != null)
                        await unitOfWork.DisposeAsync();
                }
            });
        }

        public async Task<ReservationAllerRetourWithPaiementResponseDto> InitiateFlexPayAsync(
            InitiateFlexPayReservationAllerRetourDto dto,
            CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(dto.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(dto.Paiement.MethodePaiement);

            if (!_flexPayOptions.Enabled)
            {
                throw new InvalidOperationException(
                    "Le paiement électronique FlexPay n'est pas activé sur cet environnement.");
            }

            if (dto.Paiement.MontantAPaye <= 0)
                throw new InvalidOperationException("FlexPay exige un paiement intégral : montantAPaye doit être > 0.");

            var codeDevisePaiement = FlexPayCurrencyPolicy.NormalizePaymentCurrencyOrThrow(
                dto.Paiement.CodeDevisePaiement,
                "FlexPay");
            FlexPayCurrencyPolicy.EnsureChannelCurrencySupported(
                _flexPayOptions,
                methode,
                codeDevisePaiement,
                "FlexPay");

            if (methode == MethodePaiementHelper.MobileMoney
                && string.IsNullOrWhiteSpace(dto.Paiement.Phone))
            {
                throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");
            }

            var idSite = dto.Paiement.IdSite ?? dto.IdSite
                ?? throw new InvalidOperationException("IdSite requis pour identifier le marchand FlexPay.");

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                idSite, dto.Paiement.IdSociete, cancellationToken);

            if (methode == MethodePaiementHelper.MobileMoney && !infoPaiement.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money désactivé pour ce site.");

            if (methode == MethodePaiementHelper.CarteBancaire && !infoPaiement.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var (voyageAller, voyageRetour) = await LoadAndValidateVoyagesAsync(
                dto.IdVoyageAller,
                dto.IdVoyageRetour,
                dto.Passagers,
                dto.NombreDePlace,
                dto.IdClient,
                dto.IdSociete,
                dto.IdSite,
                dto.Paiement.IdSite,
                dto.Paiement.IdSociete,
                cancellationToken);

            var codeDeviseAller = NormalizeDevise(voyageAller.CodeDevisePrix);
            var codeDeviseRetour = NormalizeDevise(voyageRetour.CodeDevisePrix);
            if (codeDeviseAller != codeDeviseRetour)
            {
                throw new InvalidOperationException(
                    $"Les devises tarif aller ({codeDeviseAller}) et retour ({codeDeviseRetour}) doivent être identiques.");
            }

            var codeDeviseVoyage = codeDeviseAller;
            var config = await _configSocieteRepository.GetOrCreateAsync(voyageAller.IdSociete, cancellationToken);

            var categories = dto.Passagers.Select(p => p.IdCategorieSiege).ToList();
            var idCommande = Guid.NewGuid();
            var holdMinutes = config.DureeHoldFlexPayMinutes > 0
                ? config.DureeHoldFlexPayMinutes
                : (_flexPayOptions.SeatHoldMinutes > 0 ? _flexPayOptions.SeatHoldMinutes : 15);

            var heldAller = await _siegeDisponibilite.CreateHoldsForCategoriesAsync(
                voyageAller.Id, idCommande, categories, holdMinutes, cancellationToken);

            try
            {
                var heldRetour = await _siegeDisponibilite.CreateHoldsForCategoriesAsync(
                    voyageRetour.Id, idCommande, categories, holdMinutes, cancellationToken);

                var montantAller = await _voyageTarifService.ComputeTotalForSiegesAsync(
                    voyageAller.Id, heldAller, voyageAller.Prix);
                var montantRetour = await _voyageTarifService.ComputeTotalForSiegesAsync(
                    voyageRetour.Id, heldRetour, voyageRetour.Prix);

                // Supplément × places × 2 legs
                var supplement = await ElectronicPaymentSupplementHelper.ComputeSupplementInVoyageCurrencyAsync(
                    config,
                    dto.NombreDePlace * 2,
                    codeDeviseVoyage,
                    dto.Paiement.IdSociete,
                    _deviseMontantConverter,
                    DateTime.UtcNow,
                    cancellationToken);

                var montantAttendu = montantAller + montantRetour + supplement;
                if (Math.Abs(dto.Paiement.MontantAPaye - montantAttendu) > ToleranceMontant)
                {
                    throw new InvalidOperationException(
                        $"Montant à payer incohérent : attendu {montantAttendu} {codeDeviseVoyage} " +
                        $"(aller {montantAller} + retour {montantRetour} + supplément {supplement}), reçu {dto.Paiement.MontantAPaye}.");
                }

                decimal montantFlexPay = montantAttendu;
                decimal taux = 1m;
                if (codeDeviseVoyage != codeDevisePaiement)
                {
                    var conversion = await _deviseMontantConverter.ConvertAsync(
                        dto.Paiement.IdSociete,
                        montantAttendu,
                        codeDeviseVoyage,
                        codeDevisePaiement,
                        DateTime.UtcNow,
                        cancellationToken);
                    montantFlexPay = conversion.MontantCible;
                    taux = conversion.Taux;
                }

                if (codeDevisePaiement == "CDF")
                    montantFlexPay = Math.Round(montantFlexPay, 0, MidpointRounding.AwayFromZero);

                var reference = $"RT-{idCommande:N}"[..Math.Min(20, $"RT-{idCommande:N}".Length)];
                var pendingOrder = $"PENDING-{idCommande:N}"[..Math.Min(100, $"PENDING-{idCommande:N}".Length)];
                var payloadJson = JsonSerializer.Serialize(dto);
                var origine = OrigineOperationResolver.Resolve(_currentUserService);

                var commande = new CommandeReservationEnAttente
                {
                    IdCommandeReservationEnAttente = idCommande,
                    IdSociete = dto.Paiement.IdSociete,
                    IdSite = idSite,
                    IdUtilisateur = dto.Paiement.IdUtilisateur,
                    Origine = origine,
                    MethodePaiement = methode,
                    TypeCommande = TypeCommandeReservation.AllerRetour,
                    MontantVoyage = montantAttendu,
                    CodeDeviseVoyage = codeDeviseVoyage,
                    MontantFlexPay = montantFlexPay,
                    CodeDevisePaiement = codeDevisePaiement,
                    TauxVersDevisePaiement = taux,
                    OrderNumberFlexPay = pendingOrder,
                    ReferenceFlexPay = reference,
                    PayloadMetierJson = payloadJson,
                    DateExpiration = DateTime.UtcNow.AddMinutes(holdMinutes)
                };

                var paiement = new Paiement
                {
                    MontantAPaye = montantFlexPay,
                    MontantPaye = 0,
                    CodeDevisePaiement = codeDevisePaiement,
                    CodeDevisePrincipale = codeDeviseVoyage,
                    TauxVersDevisePrincipale = taux,
                    MontantAPayeDevisePrincipale = montantAttendu,
                    MontantPayeDevisePrincipale = 0,
                    MethodePaiement = methode,
                    ReferenceTransaction = pendingOrder,
                    Statut = false,
                    StatutPaiementMetier = (int)StatutPaiementMetier.EnAttente,
                    IdUtilisateur = dto.Paiement.IdUtilisateur,
                    IdReservation = null,
                    IdSociete = dto.Paiement.IdSociete,
                    IdSite = idSite,
                    DatePaiement = DateTime.UtcNow,
                    DateCreation = DateTime.UtcNow,
                    Origine = origine
                };
                paiement.MettreAJourResteAPaye();

                _context.CommandesReservationEnAttente.Add(commande);
                _context.Paiements.Add(paiement);
                await _context.SaveChangesAsync(cancellationToken);

                commande.IdPaiementEnAttente = paiement.IdPaiement;
                await _context.SaveChangesAsync(cancellationToken);

                var callbackUrl = FlexPayUrlHelper.ResolveCallbackUrl(
                    _httpContextAccessor.HttpContext,
                    _flexPayOptions.CallbackBaseUrl,
                    _flexPayOptions.ForceProductionCallbackInDev);

                var flexResponse = methode == MethodePaiementHelper.CarteBancaire
                    ? await _flexPayService.InitierPaiementCarteV1Async(
                        infoPaiement.CodeMarchand,
                        infoPaiement.ApiToken,
                        reference,
                        montantFlexPay,
                        codeDevisePaiement,
                        $"Réservation aller-retour {dto.IdVoyageAller}/{dto.IdVoyageRetour}",
                        callbackUrl,
                        FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "approve"),
                        FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "cancel"),
                        FlexPayUrlHelper.DeriveRedirectUrl(callbackUrl, "decline"),
                        cancellationToken)
                    : await _flexPayService.InitierPaiementMobileMoneyAsync(
                        infoPaiement.CodeMarchand,
                        infoPaiement.ApiToken,
                        reference,
                        dto.Paiement.Phone!.Trim(),
                        montantFlexPay,
                        codeDevisePaiement,
                        callbackUrl,
                        cancellationToken);

                var orderNumber = string.IsNullOrWhiteSpace(flexResponse.OrderNumber)
                    ? pendingOrder
                    : flexResponse.OrderNumber.Trim();

                var transaction = new TransactionFlexPay
                {
                    IdTransaction = Guid.NewGuid(),
                    OrderNumber = orderNumber,
                    Reference = reference,
                    TypePaiement = methode == MethodePaiementHelper.CarteBancaire ? "2" : "1",
                    Amount = montantFlexPay,
                    Currency = codeDevisePaiement,
                    Phone = dto.Paiement.Phone,
                    StatusFlexPay = flexResponse.IsSuccess ? 2 : 1,
                    CodeFlexPay = flexResponse.Code,
                    MessageFlexPay = flexResponse.Message,
                    StatutPaiement = (int)StatutPaiementMetier.EnAttente,
                    Merchant = infoPaiement.CodeMarchand,
                    CallbackUrl = callbackUrl,
                    PaymentUrl = flexResponse.ResolvePaymentUrl(),
                    IdUtilisateur = dto.Paiement.IdUtilisateur,
                    IdCommandeReservationEnAttente = idCommande,
                    IdPaiement = paiement.IdPaiement,
                    ReponseBruteFlexPay = JsonSerializer.Serialize(flexResponse)
                };

                commande.OrderNumberFlexPay = orderNumber;
                paiement.ReferenceTransaction = orderNumber;
                _context.TransactionsFlexPay.Add(transaction);
                await _context.SaveChangesAsync(cancellationToken);

                if (!flexResponse.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"FlexPay a refusé l'initiation : {flexResponse.Message ?? flexResponse.Code ?? "erreur"}");
                }

                var message = methode == MethodePaiementHelper.CarteBancaire
                    ? "Redirigez le client vers paymentUrl pour finaliser le paiement carte."
                    : "Validez le paiement sur votre téléphone Mobile Money. La réservation aller-retour sera créée après callback.";

                return new ReservationAllerRetourWithPaiementResponseDto
                {
                    TransactionId = orderNumber,
                    Statut = TransactionStatut.EnAttente,
                    Message = message,
                    DateCreation = DateTime.UtcNow,
                    AllerRetour = new ReservationAllerRetourResponseDto
                    {
                        IdVoyageAller = dto.IdVoyageAller,
                        IdVoyageRetour = dto.IdVoyageRetour,
                        Statut = ReservationAllerRetourStatut.EnAttentePaiement,
                        IdSociete = dto.IdSociete > 0 ? dto.IdSociete : voyageAller.IdSociete,
                        IdClient = dto.IdClient,
                        IdUtilisateur = dto.IdUtilisateur,
                        IdSite = idSite,
                        Origine = origine,
                        DateCreation = DateTime.UtcNow,
                        Paiement = PaiementResponseMapper.Map(paiement)
                    },
                    IdCommandeReservationEnAttente = idCommande,
                    OrderNumberFlexPay = orderNumber,
                    ReferenceFlexPay = reference,
                    MontantVoyage = montantAttendu,
                    CodeDeviseVoyage = codeDeviseVoyage,
                    MontantFlexPay = montantFlexPay,
                    CodeDevisePaiement = codeDevisePaiement,
                    TauxApplique = taux,
                    HoldExpireAt = commande.DateExpiration,
                    PaymentUrl = flexResponse.ResolvePaymentUrl(),
                    FlexPayAccepted = true
                };
            }
            catch
            {
                await _siegeDisponibilite.ReleaseHoldsForCommandeAsync(idCommande, cancellationToken);
                throw;
            }
        }

        public async Task<ReservationAllerRetourResponseDto?> GetByIdAsync(
            int idReservationAllerRetour,
            CancellationToken cancellationToken = default)
        {
            var exists = await _context.ReservationsAllerRetour.AsNoTracking()
                .AnyAsync(a => a.IdReservationAllerRetour == idReservationAllerRetour, cancellationToken);
            if (!exists)
                return null;

            var detail = await BuildDetailAsync(idReservationAllerRetour, null, null, cancellationToken);
            EnsureTenantAccess(detail.IdSociete);
            return detail;
        }

        public async Task<ReservationAllerRetourResponseDto> CancelAsync(
            int idReservationAllerRetour,
            CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                IDbContextTransaction? tx = null;
                if (_context.Database.IsRelational())
                    tx = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var agregat = await _context.ReservationsAllerRetour
                        .FirstOrDefaultAsync(a => a.IdReservationAllerRetour == idReservationAllerRetour, cancellationToken)
                        ?? throw new InvalidOperationException($"Aller-retour {idReservationAllerRetour} introuvable.");

                    EnsureTenantAccess(agregat.IdSociete);

                    if (agregat.Statut == ReservationAllerRetourStatut.Annulee)
                        throw new InvalidOperationException("Ce dossier aller-retour est déjà annulé.");

                    var reservationIds = new List<int>();
                    if (agregat.IdReservationAller.HasValue)
                        reservationIds.Add(agregat.IdReservationAller.Value);
                    if (agregat.IdReservationRetour.HasValue)
                        reservationIds.Add(agregat.IdReservationRetour.Value);

                    var reservations = await _context.Reservations
                        .Where(r => reservationIds.Contains(r.IdReservation))
                        .ToListAsync(cancellationToken);

                    foreach (var r in reservations)
                    {
                        if (r.StatutReservation is "CONFIRMEE" or "EN_ATTENTE" or "CONFIRME")
                            r.StatutReservation = "ANNULE";
                        r.Statut = false;
                        r.DateModification = DateTime.UtcNow;
                    }

                    await ReleaseSeatAllocationsForReservationsAsync(reservationIds, cancellationToken);

                    agregat.Statut = ReservationAllerRetourStatut.Annulee;
                    agregat.DateModification = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);

                    if (tx != null)
                        await tx.CommitAsync(cancellationToken);

                    return (await BuildDetailAsync(idReservationAllerRetour, null, null, cancellationToken))!;
                }
                catch
                {
                    if (tx != null)
                        await tx.RollbackAsync(cancellationToken);
                    throw;
                }
                finally
                {
                    if (tx != null)
                        await tx.DisposeAsync();
                }
            });
        }

        /// <summary>
        /// Finalise un callback FlexPay TypeCommande=AllerRetour (appelé depuis FlexPayCallbackService).
        /// </summary>
        public async Task<(int IdReservationAller, int IdPaiement, ReservationAllerRetour Agregat)> FinalizeFlexPaySuccessAsync(
            CommandeReservationEnAttente commande,
            Paiement paiement,
            TransactionFlexPay? transaction,
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken)
        {
            var dto = JsonSerializer.Deserialize<InitiateFlexPayReservationAllerRetourDto>(commande.PayloadMetierJson)
                      ?? throw new InvalidOperationException("Payload métier aller-retour invalide.");

            var (voyageAller, voyageRetour) = await LoadAndValidateVoyagesAsync(
                dto.IdVoyageAller,
                dto.IdVoyageRetour,
                dto.Passagers,
                dto.NombreDePlace,
                dto.IdClient,
                dto.IdSociete,
                dto.IdSite,
                commande.IdSite,
                commande.IdSociete,
                cancellationToken);

            var idUtilisateur = dto.IdUtilisateur > 0 ? dto.IdUtilisateur : commande.IdUtilisateur;
            var idSociete = voyageAller.IdSociete;
            var idSite = dto.IdSite ?? commande.IdSite;

            var agregat = new ReservationAllerRetour
            {
                IdVoyageAller = voyageAller.Id,
                IdVoyageRetour = voyageRetour.Id,
                IdCommandeReservationEnAttente = commande.IdCommandeReservationEnAttente,
                Statut = ReservationAllerRetourStatut.Confirmee,
                IdSociete = idSociete,
                IdClient = dto.IdClient,
                IdUtilisateur = idUtilisateur,
                IdSite = idSite,
                Origine = commande.Origine,
                DateCreation = DateTime.UtcNow
            };
            _context.ReservationsAllerRetour.Add(agregat);
            await _context.SaveChangesAsync(cancellationToken);

            var resAller = await CreateLegReservationAsync(
                voyageAller.Id,
                dto.IdClient,
                idUtilisateur,
                idSociete,
                idSite,
                dto.NombreDePlace,
                commande.Origine,
                agregat.IdReservationAllerRetour,
                ReservationAllerRetourLeg.Aller,
                statutReservation: "CONFIRMEE");

            var resRetour = await CreateLegReservationAsync(
                voyageRetour.Id,
                dto.IdClient,
                idUtilisateur,
                idSociete,
                idSite,
                dto.NombreDePlace,
                commande.Origine,
                agregat.IdReservationAllerRetour,
                ReservationAllerRetourLeg.Retour,
                statutReservation: "CONFIRMEE");

            agregat.IdReservationAller = resAller.IdReservation;
            agregat.IdReservationRetour = resRetour.IdReservation;

            var passengerIdsAller = await CreatePassengersAsync(dto.Passagers, resAller, cancellationToken);
            var passengerIdsRetour = await CreatePassengersAsync(
                AllerRetourVoyageCompatibilityHelper.ClonePassagers(dto.Passagers),
                resRetour,
                cancellationToken);

            await _siegeDisponibilite.ConfirmHoldsAsAllocationsAsync(
                commande.IdCommandeReservationEnAttente,
                voyageAller.Id,
                passengerIdsAller,
                cancellationToken);

            await _siegeDisponibilite.ConfirmHoldsAsAllocationsAsync(
                commande.IdCommandeReservationEnAttente,
                voyageRetour.Id,
                passengerIdsRetour,
                cancellationToken);

            paiement.IdReservation = resAller.IdReservation;
            paiement.IdReservationAllerRetour = agregat.IdReservationAllerRetour;
            paiement.MontantPaye = commande.MontantFlexPay;
            paiement.MontantPayeDevisePrincipale = commande.MontantVoyage;
            paiement.Statut = true;
            paiement.StatutPaiementMetier = (int)StatutPaiementMetier.Reussi;
            paiement.ReferenceTransaction = callback.OrderNumber ?? commande.OrderNumberFlexPay;
            paiement.DatePaiement = DateTime.UtcNow;
            paiement.MettreAJourResteAPaye();

            agregat.IdPaiement = paiement.IdPaiement;
            agregat.DateModification = DateTime.UtcNow;

            if (transaction != null)
            {
                transaction.StatutPaiement = (int)StatutPaiementMetier.Reussi;
                transaction.StatusFlexPay = 0;
                transaction.IdReservation = resAller.IdReservation;
                transaction.IdPaiement = paiement.IdPaiement;
                transaction.ProviderReference = callback.ProviderReference ?? transaction.ProviderReference;
            }

            _context.CommandesReservationEnAttente.Remove(commande);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var billetsAller = await _billetEmissionService.EmitBilletsPourPaiementAsync(paiement);
                if (billetsAller.Count > 0)
                {
                    paiement.DateEmissionBillet = DateTime.UtcNow;
                    paiement.IdBilletEmis = billetsAller[0].IdBillet;
                }

                await _billetEmissionService.EmitBilletsPourReservationAsync(
                    resRetour.IdReservation,
                    paiement.IdSociete,
                    paiement.IdSite);

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Paiement FlexPay AR confirmé mais émission billets échouée — AR {IdAR}",
                    agregat.IdReservationAllerRetour);
            }

            return (resAller.IdReservation, paiement.IdPaiement, agregat);
        }

        private async Task<(Voyage Aller, Voyage Retour)> LoadAndValidateVoyagesAsync(
            int idVoyageAller,
            int idVoyageRetour,
            List<ReservationPassengerInputDto> passagers,
            int nombreDePlace,
            int idClient,
            int idSocieteDto,
            int? idSiteReservation,
            int? idSitePaiement,
            int idSocietePaiement,
            CancellationToken cancellationToken = default)
        {
            AllerRetourVoyageCompatibilityHelper.EnsureSamePassengers(passagers, nombreDePlace);

            var voyageAller = await _context.Voyages
                .Include(v => v.Vehicule)
                .Include(v => v.Destination)
                .FirstOrDefaultAsync(v => v.Id == idVoyageAller, cancellationToken)
                ?? throw new InvalidOperationException($"Voyage aller {idVoyageAller} introuvable.");

            var voyageRetour = await _context.Voyages
                .Include(v => v.Vehicule)
                .Include(v => v.Destination)
                .FirstOrDefaultAsync(v => v.Id == idVoyageRetour, cancellationToken)
                ?? throw new InvalidOperationException($"Voyage retour {idVoyageRetour} introuvable.");

            AllerRetourVoyageCompatibilityHelper.EnsureCompatible(
                voyageAller,
                voyageRetour,
                voyageAller.Destination,
                voyageRetour.Destination);

            var config = await _configSocieteRepository.GetOrCreateAsync(voyageAller.IdSociete, cancellationToken);
            await _configSocieteRepository.EnsureReservationsActivesAsync(voyageAller.IdSociete, cancellationToken);
            ConfigSocieteDefaults.EnsureReservationHorizon(voyageAller, config);
            ConfigSocieteDefaults.EnsureReservationHorizon(voyageRetour, config);

            if (voyageAller.Vehicule == null || voyageRetour.Vehicule == null)
                throw new InvalidOperationException("Véhicule introuvable pour l'un des voyages.");

            var societeOp = voyageAller.IdSociete;
            if (idSocieteDto > 0 && idSocieteDto != societeOp)
                throw new InvalidOperationException(
                    $"La société ({idSocieteDto}) ne correspond pas aux voyages ({societeOp}).");

            if (idSocietePaiement > 0 && idSocietePaiement != societeOp)
                throw new InvalidOperationException(
                    $"La société du paiement ({idSocietePaiement}) ne correspond pas aux voyages ({societeOp}).");

            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, idSiteReservation, societeOp);
            await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, idSitePaiement, societeOp);

            if (idSiteReservation.HasValue && idSitePaiement.HasValue
                && idSiteReservation.Value != idSitePaiement.Value)
            {
                throw new InvalidOperationException(
                    "Les sites réservation et paiement doivent être identiques lorsque les deux sont renseignées.");
            }

            await EnsureCapacityAsync(voyageAller, nombreDePlace, cancellationToken);
            await EnsureCapacityAsync(voyageRetour, nombreDePlace, cancellationToken);

            var idsCategorie = passagers.Select(p => p.IdCategorieSiege).Distinct().ToList();
            var categoriesCount = await _context.CategorieSieges.AsNoTracking()
                .CountAsync(c => idsCategorie.Contains(c.IdCategorieSiege) && c.IdSociete == societeOp && c.Statut, cancellationToken);
            if (categoriesCount != idsCategorie.Count)
                throw new InvalidOperationException(
                    "Une ou plusieurs catégories de siège sont invalides pour la société du voyage.");

            if (!await _context.Clients.AnyAsync(c => c.IdClient == idClient, cancellationToken))
                throw new InvalidOperationException($"Client {idClient} introuvable.");

            return (voyageAller, voyageRetour);
        }

        private async Task EnsureCapacityAsync(Voyage voyage, int demandees, CancellationToken cancellationToken)
        {
            if (demandees > voyage.Vehicule!.NombreSiege)
            {
                throw new InvalidOperationException(
                    $"Le nombre de places demandées ({demandees}) dépasse la capacité du véhicule ({voyage.Vehicule.NombreSiege}) sur le voyage {voyage.Id}.");
            }

            var prises = await _context.VoyageSeatAllocations.CountAsync(a =>
                a.IdVoyage == voyage.Id && a.Statut == "CONFIRME", cancellationToken);
            var disponibles = voyage.Vehicule.NombreSiege - prises;
            if (disponibles < demandees)
            {
                throw new InvalidOperationException(
                    $"Places insuffisantes sur le voyage {voyage.Id} (disponibles: {disponibles}, demandées: {demandees}).");
            }
        }

        private async Task<Reservation> CreateLegReservationAsync(
            int idVoyage,
            int idClient,
            int idUtilisateur,
            int idSociete,
            int? idSite,
            int nombreDePlace,
            string origine,
            int idReservationAllerRetour,
            ReservationAllerRetourLeg leg,
            string statutReservation = "EN_ATTENTE")
        {
            var reservation = new Reservation
            {
                IdVoyage = idVoyage,
                IdClient = idClient,
                IdUtilisateur = idUtilisateur,
                IdSociete = idSociete,
                IdSite = idSite,
                NombreDePlace = nombreDePlace,
                DateReservation = DateTime.UtcNow,
                StatutReservation = statutReservation,
                Statut = true,
                Origine = origine,
                IdReservationAllerRetour = idReservationAllerRetour,
                AllerRetourLeg = leg
            };
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
            return reservation;
        }

        private async Task<IReadOnlyList<int>> CreatePassengersAsync(
            IReadOnlyList<ReservationPassengerInputDto> passagers,
            Reservation reservation,
            CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var added = new List<ReservationPassenger>();
            foreach (var p in passagers)
            {
                var rp = new ReservationPassenger
                {
                    IdReservation = reservation.IdReservation,
                    IdClient = p.IdClient,
                    NomComplet = p.NomComplet.Trim(),
                    Telephone = string.IsNullOrWhiteSpace(p.Telephone) ? null : p.Telephone.Trim(),
                    Email = string.IsNullOrWhiteSpace(p.Email) ? null : p.Email.Trim(),
                    DocumentType = string.IsNullOrWhiteSpace(p.DocumentType) ? null : p.DocumentType.Trim(),
                    DocumentNumero = string.IsNullOrWhiteSpace(p.DocumentNumero) ? null : p.DocumentNumero.Trim(),
                    Genre = string.IsNullOrWhiteSpace(p.Genre) ? null : p.Genre.Trim(),
                    IdSociete = reservation.IdSociete,
                    Statut = true,
                    DateCreation = utcNow
                };
                added.Add(rp);
                _context.ReservationPassengers.Add(rp);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return added.OrderBy(e => e.IdReservationPassenger).Select(e => e.IdReservationPassenger).ToList();
        }

        private async Task ReleaseSeatAllocationsForReservationsAsync(
            IReadOnlyList<int> reservationIds,
            CancellationToken cancellationToken)
        {
            if (reservationIds.Count == 0)
                return;

            var passengerIds = await _context.ReservationPassengers
                .Where(p => reservationIds.Contains(p.IdReservation))
                .Select(p => p.IdReservationPassenger)
                .ToListAsync(cancellationToken);

            if (passengerIds.Count == 0)
                return;

            var allocations = await _context.VoyageSeatAllocations
                .Where(a => passengerIds.Contains(a.IdReservationPassenger))
                .ToListAsync(cancellationToken);

            if (allocations.Count > 0)
                _context.VoyageSeatAllocations.RemoveRange(allocations);
        }

        private async Task<ReservationAllerRetourResponseDto> BuildDetailAsync(
            int id,
            IReadOnlyList<Billet>? billetsAllerHint,
            IReadOnlyList<Billet>? billetsRetourHint,
            CancellationToken cancellationToken = default)
        {
            var agregat = await _context.ReservationsAllerRetour.AsNoTracking()
                .FirstAsync(a => a.IdReservationAllerRetour == id, cancellationToken);

            Reservation? resAller = null;
            Reservation? resRetour = null;
            if (agregat.IdReservationAller.HasValue)
            {
                resAller = await _context.Reservations.AsNoTracking()
                    .Include(r => r.Passagers)
                    .FirstOrDefaultAsync(r => r.IdReservation == agregat.IdReservationAller, cancellationToken);
            }

            if (agregat.IdReservationRetour.HasValue)
            {
                resRetour = await _context.Reservations.AsNoTracking()
                    .Include(r => r.Passagers)
                    .FirstOrDefaultAsync(r => r.IdReservation == agregat.IdReservationRetour, cancellationToken);
            }

            Paiement? paiement = null;
            if (agregat.IdPaiement.HasValue)
            {
                paiement = await _context.Paiements.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPaiement == agregat.IdPaiement, cancellationToken);
            }

            var billetsAller = billetsAllerHint;
            var billetsRetour = billetsRetourHint;

            if (billetsAller == null && agregat.IdReservationAller.HasValue)
            {
                billetsAller = await _context.Billets.AsNoTracking()
                    .Include(b => b.Siege)
                    .Include(b => b.ReservationPassenger)
                    .Include(b => b.Reservation)
                        .ThenInclude(r => r!.Voyage)
                            .ThenInclude(v => v!.VoyageTarifsCategorieSiege)
                    .Where(b => b.IdReservation == agregat.IdReservationAller)
                    .OrderBy(b => b.IdReservationPassenger)
                    .ToListAsync(cancellationToken);
            }

            if (billetsRetour == null && agregat.IdReservationRetour.HasValue)
            {
                billetsRetour = await _context.Billets.AsNoTracking()
                    .Include(b => b.Siege)
                    .Include(b => b.ReservationPassenger)
                    .Include(b => b.Reservation)
                        .ThenInclude(r => r!.Voyage)
                            .ThenInclude(v => v!.VoyageTarifsCategorieSiege)
                    .Where(b => b.IdReservation == agregat.IdReservationRetour)
                    .OrderBy(b => b.IdReservationPassenger)
                    .ToListAsync(cancellationToken);
            }

            var dtoAller = await ToBilletDtosAsync(billetsAller);
            var dtoRetour = await ToBilletDtosAsync(billetsRetour);

            return new ReservationAllerRetourResponseDto
            {
                IdReservationAllerRetour = agregat.IdReservationAllerRetour,
                IdVoyageAller = agregat.IdVoyageAller,
                IdVoyageRetour = agregat.IdVoyageRetour,
                IdReservationAller = agregat.IdReservationAller,
                IdReservationRetour = agregat.IdReservationRetour,
                IdPaiement = agregat.IdPaiement,
                Statut = agregat.Statut,
                IdSociete = agregat.IdSociete,
                IdClient = agregat.IdClient,
                IdUtilisateur = agregat.IdUtilisateur,
                IdSite = agregat.IdSite,
                Origine = agregat.Origine,
                DateCreation = agregat.DateCreation,
                DateModification = agregat.DateModification,
                ReservationAller = resAller == null ? null : MapReservation(resAller),
                ReservationRetour = resRetour == null ? null : MapReservation(resRetour),
                Paiement = paiement == null ? null : PaiementResponseMapper.Map(paiement),
                BilletsAller = dtoAller,
                BilletsRetour = dtoRetour
            };
        }

        private static ReservationResponseDto MapReservation(Reservation r) => new()
        {
            IdReservation = r.IdReservation,
            IdVoyage = r.IdVoyage,
            IdClient = r.IdClient,
            IdUtilisateur = r.IdUtilisateur,
            IdSociete = r.IdSociete,
            IdSite = r.IdSite,
            StatutReservation = r.StatutReservation,
            Statut = r.Statut,
            DateReservation = r.DateReservation,
            DateCreation = r.DateCreation,
            DateModification = r.DateModification,
            Origine = r.Origine,
            IdReservationAllerRetour = r.IdReservationAllerRetour,
            AllerRetourLeg = r.AllerRetourLeg,
            Passagers = r.Passagers?
                .OrderBy(p => p.IdReservationPassenger)
                .Select(p => new ReservationPassengerReadDto
                {
                    IdReservationPassenger = p.IdReservationPassenger,
                    IdReservation = p.IdReservation,
                    IdClient = p.IdClient,
                    NomComplet = p.NomComplet,
                    Telephone = p.Telephone,
                    Email = p.Email,
                    DocumentType = p.DocumentType,
                    DocumentNumero = p.DocumentNumero,
                    Genre = p.Genre,
                    IdSociete = p.IdSociete,
                    Statut = p.Statut
                }).ToList()
        };

        private async Task<List<Models.DTOs.BilletResponseDto>> ToBilletDtosAsync(IReadOnlyList<Billet>? billets)
        {
            if (billets == null || billets.Count == 0)
                return new List<Models.DTOs.BilletResponseDto>();

            var list = billets.ToList();
            var needsLoad = list.Any(b => b.Reservation?.Voyage == null);
            if (needsLoad)
            {
                var ids = list.Select(b => b.IdBillet).ToList();
                list = await _context.Billets.AsNoTracking()
                    .Include(b => b.Siege)
                    .Include(b => b.ReservationPassenger)
                    .Include(b => b.Reservation)
                        .ThenInclude(r => r!.Voyage)
                            .ThenInclude(v => v!.VoyageTarifsCategorieSiege)
                    .Where(b => ids.Contains(b.IdBillet))
                    .OrderBy(b => b.IdReservationPassenger)
                    .ToListAsync();
            }

            var dtos = list.Select(b => new Models.DTOs.BilletResponseDto
            {
                IdBillet = b.IdBillet,
                IsUsed = b.IsUsed,
                QrCode = b.QrCode ?? string.Empty,
                DateGeneration = b.DateGeneration,
                DateValiditeDebut = b.DateValiditeDebut,
                DateValiditeFin = b.DateValiditeFin,
                IdReservation = b.IdReservation,
                IdReservationPassenger = b.IdReservationPassenger,
                IdSiege = b.IdSiege,
                CodeSiege = b.CodeSiege,
                NomPassager = b.ReservationPassenger?.NomComplet,
                IdSociete = b.IdSociete,
                IdSite = b.IdSite,
                DateCreation = b.DateCreation,
                DateModification = b.DateModification
            }).ToList();

            await _billetPricingEnrichment.EnrichPrixVoyageAsync(list, dtos);
            return dtos;
        }

        private static string NormalizeDevise(string? code) =>
            string.IsNullOrWhiteSpace(code) ? "CDF" : code.Trim().ToUpperInvariant();

        private void EnsureTenantAccess(int idSociete)
        {
            TenantGuard.EnsureRouteSocieteMatchesJwt(
                idSociete,
                _currentUserService.SocieteId,
                _currentUserService.IsSuperAdmin);
        }
    }
}
