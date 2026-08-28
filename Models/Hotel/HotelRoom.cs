using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelRoom
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelRoom { get; set; }
        [Required]
        public int IdSociete { get; set; }
        [Required]
        public int IdHotel { get; set; }
        [Required]
        public int IdHotelRoomType { get; set; }
        [Required, MaxLength(32)]
        public string Numero { get; set; } = string.Empty;
        [MaxLength(32)]
        public string? Etage { get; set; }
        [MaxLength(120)]
        public string? Libelle { get; set; }
        public bool IsActif { get; set; } = true;
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Societe? Societe { get; set; }
        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelRoomType? RoomType { get; set; }
        [JsonIgnore, ValidateNever]
        public ICollection<HotelRoomAssignment> Assignments { get; set; } = new List<HotelRoomAssignment>();
    }

    public class HotelRoomConflictException : InvalidOperationException
    {
        public HotelRoomConflictException(string message) : base(message) { }
    }
}
