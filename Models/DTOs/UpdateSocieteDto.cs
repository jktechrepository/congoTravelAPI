using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateSocieteDto
    {
        [Required]
        public int IdSociete { get; set; }
        
        [Required]
        [StringLength(150)]
        public string? Nom { get; set; }
        
        [StringLength(500)]
        public string? Devise { get; set; }
        
        [StringLength(50)]
        public string? Type { get; set; }
        
      //  [StringLength(500)]
        public string? Logo { get; set; }
        
        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(200)]
        public string? EmailContact { get; set; }
        
        [StringLength(200)]
        public string? SiteWeb { get; set; }
        
        [StringLength(200)]
        public string? NomCompletResponsable { get; set; }
        
        [StringLength(10)]
        public string? GenreResponsable { get; set; }
        
        public string? Description { get; set; }
        
        /// <summary>
        /// Adresse de la société
        /// </summary>
        [StringLength(500)]
        public string? AdresseResidence { get; set; }
    }
}
