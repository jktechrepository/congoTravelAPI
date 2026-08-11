namespace CongoTravel.Models.DTOs.ConfigSociete
{
    public class ConfigSocieteResponseDto
    {
        public int IdConfigSociete { get; set; }
        public int IdSociete { get; set; }
        public int DureeValiditeBilletJours { get; set; }
        public decimal PenaliteReaffectationPourcentage { get; set; }
        public int? JoursAvanceMaxReservation { get; set; }
        public int HeuresLimiteReaffectation { get; set; }
        public int HeuresOuvertureEmbarquementAvantDepart { get; set; }
        public int HeuresFermetureEmbarquementApresJourDepart { get; set; }
        public int HeuresOuvertureEntreeEvenementAvantDebut { get; set; }
        public int DureeHoldFlexPayMinutes { get; set; }
        public bool ReaffectationActive { get; set; }
        public bool AutoReversementPaiementElectronique { get; set; }
        public decimal PourcentageReversementSite { get; set; }
        public decimal FraisPlateforme { get; set; }
        public string? CodeDeviseFraisPlateforme { get; set; }
        public decimal MontAddPaieElectronique { get; set; }
        public string? CodeDeviseMontAddPaieElectronique { get; set; }
        /// <summary>Poids de bagage offert (kg) ; 0 = aucun.</summary>
        public decimal PoidsBagageParKiloOffert { get; set; }
        public string? CodeDevisePrincipale { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
