using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class CreateCategorieSiegeDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSociete { get; set; }

        [Required]
        [StringLength(40)]
        public string CodeCategorieSiege { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Libelle { get; set; } = string.Empty;

        public bool Statut { get; set; } = true;
    }

    public class UpdateCategorieSiegeDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdCategorieSiege { get; set; }

        [Required]
        [StringLength(40)]
        public string CodeCategorieSiege { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Libelle { get; set; } = string.Empty;

        public bool Statut { get; set; } = true;
    }
}
