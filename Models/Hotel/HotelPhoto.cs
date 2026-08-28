using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelPhoto
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelPhoto { get; set; }
        [Required]
        public int IdHotel { get; set; }
        public byte[]? PhotoData { get; set; }
        [MaxLength(500)]
        public string? StorageKey { get; set; }
        [Required, Range(1, 3)]
        public int Ordre { get; set; }
        [MaxLength(100)]
        public string? OriginalFileName { get; set; }
        [MaxLength(50)]
        public string? TypeMIME { get; set; }
        public long? FileSize { get; set; }
        public bool Statut { get; set; } = true;
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public Hotel? Hotel { get; set; }
    }
}
