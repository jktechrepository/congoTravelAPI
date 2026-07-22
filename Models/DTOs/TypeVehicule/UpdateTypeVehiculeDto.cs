using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateTypeVehiculeDto
    {
        [Required]
        public int IdTypeVehicule { get; set; }

        [Required]
        [MaxLength(20)]
        public string Libelle { get; set; } = string.Empty;

        public bool Statut { get; set; }
    }
}
