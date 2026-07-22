using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Devise
{
    public class CreateDeviseMonetaireDto
    {
        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        [MinLength(3)]
        public string CodeDevise { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbole { get; set; }

        public bool Statut { get; set; } = true;

        public bool EstDevisePrincipale { get; set; } = false;
    }
}
