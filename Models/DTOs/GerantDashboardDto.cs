namespace CongoTravel.Models.DTOs
{
    public class GerantDashboardDto
    {
        public SocieteStatistiquesDto SocieteStatistiques { get; set; } = new();
        public ClientsStatistiquesDto ClientsStatistiques { get; set; } = new();
        public DashboardTransportStatistiquesDto TransportStatistiques { get; set; } = new();
        public List<TopClientDto> Top5ClientsCA { get; set; } = new();
        public List<TopClientDto> Top5ClientsNonPayes { get; set; } = new();
        public List<AlerteSocieteDto> AlertesSociete { get; set; } = new();
        public TendancesDto Tendances { get; set; } = new();
        public PaiementsStatistiquesDto PaiementsStatistiques { get; set; } = new();
        public List<CollecteOrigineGroupeItemDto> CollecteParOrigineGroupe { get; set; } = new();
        public CollecteOrigineGroupeSyntheseDto CollecteOrigineGroupeSynthese { get; set; } = new();
        /// <summary>Widget billetterie événement (null si permission absente).</summary>
        public Evenement.EvenementDashboardWidgetDto? EvenementStatistiques { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class SocieteStatistiquesDto
    {
        public string NomSociete { get; set; } = string.Empty;
        public string? VilleSociete { get; set; }
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public int TotalClients { get; set; }
        public int ClientsActifs { get; set; }
        public decimal ChiffreAffairesMois { get; set; }
        public decimal ChiffreAffairesMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        public decimal MontantReservationsNonPayees { get; set; }
        public decimal TauxPaiement { get; set; }
    }

    public class ClientsStatistiquesDto
    {
        public int TotalClients { get; set; }
        public int ClientsActifs { get; set; }
        public int NouveauxClientsMois { get; set; }
        public int ClientsAvecReservationsNonPayees { get; set; }
    }

    public class TopClientDto
    {
        public int Rang { get; set; }
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public decimal Valeur { get; set; }
        public decimal VariationMoisPrecedent { get; set; }
    }

    public class AlerteSocieteDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public string Statut { get; set; } = "Non lue";
        public int? IdClient { get; set; }
        public string? NomClient { get; set; }
    }

    public class PaiementsStatistiquesDto
    {
        public decimal PaiementsJour { get; set; }
        public decimal PaiementsSemaine { get; set; }
        public decimal PaiementsMois { get; set; }
        public int NombrePaiementsJour { get; set; }
        public int NombrePaiementsSemaine { get; set; }
        public int NombrePaiementsMois { get; set; }
        public decimal MoyennePaiementsJournaliers { get; set; }
    }
}
