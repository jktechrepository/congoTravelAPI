using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantPhoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPhoto { get; set; }

        [Required]
        public int IdRestaurant { get; set; }

        /// <summary>Contenu binaire (legacy / dual-write). Nullable après migration S3.</summary>
        public byte[]? PhotoData { get; set; }

        /// <summary>Clé objet S3 ou chemin local.</summary>
        [MaxLength(500)]
        public string? StorageKey { get; set; }

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
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Restaurant? Restaurant { get; set; }
    }
}
