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
        /// Photos optionnelles (0 à 3). Chaque entrée : photoBase64, ordre optionnel (1-3), fileName optionnel.
        /// Accepte aussi un tableau de chaînes base64 (voir <see cref="AddPhotoVehiculeDtoListJsonConverter"/>).
        /// </summary>
        [JsonConverter(typeof(AddPhotoVehiculeDtoListJsonConverter))]
        public List<AddPhotoVehiculeDto>? Photos { get; set; }

        /// <summary>
        /// Ancien champ unique (legacy). Converti en une entrée <see cref="Photos"/> si <c>photos</c> est absent.
        /// </summary>
        [JsonPropertyName("photo")]
        public string? LegacyPhoto { get; set; }

        /// <summary>Alias « images » (certains clients front).</summary>
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
