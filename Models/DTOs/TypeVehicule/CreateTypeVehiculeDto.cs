using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class CreateTypeVehiculeDto
    {
        [Required]
        [MaxLength(20)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public int IdSociete { get; set; }

        public bool Statut { get; set; } = true;
    }
}
