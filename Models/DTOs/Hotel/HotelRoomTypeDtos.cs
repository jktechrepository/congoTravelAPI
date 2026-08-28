using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateRoomTypeRequestDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotel { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [Range(1, int.MaxValue)]
        public int? CapacitePersonnesMax { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? PrixNuitReference { get; set; }
        [MaxLength(3)]
        public string? CodeDevise { get; set; }
    }

    public class HotelUpdateRoomTypeRequestDto
    {
        [Required, MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        [Range(1, int.MaxValue)]
        public int? CapacitePersonnesMax { get; set; }
        [Range(0, double.MaxValue)]
        public decimal? PrixNuitReference { get; set; }
        [MaxLength(3)]
        public string? CodeDevise { get; set; }
    }

    public class HotelRoomTypeResponseDto
    {
        public int IdHotelRoomType { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? CapacitePersonnesMax { get; set; }
        public decimal? PrixNuitReference { get; set; }
        public string? CodeDevise { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class HotelRoomTypeListFilter
    {
        public int? IdSociete { get; set; }
        public int? IdHotel { get; set; }
        public HotelStatus? Status { get; set; }
    }
}
