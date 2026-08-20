using CongoTravel.Models;
using CongoTravel.Services;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Calcule le montant de reversement auto :
    /// <c>MontantPaye × % − FraisPlateforme</c> (frais converti en devise du paiement si besoin).
    /// </summary>
    public static class ReversementMontantCalculator
    {
        public static async Task<ReversementMontantResult?> ComputeAsync(
            decimal montantBrut,
            string? codeDevisePaiement,
            DateTime dateReference,
            int idSociete,
            ConfigSociete config,
            IDeviseMontantConverter deviseMontantConverter,
            ILogger? logger = null,
            int? idPaiementPourLog = null,
            CancellationToken cancellationToken = default)
        {
            if (config.PourcentageReversementSite <= 0)
                return null;

            if (montantBrut <= 0)
                return null;

            var codeDevise = (codeDevisePaiement ?? "CDF").Trim().ToUpperInvariant();
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
                        var conversion = await deviseMontantConverter.ConvertAsync(
                            idSociete,
                            config.FraisPlateforme,
                            deviseFrais,
                            codeDevise,
                            dateReference,
                            cancellationToken);
                        fraisEnDevisePaiement = conversion.MontantCible;
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger?.LogWarning(ex,
                            "Reversement auto ignoré — conversion frais plateforme {DeviseFrais}->{DevisePaiement} impossible (paiement {IdPaiement})",
                            deviseFrais, codeDevise, idPaiementPourLog);
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

            var motifSuffix = idPaiementPourLog.HasValue
                ? $" — paiement #{idPaiementPourLog.Value}"
                : string.Empty;

            var motif = fraisMotif != null
                ? $"Reversement auto {config.PourcentageReversementSite:0.##}% − {fraisMotif} (frais plateforme){motifSuffix}"
                : $"Reversement auto {config.PourcentageReversementSite:0.##}%{motifSuffix}";

            return new ReversementMontantResult
            {
                Montant = montant,
                CodeDevise = codeDevise,
                Motif = motif
            };
        }
    }
}
