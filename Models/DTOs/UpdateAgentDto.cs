using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateAgentDto
    {
        [Required]
        public int IdAgent { get; set; }
        
        [Required]
        public int IdSociete { get; set; }
        
        [Required]
        [StringLength(200)]
        public string? NomComplet { get; set; }
        
        [StringLength(10)]
        public string? Genre { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? DateNaissance { get; set; }
        
        [StringLength(200)]
        public string? EmailAgent { get; set; }
        
        [StringLength(20)]
        public string? TelephoneAgent { get; set; }
        
      
        public string? PhotoUrl { get; set; }
        
        [StringLength(20)]
        public string? EtatCivil { get; set; }
        
        [StringLength(200)]
        public string? Fonction { get; set; }
        
        [StringLength(200)]
        public string? RoleAgent { get; set; }
        
        /// <summary>
        /// Adresse de résidence de l'agent
        /// </summary>
        [StringLength(500)]
        public string? AdresseResidence { get; set; }
    }
}
