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

            if (config.PourcentageReversementSite <= 0)
                return null;

            var montantBrut = paiement.MontantPaye ?? 0m;
            if (montantBrut <= 0)
                return null;

            var codeDevise = (paiement.CodeDevisePaiement ?? "CDF").Trim().ToUpperInvariant();
            if (codeDevise is not ("CDF" or "USD"))
                return null;

            var partPercent = Math.Round(
                montantBrut * (config.PourcentageReversementSite / 100m),
                2,
                MidpointRounding.AwayFromZero);

            if (codeDevise == "CDF")
                partPercent = Math.Round(partPercent, 0, MidpointRounding.AwayFromZero);

            decimal fraisEnDevisePaiement = 0m;
            string? fraisMotif = null;

            if (config.FraisPlateforme > 0)
            {
                var deviseFrais = string.IsNullOrWhiteSpace(config.CodeDeviseFraisPlateforme)
                    ? codeDevise
                    : config.CodeDeviseFraisPlateforme.Trim().ToUpperInvariant();

                if (deviseFrais == codeDevise)
                {
                    fraisEnDevisePaiement = config.FraisPlateforme;
                }
                else
                {
                    try
                    {
                        var conversion = await _deviseMontantConverter.ConvertAsync(
                            paiement.IdSociete,
                            config.FraisPlateforme,
                            deviseFrais,
                            codeDevise,
                            paiement.DatePaiement,
                            cancellationToken);
                        fraisEnDevisePaiement = conversion.MontantCible;
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogWarning(ex,
                            "Reversement auto ignoré — conversion frais plateforme {DeviseFrais}->{DevisePaiement} impossible (paiement {IdPaiement})",
                            deviseFrais, codeDevise, paiement.IdPaiement);
                        return null;
                    }
                }

                if (codeDevise == "CDF")
                    fraisEnDevisePaiement = Math.Round(fraisEnDevisePaiement, 0, MidpointRounding.AwayFromZero);

                fraisMotif = $"{config.FraisPlateforme:0.##} {deviseFrais}";
            }

            var montant = Math.Max(0m, partPercent - fraisEnDevisePaiement);

            if (montant <= 0)
                return null;

            var motif = fraisMotif != null
                ? $"Reversement auto {config.PourcentageReversementSite:0.##}% − {fraisMotif} (frais plateforme) — paiement #{paiement.IdPaiement}"
                : $"Reversement auto {config.PourcentageReversementSite:0.##}% — paiement #{paiement.IdPaiement}";

            return new ReversementMontantResult
            {
                Montant = montant,
                CodeDevise = codeDevise,
                Motif = motif
            };
        }
    }
}
