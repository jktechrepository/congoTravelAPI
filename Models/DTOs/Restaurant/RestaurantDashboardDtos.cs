namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantDashboardResponseDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public RestaurantDashboardSummaryDto Summary { get; set; } = new();

        public RestaurantDashboardReservationBreakdownDto Reservations { get; set; } = new();

        public List<RestaurantDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<RestaurantDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<RestaurantDashboardTopCreneauDto> Top5CreneauxCa { get; set; } = new();

        public List<RestaurantDashboardRecentReservationDto> ReservationsRecentes { get; set; } = new();

        public List<RestaurantDashboardRecentPaymentDto> PaiementsRecents { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class RestaurantDashboardSummaryDto
    {
        public int EtablissementsPublies { get; set; }

        public int CreneauxPublies { get; set; }

        /// <summary>Créneaux Published dont DateService est dans le mois, ou qui chevauchent la période UTC.</summary>
        public int CreneauxActifs { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public int ReservationsConfirmeesJour { get; set; }

        public decimal MontantAcomptesSuccesMois { get; set; }

        public int HoldsEnCours { get; set; }
    }

    public class RestaurantDashboardReservationBreakdownDto
    {
        public int Hold { get; set; }

        public int Confirmed { get; set; }

        public int Cancelled { get; set; }

        public int Expired { get; set; }
    }

    public class RestaurantDashboardRevenueByProviderDto
    {
        public string Provider { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class RestaurantDashboardRevenueByDeviseDto
    {
        public string CodeDevise { get; set; } = "CDF";

        public decimal Montant { get; set; }

        public int NombrePaiements { get; set; }
    }

    public class RestaurantDashboardTopCreneauDto
    {
        public int Rang { get; set; }

        public int IdRestaurantCreneau { get; set; }

        public string NomRestaurant { get; set; } = string.Empty;

        public DateOnly DateService { get; set; }

        public DateTime StartAtUtc { get; set; }

        public decimal ChiffreAffaires { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int CouvertsConfirmes { get; set; }
    }

    public class RestaurantDashboardRecentReservationDto
    {
        public int IdRestaurantReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? NomRestaurant { get; set; }

        public int NombreCouverts { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class RestaurantDashboardRecentPaymentDto
    {
        public int IdRestaurantPayment { get; set; }

        public string ReferencePaiement { get; set; } = string.Empty;

        public string Provider { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Montant { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public string? ReferenceReservation { get; set; }

        public DateTime DateCreation { get; set; }
    }

    public class RestaurantSuperAdminDashboardResponseDto
    {
        public RestaurantDashboardGlobalSummaryDto Global { get; set; } = new();

        public List<RestaurantDashboardSocieteSummaryDto> Societes { get; set; } = new();

        public DateTime PeriodeDebutUtc { get; set; }

        public DateTime PeriodeFinUtc { get; set; }

        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class RestaurantDashboardGlobalSummaryDto
    {
        public int TotalSocietesActives { get; set; }

        public int EtablissementsPublies { get; set; }

        public int CreneauxPublies { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public decimal MontantAcomptes { get; set; }

        public List<RestaurantDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    public class RestaurantDashboardSocieteSummaryDto
    {
        public int IdSociete { get; set; }

        public string NomSociete { get; set; } = string.Empty;

        public int EtablissementsPublies { get; set; }

        public int CreneauxPublies { get; set; }

        public int ReservationsConfirmeesMois { get; set; }

        public decimal MontantAcomptes { get; set; }

        public List<RestaurantDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    /// <summary>Widget compact injecté dans les dashboards transport (Gérant, Admin, Financier).</summary>
    public class RestaurantDashboardWidgetDto
    {
        public RestaurantDashboardSummaryDto Summary { get; set; } = new();

        public List<RestaurantDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();

        public List<RestaurantDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();

        public List<RestaurantDashboardTopCreneauDto> TopCreneauxCa { get; set; } = new();
    }
}
