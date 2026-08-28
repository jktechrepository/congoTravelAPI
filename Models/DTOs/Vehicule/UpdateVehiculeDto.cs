using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CongoTravel.Helpers;

namespace CongoTravel.Models.DTOs
{
    public class UpdateVehiculeDto
    {
        [Required]
        public int IdVehicule { get; set; }

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
        /// Répartition des sièges par catégorie. Si non fourni, la répartition existante est conservée/synchronisée.
        /// </summary>
        public List<VehiculeCategorieSiegeAllocationDto>? RepartitionCategorieSieges { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(20)]
        public string NumeroDePlaque { get; set; } = string.Empty;

        public bool Statut { get; set; }

        /// <summary>
        /// LEGACY / déprécié — photos embarquées en photoBase64.
        /// null = ne pas modifier ; [] = tout supprimer ; 1–3 = remplacer.
        /// Préférer <c>PUT /api/Vehicule/{id}/photos</c> multipart (champ <c>files</c>).
        /// </summary>
        [JsonConverter(typeof(AddPhotoVehiculeDtoListJsonConverter))]
        public List<AddPhotoVehiculeDto>? Photos { get; set; }

        /// <summary>LEGACY — ancien champ unique. Remplace les photos si <see cref="Photos"/> est null. Préférer multipart.</summary>
        [JsonPropertyName("photo")]
        public string? LegacyPhoto { get; set; }

        /// <summary>LEGACY — alias « images ». Préférer multipart.</summary>
        [JsonPropertyName("images")]
        [JsonConverter(typeof(AddPhotoVehiculeDtoListJsonConverter))]
        public List<AddPhotoVehiculeDto>? Images
        {
            set
            {
                if (value != null)
                    Photos = value;
            }
        }

        /// <summary>null = ne pas toucher aux photos ; sinon liste à appliquer (y compris vide).</summary>
        public IReadOnlyList<AddPhotoVehiculeDto>? ResolvePhotosForPersistence()
        {
            if (Photos != null)
                return Photos;

            if (!string.IsNullOrWhiteSpace(LegacyPhoto))
                return new List<AddPhotoVehiculeDto> { new() { PhotoBase64 = LegacyPhoto.Trim() } };

            return null;
        }
    }
}
