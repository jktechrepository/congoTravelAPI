using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreateEtablissementRequestDto
    {
        [Required]
        [MaxLength(64)]
        public string CodeRestaurant { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [Range(0, 100)]
        public decimal AcomptePourcentDefaut { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdSite { get; set; }

        /// <summary>
        /// LEGACY / déprécié — photos embarquées en photoBase64 (max 3).
        /// Préférer : créer l'établissement sans photos, puis POST/PUT multipart
        /// <c>/api/restaurants/etablissements/{id}/photos</c>.
        /// </summary>
        public List<AddRestaurantPhotoDto>? Photos { get; set; }
    }
}
