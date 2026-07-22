using CongoTravel.Models.Enums;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Dashboard Admin société — statistiques transport et collecte.
    /// </summary>
    public class DashboardDto
    {
        public string CodeDevisePrincipale { get; set; } = "CDF";

        /// <summary>Total des agents actifs de la société.</summary>
        public int TotalAgents { get; set; }

        /// <summary>Clients distincts avec au moins une réservation active.</summary>
        public int TotalClientsActifs { get; set; }

        public DashboardTransportStatistiquesDto TransportStatistiques { get; set; } = new();

        public CollecteMoisDto CollecteMois { get; set; } = new();

        /// <summary>Collecte du mois courant ventilée CLIENT vs AGENT vs INCONNU.</summary>
        public List<CollecteOrigineGroupeItemDto> CollecteParOrigineGroupe { get; set; } = new();

        /// <summary>KPIs agrégés part digital vs guichet (INCONNU exclu du dénominateur KPI).</summary>
        public CollecteOrigineGroupeSyntheseDto CollecteOrigineGroupeSynthese { get; set; } = new();

        public List<TopAgentCollecteurDto> Top5AgentsCollecteurs { get; set; } = new();

        /// <summary>Widget billetterie événement (null si permission absente).</summary>
        public Evenement.EvenementDashboardWidgetDto? EvenementStatistiques { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class DashboardTransportStatistiquesDto
    {
        public int VoyagesActifs { get; set; }
        public int VoyagesAujourdhui { get; set; }
        public int VoyagesSemaine { get; set; }
        public int VoyagesMois { get; set; }
        public int ReservationsConfirmeesMois { get; set; }
        public int ReservationsConfirmeesJour { get; set; }
        public int BilletsEmisMois { get; set; }
    }

    public class TopAgentCollecteurDto
    {
        public int IdAgent { get; set; }
        public string? Matricule { get; set; }
        public string? NomComplet { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
    }

    public class CollecteMoisDto
    {
        public string MoisLabel { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public decimal MontantMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TicketMoyen { get; set; }
        public decimal VariationTicketMoyen { get; set; }
    }

    public class CollecteOrigineGroupeItemDto
    {
        public string OrigineGroupe { get; set; } = OrigineOperationGroupe.INCONNU;
        public decimal Montant { get; set; }
        public int NombrePaiements { get; set; }
        public decimal MontantMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        /// <summary>Part du total mois (CLIENT + AGENT + INCONNU).</summary>
        public decimal PartPourcentage { get; set; }
    }

    public class CollecteOrigineGroupeSyntheseDto
    {
        /// <summary>CLIENT / (CLIENT + AGENT).</summary>
        public decimal PartDigitalPourcentage { get; set; }
        /// <summary>AGENT / (CLIENT + AGENT).</summary>
        public decimal PartGuichetPourcentage { get; set; }
        public decimal MontantClassifie { get; set; }
        public decimal MontantNonClassifie { get; set; }
    }
}
