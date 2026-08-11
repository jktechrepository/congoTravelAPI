namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueDashboardResponseDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public SiteTouristiqueDashboardSummaryDto Summary { get; set; } = new();

        public SiteTouristiqueDashboardReservationBreakdownDto Reservations { get; set; } = new();

        public List<SiteTouristiqueDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<SiteTouristiqueDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<SiteTouristiqueDashboardTopJourneeDto> Top5JourneesCa { get; set; } = new();

        public List<SiteTouristiqueDashboardRecentReservationDto> ReservationsRecentes { get; set; } = new();

        public List<SiteTouristiqueDashboardRecentPaymentDto> PaiementsRecents { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class SiteTouristiqueDashboardSummaryDto
    {
        public int JourneesPubliees { get; set; }

        public int JourneesActives { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int ReservationsConfirmeesJour { get; set; }

        public int TicketsEmisMois { get; set; }

        public int TicketsUtilisesMois { get; set; }

        public int HoldsEnCours { get; set; }
    }

    public class SiteTouristiqueDashboardReservationBreakdownDto
    {
        public int Hold { get; set; }

        public int Confirmed { get; set; }

        public int Cancelled { get; set; }

        public int Expired { get; set; }
    }

    public class SiteTouristiqueDashboardRevenueByProviderDto
    {
        public string Provider { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class SiteTouristiqueDashboardRevenueByDeviseDto
    {
        public string CodeDevise { get; set; } = "CDF";

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class SiteTouristiqueDashboardTopJourneeDto
    {
        public int Rang { get; set; }

        public int IdSiteTouristiqueJournee { get; set; }

        public string CodeLieu { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public decimal ChiffreAffaires { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int TicketsVendus { get; set; }
    }

    public class SiteTouristiqueDashboardRecentReservationDto
    {
        public int IdSiteTouristiqueReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? NomLieu { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class SiteTouristiqueDashboardRecentPaymentDto
    {
        public int IdSiteTouristiquePayment { get; set; }

        public string ReferencePaiement { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? ReferenceReservation { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class SiteTouristiqueSuperAdminDashboardResponseDto
    {
        public SiteTouristiqueDashboardGlobalSummaryDto Global { get; set; } = new();

        public List<SiteTouristiqueDashboardSocieteSummaryDto> Societes { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class SiteTouristiqueDashboardGlobalSummaryDto
    {
        public int TotalSocietesActives { get; set; }

        public int JourneesPubliees { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int TicketsEmisMois { get; set; }

        public List<SiteTouristiqueDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    public class SiteTouristiqueDashboardSocieteSummaryDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public int JourneesPubliees { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int TicketsEmisMois { get; set; }

        public List<SiteTouristiqueDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    /// <summary>Widget compact injecté dans les dashboards transport (Gérant, Admin, Financier).</summary>
    public class SiteTouristiqueDashboardWidgetDto
    {
        public SiteTouristiqueDashboardSummaryDto Summary { get; set; } = new();

        public List<SiteTouristiqueDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<SiteTouristiqueDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<SiteTouristiqueDashboardTopJourneeDto> TopJourneesCa { get; set; } = new();
    }
}
