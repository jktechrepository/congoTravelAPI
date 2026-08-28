using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CongoTravel.Helpers;

namespace CongoTravel.Models.DTOs
{
    public class CreateVehiculeDto
    {
        [MaxLength(100)]
        public string? Marques { get; set; }

        [Required]
        [MaxLength(100)]
        public string AliasVehicule { get; set; } = string.Empty;

        [Required]
        public int IdTypeVehicule { get; set; }

        [Required]
        public int NombreSiege { get; set; }

        /// <summary>
        /// Répartition des sièges par catégorie. Si non fourni, fallback sur une catégorie ECO.
        /// </summary>
        public List<VehiculeCategorieSiegeAllocationDto>? RepartitionCategorieSieges { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(20)]
        public string NumeroDePlaque { get; set; } = string.Empty;

        public bool Statut { get; set; } = true;

        /// <summary>
        /// LEGACY / déprécié — photos embarquées en photoBase64.
        /// Préférer : créer le véhicule sans photos, puis POST/PUT multipart
        /// <c>/api/Vehicule/{id}/photos</c> (champ <c>file</c> ou <c>files</c>).
        /// Conservé pour compatibilité clients existants (0 à 3 entrées).
        /// </summary>
        [JsonConverter(typeof(AddPhotoVehiculeDtoListJsonConverter))]
        public List<AddPhotoVehiculeDto>? Photos { get; set; }

        /// <summary>
        /// LEGACY — ancien champ unique. Converti en une entrée <see cref="Photos"/> si <c>photos</c> est absent.
        /// Préférer multipart sur <c>/api/Vehicule/{id}/photos</c>.
        /// </summary>
        [JsonPropertyName("photo")]
        public string? LegacyPhoto { get; set; }

        /// <summary>LEGACY — alias « images ». Préférer multipart.</summary>
        [JsonPropertyName("images")]
        [JsonConverter(typeof(AddPhotoVehiculeDtoListJsonConverter))]
        public List<AddPhotoVehiculeDto>? Images
        {
            set
            {
                if (value is { Count: > 0 })
                    Photos = value;
            }
        }

        /// <summary>Liste effective pour persistance (photos ou legacy photo).</summary>
        public IReadOnlyList<AddPhotoVehiculeDto>? ResolvePhotosForPersistence()
        {
            if (Photos is { Count: > 0 })
                return Photos;

            if (!string.IsNullOrWhiteSpace(LegacyPhoto))
                return new List<AddPhotoVehiculeDto> { new() { PhotoBase64 = LegacyPhoto.Trim() } };

            return Photos;
        }
    }

    public class VehiculeCategorieSiegeAllocationDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdCategorieSiege { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int NombreSiegeParCategorie { get; set; }
    }
}
