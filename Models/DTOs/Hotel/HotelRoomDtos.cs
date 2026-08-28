using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreateRoomRequestDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotel { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int IdHotelRoomType { get; set; }
        [Required, MaxLength(32)]
        public string Numero { get; set; } = string.Empty;
        [MaxLength(32)]
        public string? Etage { get; set; }
        [MaxLength(120)]
        public string? Libelle { get; set; }
        public bool IsActif { get; set; } = true;
    }

    public class HotelUpdateRoomRequestDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotelRoomType { get; set; }
        [Required, MaxLength(32)]
        public string Numero { get; set; } = string.Empty;
        [MaxLength(32)]
        public string? Etage { get; set; }
        [MaxLength(120)]
        public string? Libelle { get; set; }
        public bool IsActif { get; set; } = true;
    }

    public class HotelRoomResponseDto
    {
        public int IdHotelRoom { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public int IdHotelRoomType { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string? Etage { get; set; }
        public string? Libelle { get; set; }
        public bool IsActif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class HotelRoomListFilter
    {
        public int? IdHotel { get; set; }
        public int? IdHotelRoomType { get; set; }
        public bool? IsActif { get; set; }
    }

    public class HotelAssignRoomItemDto
    {
        [Required, Range(1, int.MaxValue)]
        public int IdHotelReservationLine { get; set; }
        [Required, Range(1, int.MaxValue)]
        public int IdHotelRoom { get; set; }
    }

    public class HotelAssignRoomsRequestDto
    {
        [Required, MinLength(1)]
        public List<HotelAssignRoomItemDto> Items { get; set; } = new();
    }

    public class HotelRoomAssignmentResponseDto
    {
        public int IdHotelRoomAssignment { get; set; }
        public int IdHotelReservation { get; set; }
        public int IdHotelReservationLine { get; set; }
        public int IdHotelRoom { get; set; }
        public string? Numero { get; set; }
        public DateTime DateAttributionUtc { get; set; }
    }
}
