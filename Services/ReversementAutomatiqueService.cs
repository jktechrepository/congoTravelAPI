using Microsoft.Extensions.Options;
using CongoTravel.Configuration;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class ReversementAutomatiqueService : IReversementAutomatiqueService
    {
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly IReversementSiteService _reversementSiteService;
        private readonly FlexPayOptions _flexPayOptions;
        private readonly ILogger<ReversementAutomatiqueService> _logger;

        public ReversementAutomatiqueService(
            IConfigSocieteRepository configSocieteRepository,
            IDeviseMontantConverter deviseMontantConverter,
            IReversementSiteService reversementSiteService,
            IOptions<FlexPayOptions> flexPayOptions,
            ILogger<ReversementAutomatiqueService> logger)
        {
            _configSocieteRepository = configSocieteRepository;
            _deviseMontantConverter = deviseMontantConverter;
            _reversementSiteService = reversementSiteService;
            _flexPayOptions = flexPayOptions.Value;
            _logger = logger;
        }

        public Task<bool> TryDeclencherApresPaiementElectroniqueAsync(
            Paiement paiement,
            Reservation reservation,
            CancellationToken cancellationToken = default) =>
            TryDeclencherAsync(
                ReversementAutomatiqueContext.FromTransport(paiement, reservation),
                cancellationToken);

        public async Task<bool> TryDeclencherAsync(
            ReversementAutomatiqueContext ctx,
            CancellationToken cancellationToken = default)
        {
            if (!_flexPayOptions.Enabled || !_flexPayOptions.AutoReversementEnabled)
            {
                _logger.LogDebug(
                    "Reversement auto désactivé (FlexPay) — module {Module}, paiementSource {IdPaiementSource}",
                    ctx.ModulePaiement, ctx.IdPaiementSource);
                return false;
            }

            if (!ctx.EstPaiementElectronique)
            {
                _logger.LogDebug(
                    "Reversement auto ignoré — paiement non électronique (module {Module}, paiementSource {IdPaiementSource})",
                    ctx.ModulePaiement, ctx.IdPaiementSource);
                return false;
            }

            var config = await _configSocieteRepository.GetOrCreateAsync(ctx.IdSociete, cancellationToken);
            if (!config.AutoReversementPaiementElectronique)
            {
                _logger.LogDebug(
                    "Reversement auto désactivé pour société {IdSociete} — module {Module}, paiementSource {IdPaiementSource}",
                    ctx.IdSociete, ctx.ModulePaiement, ctx.IdPaiementSource);
                return false;
            }

            var montant = await ReversementMontantCalculator.ComputeAsync(
                ctx.MontantBrut,
                ctx.CodeDevisePaiement,
                ctx.DateReference,
                ctx.IdSociete,
                config,
                _deviseMontantConverter,
                _logger,
                ctx.IdPaiementSource,
                cancellationToken);

            if (montant == null || montant.Montant <= 0)
                return false;

            if (!ctx.IdSite.HasValue || ctx.IdSite.Value <= 0)
            {
                _logger.LogWarning(
                    "Reversement auto impossible — IdSite absent (module {Module}, paiementSource {IdPaiementSource})",
                    ctx.ModulePaiement, ctx.IdPaiementSource);
                return false;
            }

            var idUtilisateur = ctx.IdUtilisateur > 0 ? ctx.IdUtilisateur : 0;

            try
            {
                var result = await _reversementSiteService.InitierPourPaiementAsync(
                    ctx.ModulePaiement,
                    ctx.IdPaiementSource,
                    ctx.IdReservationSource,
                    ctx.IdSite.Value,
                    ctx.IdSociete,
                    idUtilisateur,
                    montant.Montant,
                    montant.CodeDevise,
                    montant.Motif,
                    ctx.IdPaiementTransport,
                    ctx.IdReservationTransport,
                    cancellationToken);

                if (result == null)
                {
                    _logger.LogDebug(
                        "Reversement auto déjà traité ou ignoré (idempotence / site) — module {Module}, paiementSource {IdPaiementSource}",
                        ctx.ModulePaiement, ctx.IdPaiementSource);
                    return true;
                }

                _logger.LogInformation(
                    "Reversement auto initié — module {Module}, paiementSource {IdPaiementSource}, reversement {IdReversementSite}, statut {Statut}",
                    ctx.ModulePaiement, ctx.IdPaiementSource, result.IdReversementSite, result.Statut);

                return result.Statut != Models.Enums.StatutReversementSite.Echec;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Échec reversement auto après paiement — module {Module}, paiementSource {IdPaiementSource} — la réservation reste confirmée",
                    ctx.ModulePaiement, ctx.IdPaiementSource);
                return false;
            }
        }
    }
}
