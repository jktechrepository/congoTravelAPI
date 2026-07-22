namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementDashboardResponseDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public EvenementDashboardSummaryDto Summary { get; set; } = new();

        public EvenementDashboardReservationBreakdownDto Reservations { get; set; } = new();

        public List<EvenementDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<EvenementDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<EvenementDashboardTopSessionDto> Top5SessionsCa { get; set; } = new();

        public List<EvenementDashboardRecentReservationDto> ReservationsRecentes { get; set; } = new();

        public List<EvenementDashboardRecentPaymentDto> PaiementsRecents { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class EvenementDashboardSummaryDto
    {
        public int SessionsPubliees { get; set; }

        public int SessionsActives { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int ReservationsConfirmeesJour { get; set; }

        public int TicketsEmisMois { get; set; }

        public int TicketsUtilisesMois { get; set; }

        public int HoldsEnCours { get; set; }
    }

    public class EvenementDashboardReservationBreakdownDto
    {
        public int Hold { get; set; }

        public int Confirmed { get; set; }

        public int Cancelled { get; set; }

        public int Expired { get; set; }
    }

    public class EvenementDashboardRevenueByProviderDto
    {
        public string Provider { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class EvenementDashboardRevenueByDeviseDto
    {
        public string CodeDevise { get; set; } = "CDF";

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class EvenementDashboardTopSessionDto
    {
        public int Rang { get; set; }

        public int IdEvenementSession { get; set; }

        public string CodeSession { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public decimal ChiffreAffaires { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int TicketsVendus { get; set; }
    }

    public class EvenementDashboardRecentReservationDto
    {
        public int IdEvenementReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? SessionLibelle { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class EvenementDashboardRecentPaymentDto
    {
        public int IdEvenementPayment { get; set; }

        public string ReferencePaiement { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? ReferenceReservation { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class EvenementSuperAdminDashboardResponseDto
    {
        public EvenementDashboardGlobalSummaryDto Global { get; set; } = new();

        public List<EvenementDashboardSocieteSummaryDto> Societes { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class EvenementDashboardGlobalSummaryDto
    {
        public int TotalSocietesActives { get; set; }

        public int SessionsPubliees { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int TicketsEmisMois { get; set; }

        public List<EvenementDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    public class EvenementDashboardSocieteSummaryDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public int SessionsPubliees { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int TicketsEmisMois { get; set; }

        public List<EvenementDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    /// <summary>Widget compact injecté dans les dashboards transport (Gérant, Admin, Financier).</summary>
    public class EvenementDashboardWidgetDto
    {
        public EvenementDashboardSummaryDto Summary { get; set; } = new();

        public List<EvenementDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<EvenementDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<EvenementDashboardTopSessionDto> TopSessionsCa { get; set; } = new();
    }
}
