using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.ConfigSociete
{
    public class ConfigSocieteUpdateDto
    {
        [Range(0, 365)]
        public int DureeValiditeBilletJours { get; set; }

        [Range(0, 100)]
        public decimal PenaliteReaffectationPourcentage { get; set; }

        [Range(1, 730)]
        public int? JoursAvanceMaxReservation { get; set; }

        [Range(0, 72)]
        public int HeuresLimiteReaffectation { get; set; }

        [Range(0, 72)]
        public int HeuresOuvertureEmbarquementAvantDepart { get; set; }

        [Range(1, 168)]
        public int HeuresFermetureEmbarquementApresJourDepart { get; set; }

        /// <summary>Heures avant le début session pour ouvrir l'entrée événement (0–72).</summary>
        [Range(0, 72)]
        public int HeuresOuvertureEntreeEvenementAvantDebut { get; set; }

        /// <summary>Heures avant le début créneau pour ouvrir l'entrée restaurant (0–72).</summary>
        [Range(0, 72)]
        public int HeuresOuvertureEntreeRestaurantAvantDebut { get; set; }

        [Range(1, 120)]
        public int DureeHoldFlexPayMinutes { get; set; }

        public bool ReaffectationActive { get; set; }

        public bool ReservationIsActif { get; set; } = true;

        public bool ActiviteTransport { get; set; } = true;
        public bool ActiviteEvenement { get; set; } = true;
        public bool ActiviteSiteTouristique { get; set; } = true;
        public bool ActiviteRestaurant { get; set; } = true;
        public bool ActiviteHotel { get; set; } = true;

        public bool AutoReversementPaiementElectronique { get; set; }

        [Range(0, 100)]
        public decimal PourcentageReversementSite { get; set; } = 100m;

        [Range(0, double.MaxValue)]
        public decimal FraisPlateforme { get; set; }

        [MaxLength(3)]
        public string? CodeDeviseFraisPlateforme { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MontAddPaieElectronique { get; set; }

        [MaxLength(3)]
        public string? CodeDeviseMontAddPaieElectronique { get; set; }

        /// <summary>Poids de bagage offert (kg) ; 0 = aucun.</summary>
        [Range(0, double.MaxValue)]
        public decimal PoidsBagageParKiloOffert { get; set; }
    }
}