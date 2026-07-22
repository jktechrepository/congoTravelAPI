using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services
{
    /// <summary>
    /// Initiation FlexPay : holds sièges + commande en attente + appel API FlexPay (pas de réservation).
    /// </summary>
    public class FlexPayReservationService : IFlexPayReservationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiegeDisponibiliteService _siegeDisponibilite;
        private readonly IVoyageTarifService _voyageTarifService;
        private readonly IFlexPayService _flexPayService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly IInfoPaiementResolutionService _infoPaiementResolution;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<FlexPayReservationService> _logger;

        public FlexPayReservationService(
            CongoTravelDbContext context,
            ISiegeDisponibiliteService siegeDisponibilite,
            IVoyageTarifService voyageTarifService,
            IFlexPayService flexPayService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<FlexPayOptions> flexPayOptions,
            IInfoPaiementResolutionService infoPaiementResolution,
            IConfigSocieteRepository configSocieteRepository,
            IDeviseMontantConverter deviseMontantConverter,
            ICurrentUserService currentUserService,
            ILogger<FlexPayReservationService> logger)
        {
            _context = context;
            _siegeDisponibilite = siegeDisponibilite;
            _voyageTarifService = voyageTarifService;
            _flexPayService = flexPayService;
            _httpContextAccessor = httpContextAccessor;
            _flexPayOptions = flexPayOptions.Value;
            _infoPaiementResolution = infoPaiementResolution;
            _configSocieteRepository = configSocieteRepository;
            _deviseMontantConverter = deviseMontantConverter;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<ReservationWithPaiementResponseDto> InitiateAsync(
            InitiateFlexPayReservationDto dto,
            CancellationToken cancellationToken = default)
        {
            MethodePaiementHelper.EnsureElectronicOnly(dto.Paiement.MethodePaiement);
            var methode = MethodePaiementHelper.NormalizeForStorage(dto.Paiement.MethodePaiement);

            if (!_flexPayOptions.Enabled)
            {
                throw new InvalidOperationException(
                    "Le paiement électronique FlexPay n'est pas activé sur cet environnement.");
            }

            if (dto.Reservation.Passagers == null || dto.Reservation.Passagers.Count == 0)
                throw new InvalidOperationException("Reservation.Passagers est requis.");

            if (dto.Reservation.Passagers.Count != dto.Reservation.NombreDePlace)
                throw new InvalidOperationException("Le nombre de passagers doit correspondre à nombreDePlace.");

            if (dto.Paiement.MontantAPaye <= 0)
                throw new InvalidOperationException("FlexPay exige un paiement intégral : montantAPaye doit être > 0.");

            var codeDevisePaiement = dto.Paiement.CodeDevisePaiement.Trim().ToUpperInvariant();
            if (codeDevisePaiement is not ("CDF" or "USD"))
                throw new InvalidOperationException("FlexPay n'accepte que CDF ou USD comme devise de paiement.");

            if (methode == MethodePaiementHelper.MobileMoney
                && string.IsNullOrWhiteSpace(dto.Paiement.Phone))
            {
                throw new InvalidOperationException("Le numéro de téléphone est requis pour MOBILE_MONEY.");
            }

            var idSite = dto.Paiement.IdSite ?? dto.Reservation.IdSite
                ?? throw new InvalidOperationException("IdSite requis pour identifier le marchand FlexPay.");

            var infoPaiement = await _infoPaiementResolution.ResolveActiveForSiteAsync(
                idSite, dto.Paiement.IdSociete, cancellationToken);

            if (methode == MethodePaiementHelper.MobileMoney && !infoPaiement.ActifMobileMoney)
                throw new InvalidOperationException("Mobile Money désactivé pour ce site.");

            if (methode == MethodePaiementHelper.CarteBancaire && !infoPaiement.ActifCarteBancaire)
                throw new InvalidOperationException("Carte bancaire désactivée pour ce site.");

            var voyage = await _context.Voyages.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == dto.Reservation.IdVoyage, cancellationToken)
                ?? throw new InvalidOperationException($"Voyage {dto.Reservation.IdVoyage} introuvable.");

            var config = await _configSocieteRepository.GetOrCreateAsync(voyage.IdSociete, cancellationToken);
            ConfigSocieteDefaults.EnsureReservationHorizon(voyage, config);

            var codeDeviseVoyage = string.IsNullOrWhiteSpace(voyage.CodeDevisePrix)
                ? "CDF"
                : voyage.CodeDevisePrix.Trim().ToUpperInvariant();

            var categories = dto.Reservation.Passagers.Select(p => p.IdCategorieSiege).ToList();
            var idCommande = Guid.NewGuid();
            var holdMinutes = config.DureeHoldFlexPayMinutes > 0
                ? config.DureeHoldFlexPayMinutes
                : (_flexPayOptions.SeatHoldMinutes > 0 ? _flexPayOptions.SeatHoldMinutes : 15);

            var heldSiegeIds = await _siegeDisponibilite.CreateHoldsForCategoriesAsync(
                dto.Reservation.IdVoyage,
                idCommande,
                categories,
                holdMinutes,
                cancellationToken);

            try
            {
                var montantBillets = await _voyageTarifService.ComputeTotalForSiegesAsync(
                    voyage.Id,
                    heldSiegeIds,
                    voyage.Prix);

                var supplement = await ElectronicPaymentSupplementHelper.ComputeSupplementInVoyageCurrencyAsync(
                    config,
                    dto.Reservation.NombreDePlace,
                    codeDeviseVoyage,
                    dto.Paiement.IdSociete,
                    _deviseMontantConverter,
                    DateTime.UtcNow,
                    cancellationToken);

                var montantAttendu = montantBillets + supplement;

                const decimal tolerance = 0.05m;
                if (Math.Abs(dto.Paiement.MontantAPaye - montantAttendu) > tolerance)
                {
                    throw new InvalidOperationException(
                        $"Montant à payer incohérent : attendu {montantAttendu} {codeDeviseVoyage} " +
                        $"(billets {montantBillets} + supplément électronique {supplement}), reçu {dto.Paiement.MontantAPaye}.");
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
                        $"Réservation voyage {dto.Reservation.IdVoyage}",
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

                _logger.LogInformation(
                    "FlexPay initiation OK — Commande={CommandeId}, Order={OrderNumber}, Voyage={VoyageId}",
                    idCommande, orderNumber, dto.Reservation.IdVoyage);

                var initiationMessage = methode == MethodePaiementHelper.CarteBancaire
                    ? "Redirigez le client vers paymentUrl pour finaliser le paiement carte."
                    : "Validez le paiement sur votre téléphone Mobile Money. La réservation sera créée après callback.";

                return FlexPayReservationResponseMapper.MapInitiation(
                    dto,
                    paiement,
                    commande,
                    orderNumber,
                    flexResponse.ResolvePaymentUrl(),
                    flexPayAccepted: true,
                    initiationMessage);
            }
            catch
            {
                await _siegeDisponibilite.ReleaseHoldsForCommandeAsync(idCommande, cancellationToken);
                throw;
            }
        }
    }
}
