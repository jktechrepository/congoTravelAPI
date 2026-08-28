using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateExtraRequestDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotel { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
        [MaxLength(3)]
        public string? CodeDevise { get; set; }
        public HotelExtraPricingUnit PricingUnit { get; set; } = HotelExtraPricingUnit.PerStay;
        public bool IsActif { get; set; } = true;
    }

    public class HotelUpdateExtraRequestDto
    {
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
        [MaxLength(3)]
        public string? CodeDevise { get; set; }
        public HotelExtraPricingUnit PricingUnit { get; set; } = HotelExtraPricingUnit.PerStay;
        public bool IsActif { get; set; } = true;
    }

    public class HotelExtraResponseDto
    {
        public int IdHotelExtra { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public decimal PrixUnitaire { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string PricingUnit { get; set; } = nameof(HotelExtraPricingUnit.PerStay);
        public bool IsActif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class HotelExtraListFilter
    {
        public int? IdHotel { get; set; }
        public bool? IsActif { get; set; }
    }

    public class HotelSetReservationExtraItemDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotelExtra { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class HotelSetReservationExtrasRequestDto
    {
        public List<HotelSetReservationExtraItemDto> Items { get; set; } = new();
    }

    public class HotelReservationExtraResponseDto
    {
        public int IdHotelReservationExtra { get; set; }
        public int IdHotelExtra { get; set; }
        public string? Code { get; set; }
        public string? Libelle { get; set; }
        public string PricingUnit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PrixUnitaireSnapshot { get; set; }
        public decimal MontantLigne { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }
}
