namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelDashboardResponseDto
    {
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public HotelDashboardSummaryDto Summary { get; set; } = new();
        public HotelDashboardReservationBreakdownDto Reservations { get; set; } = new();
        public List<HotelDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();
        public List<HotelDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
        public List<HotelDashboardTopHotelDto> Top5HotelsCa { get; set; } = new();
        public List<HotelDashboardRecentReservationDto> ReservationsRecentes { get; set; } = new();
        public List<HotelDashboardRecentPaymentDto> PaiementsRecents { get; set; } = new();
        public DateTime PeriodeDebutUtc { get; set; }
        public DateTime PeriodeFinUtc { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class HotelDashboardSummaryDto
    {
        public int HotelsPublies { get; set; }
        public int RoomTypesPublies { get; set; }
        public int AllotmentsActifs { get; set; }
        public int ReservationsConfirmeesMois { get; set; }
        public int ReservationsConfirmeesJour { get; set; }
        public decimal MontantAcomptesSuccesMois { get; set; }
        public int HoldsEnCours { get; set; }
    }

    public class HotelDashboardReservationBreakdownDto
    {
        public int Hold { get; set; }
        public int Confirmed { get; set; }
        public int Cancelled { get; set; }
        public int Expired { get; set; }
    }

    public class HotelDashboardRevenueByProviderDto
    {
        public string Provider { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public int NombrePaiements { get; set; }
    }

    public class HotelDashboardRevenueByDeviseDto
    {
        public string CodeDevise { get; set; } = "CDF";
        public decimal Montant { get; set; }
        public int NombrePaiements { get; set; }
    }

    public class HotelDashboardTopHotelDto
    {
        public int Rang { get; set; }
        public int IdHotel { get; set; }
        public string NomHotel { get; set; } = string.Empty;
        public decimal ChiffreAffaires { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public int ReservationsConfirmees { get; set; }
        public int NuitsConfirmees { get; set; }
    }

    public class HotelDashboardRecentReservationDto
    {
        public int IdHotelReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string? NomHotel { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class HotelDashboardRecentPaymentDto
    {
        public int IdHotelPayment { get; set; }
        public string ReferencePaiement { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string? ReferenceReservation { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class HotelSuperAdminDashboardResponseDto
    {
        public HotelDashboardGlobalSummaryDto Global { get; set; } = new();
        public List<HotelDashboardSocieteSummaryDto> Societes { get; set; } = new();
        public DateTime PeriodeDebutUtc { get; set; }
        public DateTime PeriodeFinUtc { get; set; }
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class HotelDashboardGlobalSummaryDto
    {
        public int TotalSocietesActives { get; set; }
        public int HotelsPublies { get; set; }
        public int RoomTypesPublies { get; set; }
        public int ReservationsConfirmeesMois { get; set; }
        public decimal MontantAcomptes { get; set; }
        public List<HotelDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    public class HotelDashboardSocieteSummaryDto
    {
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public int HotelsPublies { get; set; }
        public int RoomTypesPublies { get; set; }
        public int ReservationsConfirmeesMois { get; set; }
        public decimal MontantAcomptes { get; set; }
        public List<HotelDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
    }

    public class HotelDashboardWidgetDto
    {
        public HotelDashboardSummaryDto Summary { get; set; } = new();
        public List<HotelDashboardRevenueByProviderDto> RevenuParProvider { get; set; } = new();
        public List<HotelDashboardRevenueByDeviseDto> RevenuParDevise { get; set; } = new();
        public List<HotelDashboardTopHotelDto> TopHotelsCa { get; set; } = new();
    }
}
