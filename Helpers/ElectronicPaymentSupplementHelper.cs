using CongoTravel.Models;
using CongoTravel.Services;

namespace CongoTravel.Helpers
{
    public static class ElectronicPaymentSupplementHelper
    {
        public static async Task<decimal> ComputeSupplementInVoyageCurrencyAsync(
            ConfigSociete config,
            int nombreDePlace,
            string codeDeviseVoyage,
            int idSociete,
            IDeviseMontantConverter converter,
            DateTime dateRef,
            CancellationToken cancellationToken = default)
        {
            if (config.MontAddPaieElectronique <= 0 || nombreDePlace <= 0)
                return 0m;

            codeDeviseVoyage = codeDeviseVoyage.Trim().ToUpperInvariant();
            var supplementBrut = config.MontAddPaieElectronique * nombreDePlace;

            var deviseSupp = string.IsNullOrWhiteSpace(config.CodeDeviseMontAddPaieElectronique)
                ? codeDeviseVoyage
                : config.CodeDeviseMontAddPaieElectronique.Trim().ToUpperInvariant();

            decimal supplementEnDeviseVoyage;
            if (deviseSupp == codeDeviseVoyage)
            {
                supplementEnDeviseVoyage = supplementBrut;
            }
            else
            {
                var conversion = await converter.ConvertAsync(
                    idSociete,
                    supplementBrut,
                    deviseSupp,
                    codeDeviseVoyage,
                    dateRef,
                    cancellationToken);
                supplementEnDeviseVoyage = conversion.MontantCible;
            }

            if (codeDeviseVoyage == "CDF")
                supplementEnDeviseVoyage = Math.Round(supplementEnDeviseVoyage, 0, MidpointRounding.AwayFromZero);

            return supplementEnDeviseVoyage;
        }
    }
}
