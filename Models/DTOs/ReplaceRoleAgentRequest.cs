using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class ReplaceRoleAgentRequest
    {
        [Required(ErrorMessage = "L'ancien rôle est obligatoire")]
        public string AncienRoleAgent { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Le nouveau rôle est obligatoire")]
        public string NouveauRoleAgent { get; set; } = string.Empty;
        
        public bool IsPrimary { get; set; } = false;
    }
}

