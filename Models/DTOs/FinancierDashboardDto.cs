namespace CongoTravel.Models.DTOs
{
    public class FinancierDashboardDto
    {
        public GlobalFinancierStatistiquesDto GlobalStatistiques { get; set; } = new();
        public List<SocieteFinancierSummaryDto> SocietesFinancieres { get; set; } = new();
        public List<TransactionRecenteDto> TransactionsRecentes { get; set; } = new();
        public List<AlerteFinanciereDto> AlertesFinancieres { get; set; } = new();
        public TendancesFinancieresDto Tendances { get; set; } = new();
        public List<CollecteOrigineGroupeItemDto> CollecteParOrigineGroupe { get; set; } = new();
        public CollecteOrigineGroupeSyntheseDto CollecteOrigineGroupeSynthese { get; set; } = new();
        /// <summary>Widget billetterie événement (null si permission absente).</summary>
        public Evenement.EvenementDashboardWidgetDto? EvenementStatistiques { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class GlobalFinancierStatistiquesDto
    {
        public decimal ChiffreAffairesMois { get; set; }
        public decimal ChiffreAffairesMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        public decimal MontantReservationsNonPayees { get; set; }
        public decimal TauxPaiementGlobal { get; set; }
        public int NombreTotalTransactions { get; set; }
        public decimal MoyenneTransaction { get; set; }
        public int NombreTotalReservations { get; set; }
        public int NombreTotalVoyages { get; set; }
        public decimal TauxRemplissageMoyen { get; set; }
    }

    public class SocieteFinancierSummaryDto
    {
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public string? VilleSociete { get; set; }
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public decimal ChiffreAffairesMois { get; set; }
        public decimal MontantReservationsNonPayees { get; set; }
        public decimal TauxPaiement { get; set; }
        public int NombreTransactions { get; set; }
        public int NombreReservations { get; set; }
        public int NombreVoyages { get; set; }
        public string StatutFinancier { get; set; } = string.Empty;
        public decimal TauxRemplissageMoyen { get; set; }
        public List<CollecteOrigineGroupeItemDto> CollecteParOrigineGroupe { get; set; } = new();
        public CollecteOrigineGroupeSyntheseDto CollecteOrigineGroupeSynthese { get; set; } = new();
    }

    public class TransactionRecenteDto
    {
        public int IdTransaction { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string NomSociete { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DateTransaction { get; set; }
        public string TypeTransaction { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ReferenceReservation { get; set; } = string.Empty;
        public string VoyageInfo { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DateVoyage { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
    }

    public class AlerteFinanciereDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
        public string TypeAlerteTransport { get; set; } = string.Empty;
        public int NombreReservationsConcernees { get; set; }
        public decimal TauxConcerne { get; set; }
        public string ActionSuggeree { get; set; } = string.Empty;
    }

    public class TendancesFinancieresDto
    {
        public List<TendanceMensuelleDto> RevenusTransport { get; set; } = new();
        public List<TendanceMensuelleDto> Encaissements { get; set; } = new();
        public List<TendanceMensuelleDto> TauxPaiement { get; set; } = new();
        public List<TendanceMensuelleDto> NombreReservations { get; set; } = new();
        public List<TendanceMensuelleDto> NombreVoyages { get; set; } = new();
    }
}
