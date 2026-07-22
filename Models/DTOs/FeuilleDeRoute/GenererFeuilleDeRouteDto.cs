using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.FeuilleDeRoute
{
    public class GenererFeuilleDeRouteDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdVoyage { get; set; }
    }
}
