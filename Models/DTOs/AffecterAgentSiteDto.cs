using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class AffecterAgentSiteDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "IdSite doit être supérieur à 0.")]
        public int IdSite { get; set; }
    }
}
