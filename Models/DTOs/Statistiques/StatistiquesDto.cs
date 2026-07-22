using CongoTravel.Models.DTOs;

namespace CongoTravel.Models.DTOs.Statistiques
{
    /// <summary>Statistiques transport consolidées (société).</summary>
    public class StatistiquesTransportDto
    {
        public StatistiquesGeneralesDto Generales { get; set; } = new();
        public StatistiquesFinancieresDto Financieres { get; set; } = new();
        public StatistiquesOperationnellesDto Operationnelles { get; set; } = new();
        public StatistiquesPerformanceDto Performance { get; set; } = new();
        public PeriodeStatistiquesDto Periode { get; set; } = new();
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class StatistiquesGeneralesDto
    {
        public int TotalClients { get; set; }
        public int TotalReservations { get; set; }
        public int TotalVoyages { get; set; }
        public int TotalBillets { get; set; }
        public decimal TotalPaiements { get; set; }
        public decimal MontantReservationsNonPayees { get; set; }
        public decimal TauxPaiement { get; set; }
        public int TotalPaiementsCount { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class StatistiquesFinancieresDto
    {
        public decimal ChiffreAffaires { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal MontantDu { get; set; }
        public List<EvolutionMensuelleDto> EvolutionMensuelle { get; set; } = new();
        public List<RepartitionPaiementDto> RepartitionPaiements { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class StatistiquesOperationnellesDto
    {
        public List<RepartitionParDestinationDto> RepartitionParDestination { get; set; } = new();
        public List<RepartitionParTypeVehiculeDto> RepartitionParTypeVehicule { get; set; } = new();
        public List<StatistiqueVoyageMoisDto> StatistiquesVoyagesMois { get; set; } = new();
        public ClientActiviteDto ClientActivite { get; set; } = new();
        public DashboardTransportStatistiquesDto TransportStatistiques { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class StatistiquesPerformanceDto
    {
        public decimal TauxPaiementGlobal { get; set; }
        public List<TopAgentDto> TopAgents { get; set; } = new();
        public List<PerformanceMensuelleDto> PerformanceMensuelle { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class EvolutionMensuelleDto
    {
        public string Mois { get; set; } = string.Empty;
        public decimal ChiffreAffaires { get; set; }
        public int NombrePaiements { get; set; }
        public int NombreReservations { get; set; }
    }

    public class RepartitionPaiementDto
    {
        public string MethodePaiement { get; set; } = string.Empty;
        public decimal MontantTotal { get; set; }
        public int NombrePaiements { get; set; }
        public decimal Pourcentage { get; set; }
    }

    public class RepartitionParDestinationDto
    {
        public string Destination { get; set; } = string.Empty;
        public int NombreReservations { get; set; }
        public decimal MontantTotal { get; set; }
        public decimal Pourcentage { get; set; }
    }

    public class RepartitionParTypeVehiculeDto
    {
        public string TypeVehicule { get; set; } = string.Empty;
        public int NombreReservations { get; set; }
        public decimal MontantTotal { get; set; }
        public decimal Pourcentage { get; set; }
    }

    public class StatistiqueVoyageMoisDto
    {
        public string Mois { get; set; } = string.Empty;
        public int NombreVoyages { get; set; }
        public int NombreBillets { get; set; }
        public decimal MontantTotal { get; set; }
    }

    public class ClientActiviteDto
    {
        public int NombreClientsActifs { get; set; }
        public int NombreClientsInactifs { get; set; }
        public int TotalClients { get; set; }
        public decimal PourcentageActifs { get; set; }
        public decimal PourcentageInactifs { get; set; }
    }

    public class TopAgentDto
    {
        public int IdAgent { get; set; }
        public string NomAgent { get; set; } = string.Empty;
        public string? Matricule { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
    }

    public class PerformanceMensuelleDto
    {
        public string Mois { get; set; } = string.Empty;
        public decimal TauxPaiement { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TicketMoyen { get; set; }
    }

    public class PeriodeStatistiquesDto
    {
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string LibellePeriode { get; set; } = string.Empty;
    }
}
