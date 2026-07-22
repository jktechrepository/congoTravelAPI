using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Models.DTOs
{
    /// <summary>Dashboard Super-Admin — vue globale multi-sociétés (transport).</summary>
    public class SuperAdminDashboardTransportDto
    {
        public SuperAdminGlobalStatistiquesTransportDto GlobalStatistiques { get; set; } = new();
        public List<SocieteTransportSummaryDto> Societes { get; set; } = new();
        public List<SuperAdminTopSocieteCaDto> Top5SocietesCa { get; set; } = new();
        public List<TransactionRecenteDto> TransactionsRecentes { get; set; } = new();
        public PagedResult<ReservationResponseDto> Reservations { get; set; } = null!;
        public List<CollecteOrigineGroupeItemDto> CollecteParOrigineGroupe { get; set; } = new();
        public CollecteOrigineGroupeSyntheseDto CollecteOrigineGroupeSynthese { get; set; } = new();
        /// <summary>Synthèse billetterie événement multi-sociétés.</summary>
        public EvenementDashboardGlobalSummaryDto? EvenementStatistiques { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class SuperAdminGlobalStatistiquesTransportDto
    {
        public int TotalSocietes { get; set; }
        public int SocietesActives { get; set; }
        public int TotalClient { get; set; }
        public int TotalClientActif { get; set; }
        public int TotalReservation { get; set; }
        public int TotalVoyagesActifs { get; set; }
        public int VoyagesAujourdhui { get; set; }
        public int VoyagesSemaine { get; set; }
        public int TotalReservationsConfirmeesMois { get; set; }
        public int TotalReservationsConfirmeesJour { get; set; }
        public int TotalBilletsEmisMois { get; set; }
        public decimal ChiffreAffairesMois { get; set; }
        public int NombreTransactionsMois { get; set; }
    }

    public class SocieteTransportSummaryDto
    {
        public int IdSociete { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? Ville { get; set; }
        public bool Statut { get; set; }
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public int VoyagesMois { get; set; }
        public int ReservationsConfirmeesMois { get; set; }
        public int BilletsEmisMois { get; set; }
        public decimal ChiffreAffairesMois { get; set; }
        public DateTime? DerniereActivite { get; set; }
    }

    public class SuperAdminTopSocieteCaDto
    {
        public int Rang { get; set; }
        public int IdSociete { get; set; }
        public string Nom { get; set; } = string.Empty;
        public decimal ChiffreAffairesMois { get; set; }
        public string CodeDevisePrincipale { get; set; } = "CDF";
    }
}
