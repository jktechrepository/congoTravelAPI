using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class CreateAgentDto
    {
        [MaxLength(50)]
        public string? Matricule { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string NomComplet { get; set; }
        
        [Required]
        public int IdSociete { get; set; }
        
        [MaxLength(10)]
        public string? Genre { get; set; }
        
        [Required]
        public DateTime DateNaissance { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string TelephoneAgent { get; set; } = null!;
        
        [MaxLength(200)]
        [EmailAddress]
        public string? EmailAgent { get; set; }
        
        public bool Statut { get; set; } = true;
        
        [MaxLength(20)]
        public string? EtatCivil { get; set; }
        
        public string? SerialNumber { get; set; }
        
        [MaxLength(200)]
        public string? Fonction { get; set; }
        
        [MaxLength(200)]
        public string? RoleAgent { get; set; }
        
        public string? PhotoUrl { get; set; }
        
        [MaxLength(500)]
        public string? AdresseResidence { get; set; }
        
        [MaxLength(200)]
        public string? Zone { get; set; }
    }
}
