using CongoTravel.Models;

namespace CongoTravel.Helpers
{
    public static class ConfigSocieteDefaults
    {
        public const int DureeValiditeBilletJours = 0;
        public const decimal PenaliteReaffectationPourcentage = 0m;
        public const int JoursAvanceMaxReservationDefault = 60;
        public const int HeuresLimiteReaffectation = 2;
        public const int HeuresOuvertureEmbarquementAvantDepart = 3;
        public const int HeuresFermetureEmbarquementApresJourDepart = 24;
        public const int DureeHoldFlexPayMinutes = 15;
        public const int DureeHoldEvenementMinutes = 15;

        public static ConfigSociete CreateForSociete(int idSociete) => new()
        {
            IdSociete = idSociete,
            DureeValiditeBilletJours = DureeValiditeBilletJours,
            PenaliteReaffectationPourcentage = PenaliteReaffectationPourcentage,
            JoursAvanceMaxReservation = JoursAvanceMaxReservationDefault,
            HeuresLimiteReaffectation = HeuresLimiteReaffectation,
            HeuresOuvertureEmbarquementAvantDepart = HeuresOuvertureEmbarquementAvantDepart,
            HeuresFermetureEmbarquementApresJourDepart = HeuresFermetureEmbarquementApresJourDepart,
            DureeHoldFlexPayMinutes = DureeHoldFlexPayMinutes,
            DureeHoldEvenementMinutes = DureeHoldEvenementMinutes,
            ReaffectationActive = true,
            PourcentageReversementSite = 100m,
            DateCreation = DateTime.UtcNow
        };

        public static void Normalize(ConfigSociete config)
        {
            config.DureeValiditeBilletJours = Math.Max(0, config.DureeValiditeBilletJours);
            config.PenaliteReaffectationPourcentage = Math.Clamp(config.PenaliteReaffectationPourcentage, 0m, 100m);
            config.PourcentageReversementSite = Math.Clamp(config.PourcentageReversementSite, 0m, 100m);
            config.HeuresLimiteReaffectation = Math.Clamp(config.HeuresLimiteReaffectation, 0, 72);
            config.HeuresOuvertureEmbarquementAvantDepart = Math.Clamp(config.HeuresOuvertureEmbarquementAvantDepart, 0, 72);
            config.HeuresFermetureEmbarquementApresJourDepart = Math.Clamp(config.HeuresFermetureEmbarquementApresJourDepart, 1, 168);
            config.DureeHoldFlexPayMinutes = Math.Clamp(config.DureeHoldFlexPayMinutes, 1, 120);
            config.DureeHoldEvenementMinutes = Math.Clamp(config.DureeHoldEvenementMinutes, 1, 120);
            if (config.JoursAvanceMaxReservation.HasValue)
                config.JoursAvanceMaxReservation = Math.Clamp(config.JoursAvanceMaxReservation.Value, 1, 730);

            config.FraisPlateforme = Math.Max(0m, config.FraisPlateforme);
            if (string.IsNullOrWhiteSpace(config.CodeDeviseFraisPlateforme))
                config.CodeDeviseFraisPlateforme = null;
            else
            {
                config.CodeDeviseFraisPlateforme = config.CodeDeviseFraisPlateforme.Trim().ToUpperInvariant();
                if (config.FraisPlateforme > 0
                    && config.CodeDeviseFraisPlateforme is not ("CDF" or "USD"))
                {
                    throw new InvalidOperationException(
                        "CodeDeviseFraisPlateforme invalide. Valeurs acceptées : CDF, USD, ou null (devise du paiement).");
                }
            }

            config.MontAddPaieElectronique = Math.Max(0m, config.MontAddPaieElectronique);
            if (string.IsNullOrWhiteSpace(config.CodeDeviseMontAddPaieElectronique))
                config.CodeDeviseMontAddPaieElectronique = null;
            else
            {
                config.CodeDeviseMontAddPaieElectronique = config.CodeDeviseMontAddPaieElectronique.Trim().ToUpperInvariant();
                if (config.MontAddPaieElectronique > 0
                    && config.CodeDeviseMontAddPaieElectronique is not ("CDF" or "USD"))
                {
                    throw new InvalidOperationException(
                        "CodeDeviseMontAddPaieElectronique invalide. Valeurs acceptées : CDF, USD, ou null (devise du voyage).");
                }
            }

            config.PoidsBagageParKiloOffert = Math.Max(0m, config.PoidsBagageParKiloOffert);
        }

        public static void EnsureReservationHorizon(Voyage voyage, ConfigSociete config)
        {
            if (!config.JoursAvanceMaxReservation.HasValue)
                return;

            var maxDate = DateTime.UtcNow.Date.AddDays(config.JoursAvanceMaxReservation.Value);
            if (voyage.DateDepart.Date > maxDate)
            {
                throw new InvalidOperationException(
                    $"La date de départ du voyage ({voyage.DateDepart:dd/MM/yyyy}) dépasse l'horizon de réservation autorisé ({config.JoursAvanceMaxReservation} jours).");
            }
        }
    }
}
