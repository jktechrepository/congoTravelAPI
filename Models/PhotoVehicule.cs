using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    public class PhotoVehicule
    {
        [Key]
        public int IdPhotoVehicule { get; set; }

        [Required]
        public int IdVehicule { get; set; }

        /// <summary>Contenu binaire de l'image (JPEG/PNG). Exposé en base64 via l'API.</summary>
        [Required]
        public byte[] PhotoData { get; set; } = Array.Empty<byte>();

        /// <summary>Position d'affichage (1 à 3).</summary>
        [Required]
        [Range(1, 3)]
        public int Ordre { get; set; }

        [MaxLength(100)]
        public string? OriginalFileName { get; set; }

        [MaxLength(50)]
        public string? TypeMIME { get; set; }

        public long? FileSize { get; set; }

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Vehicule? Vehicule { get; set; }
    }
}
