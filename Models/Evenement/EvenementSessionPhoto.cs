using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Evenement
{
    public class EvenementSessionPhoto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementSessionPhoto { get; set; }

        [Required]
        public int IdEvenementSession { get; set; }

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
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementSession? Session { get; set; }
    }
}
