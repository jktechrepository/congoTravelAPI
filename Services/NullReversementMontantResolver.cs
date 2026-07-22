using CongoTravel.Models;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Stub par défaut — aucune règle de montant tant que la politique métier n'est pas définie.
    /// </summary>
    public class NullReversementMontantResolver : IReversementMontantResolver
    {
        private readonly ILogger<NullReversementMontantResolver> _logger;

        public NullReversementMontantResolver(ILogger<NullReversementMontantResolver> logger)
        {
            _logger = logger;
        }

        public Task<ReversementMontantResult?> ResolveAsync(
            Paiement paiement,
            Reservation reservation,
            ConfigSociete config,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Reversement auto ignoré — règle de montant non configurée (paiement {IdPaiement})",
                paiement.IdPaiement);
            return Task.FromResult<ReversementMontantResult?>(null);
        }
    }
}
