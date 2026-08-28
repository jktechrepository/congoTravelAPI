using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateNightRequestDto
    {
        public int IdHotel { get; set; }
        public DateTime NightDate { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
        public string? CodeDevise { get; set; }
        /// <summary>Renseigné par la génération planif (Phase 7b) pour traçabilité.</summary>
        public int? IdHotelPlanification { get; set; }
    }

    public class HotelCreateNightBatchRequestDto
    {
        public int IdHotel { get; set; }
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

    public class HotelUpdateNightRequestDto
    {
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
        public string? CodeDevise { get; set; }
    }

    public class HotelNightListFilter
    {
        public int? IdSociete { get; set; }
        public int? IdHotel { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public HotelStatus? Status { get; set; }
    }

    public class HotelNightResponseDto
    {
        public int IdHotelNight { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public DateTime NightDate { get; set; }
        public int CapaciteTotale { get; set; }
        public int QuantiteHold { get; set; }
        public int QuantiteVendue { get; set; }
        public int QuantiteDisponible { get; set; }
        public decimal PrixNuit { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string Status { get; set; } = string.Empty;
    }

    public class HotelNightBatchResultDto
    {
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<HotelNightResponseDto> Created { get; set; } = new();
    }
}
