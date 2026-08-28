using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateEtablissementRequestDto
    {
        [Required, MaxLength(64)]
        public string CodeHotel { get; set; } = string.Empty;
        [Required, MaxLength(255)]
        public string Nom { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        [MaxLength(500)]
        public string? Adresse { get; set; }
        [Range(0, 100)]
        public decimal AcomptePourcentDefaut { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int IdSite { get; set; }
        public List<AddHotelPhotoDto>? Photos { get; set; }
    }

    public class HotelUpdateEtablissementRequestDto
    {
        [Required, MaxLength(255)]
        public string Nom { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Description { get; set; }
        [MaxLength(500)]
        public string? Adresse { get; set; }
        [Range(0, 100)]
        public decimal? AcomptePourcentDefaut { get; set; }
        [Range(1, int.MaxValue)]
        public int? IdSite { get; set; }
    }

    public class HotelEtablissementResponseDto
    {
        public int IdHotel { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public string CodeHotel { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Adresse { get; set; }
        public decimal AcomptePourcentDefaut { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public int RoomTypesCount { get; set; }
        public HotelPhotoDto? PhotoCouverture { get; set; }
        public List<HotelPhotoDto> Photos { get; set; } = new();
    }

    public class HotelEtablissementListItemDto
    {
        public int IdHotel { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int? IdSite { get; set; }
        public string? NomSite { get; set; }
        public string CodeHotel { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Adresse { get; set; }
        public decimal AcomptePourcentDefaut { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public HotelPhotoDto? PhotoCouverture { get; set; }
    }

    public class HotelEtablissementListFilter
    {
        public HotelStatus? Status { get; set; }
        public int? IdSociete { get; set; }
    }
}
