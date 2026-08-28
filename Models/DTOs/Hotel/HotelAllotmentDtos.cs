using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateAllotmentRequestDto
    {
        public int IdHotel { get; set; }
        public int IdHotelRoomType { get; set; }
        public DateTime NightDate { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
        public string? CodeDevise { get; set; }
        /// <summary>Renseigné par la génération planif (Phase 7a) pour traçabilité.</summary>
        public int? IdHotelPlanification { get; set; }
    }

    public class HotelCreateAllotmentBatchRequestDto
    {
        public int IdHotel { get; set; }
        public int IdHotelRoomType { get; set; }
        /// <summary>Début inclusif (check-in).</summary>
        public DateTime From { get; set; }
        /// <summary>Fin exclusive (check-out) — nuits dans [From, To).</summary>
        public DateTime To { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
        public string? CodeDevise { get; set; }
        /// <summary>Si true, ignore les nuits déjà existantes (UQ) au lieu de conflit.</summary>
        public bool SkipExisting { get; set; } = true;
    }

    public class HotelUpdateAllotmentRequestDto
    {
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
        public string? CodeDevise { get; set; }
    }

    public class HotelAllotmentListFilter
    {
        public int? IdSociete { get; set; }
        public int? IdHotel { get; set; }
        public int? IdHotelRoomType { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public HotelStatus? Status { get; set; }
    }

    public class HotelAllotmentResponseDto
    {
        public int IdHotelNightAllotment { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public int IdHotelRoomType { get; set; }
        public string? CodeRoomType { get; set; }
        public string? LibelleRoomType { get; set; }
        public DateTime NightDate { get; set; }
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixNuit { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string Status { get; set; } = string.Empty;
    }

    public class HotelAllotmentBatchResultDto
    {
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<HotelAllotmentResponseDto> Created { get; set; } = new();
    }

    public class HotelAvailabilityNightDto
    {
        public DateTime NightDate { get; set; }
        public int? IdHotelRoomType { get; set; }
        public string? CodeRoomType { get; set; }
        public string? LibelleRoomType { get; set; }
        public int? IdHotelNightAllotment { get; set; }
        public int? IdHotelNight { get; set; }
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixNuit { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }

    public class HotelAvailabilityResponseDto
    {
        public int IdHotel { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string InventoryMode { get; set; } = nameof(HotelInventoryMode.ClassQuota);
        public int? IdHotelRoomType { get; set; }
        /// <summary>Min dispo sur le séjour (roomType filtré en ClassQuota, ou pool global en GlobalQuota).</summary>
        public int? MinDisponible { get; set; }
        public List<HotelAvailabilityNightDto> Nights { get; set; } = new();
    }
}
