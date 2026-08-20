using CongoTravel.Helpers;
using CongoTravel.Models;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class PaiementElectroniqueReversementMontantResolver : IReversementMontantResolver
    {
        private readonly IDeviseMontantConverter _deviseMontantConverter;
        private readonly ILogger<PaiementElectroniqueReversementMontantResolver> _logger;

        public PaiementElectroniqueReversementMontantResolver(
            IDeviseMontantConverter deviseMontantConverter,
            ILogger<PaiementElectroniqueReversementMontantResolver> logger)
        {
            _deviseMontantConverter = deviseMontantConverter;
            _logger = logger;
        }

        public async Task<ReversementMontantResult?> ResolveAsync(
            Paiement paiement,
            Reservation reservation,
            ConfigSociete config,
            CancellationToken cancellationToken = default)
        {
            if (!MethodePaiementHelper.IsElectronic(paiement.MethodePaiement))
                return null;

            return await ReversementMontantCalculator.ComputeAsync(
                paiement.MontantPaye ?? 0m,
                paiement.CodeDevisePaiement,
                paiement.DatePaiement == default ? DateTime.UtcNow : paiement.DatePaiement,
                paiement.IdSociete,
                config,
                _deviseMontantConverter,
                _logger,
                paiement.IdPaiement,
                cancellationToken);
        }
    }
}
