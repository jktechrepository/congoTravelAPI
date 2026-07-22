using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class ReversementAutomatiqueService : IReversementAutomatiqueService
    {
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IReversementMontantResolver _montantResolver;
        private readonly IReversementSiteService _reversementSiteService;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly ILogger<ReversementAutomatiqueService> _logger;

        public ReversementAutomatiqueService(
            IConfigSocieteRepository configSocieteRepository,
            IReversementMontantResolver montantResolver,
            IReversementSiteService reversementSiteService,
            IOptions<FlexPayOptions> flexPayOptions,
            ILogger<ReversementAutomatiqueService> logger)
        {
            _configSocieteRepository = configSocieteRepository;
            _montantResolver = montantResolver;
            _reversementSiteService = reversementSiteService;
            _flexPayOptions = flexPayOptions.Value;
            _logger = logger;
        }

        public async Task<bool> TryDeclencherApresPaiementElectroniqueAsync(
            Paiement paiement,
            Reservation reservation,
            CancellationToken cancellationToken = default)
        {
            if (!_flexPayOptions.Enabled || !_flexPayOptions.AutoReversementEnabled)
            {
                _logger.LogDebug(
                    "Reversement auto désactivé (FlexPay) — paiement {IdPaiement}",
                    paiement.IdPaiement);
                return false;
            }

            var config = await _configSocieteRepository.GetOrCreateAsync(paiement.IdSociete, cancellationToken);
            if (!config.AutoReversementPaiementElectronique)
            {
                _logger.LogDebug(
                    "Reversement auto désactivé pour société {IdSociete} — paiement {IdPaiement}",
                    paiement.IdSociete, paiement.IdPaiement);
                return false;
            }

            var montant = await _montantResolver.ResolveAsync(paiement, reservation, config, cancellationToken);
            if (montant == null || montant.Montant <= 0)
                return false;

            var idSite = reservation.IdSite ?? paiement.IdSite;
            if (!idSite.HasValue || idSite.Value <= 0)
            {
                _logger.LogWarning(
                    "Reversement auto impossible — IdSite absent (paiement {IdPaiement})",
                    paiement.IdPaiement);
                return false;
            }

            var idUtilisateur = paiement.IdUtilisateur > 0
                ? paiement.IdUtilisateur
                : reservation.IdUtilisateur;

            try
            {
                var result = await _reversementSiteService.InitierPourPaiementAsync(
                    paiement.IdPaiement,
                    reservation.IdReservation,
                    idSite.Value,
                    paiement.IdSociete,
                    idUtilisateur,
                    montant.Montant,
                    montant.CodeDevise,
                    montant.Motif,
                    cancellationToken);

                if (result == null)
                {
                    _logger.LogDebug(
                        "Reversement auto déjà traité (idempotence) — paiement {IdPaiement}",
                        paiement.IdPaiement);
                    return true;
                }

                _logger.LogInformation(
                    "Reversement auto initié — paiement {IdPaiement}, reversement {IdReversementSite}, statut {Statut}",
                    paiement.IdPaiement, result.IdReversementSite, result.Statut);

                return result.Statut != Models.Enums.StatutReversementSite.Echec;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Échec reversement auto après paiement {IdPaiement} — la réservation reste confirmée",
                    paiement.IdPaiement);
                return false;
            }
        }
    }
}
