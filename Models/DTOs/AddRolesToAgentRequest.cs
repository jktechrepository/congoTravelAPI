using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class AddRolesToAgentRequest
    {
        [Required(ErrorMessage = "Les rôles sont obligatoires")]
        public List<string> Roles { get; set; } = new List<string>();
        
        /// <summary>
        /// Indique si le rôle doit être marqué comme principal. Optionnel, valeur par défaut: false
        /// </summary>
        public bool? IsPrimary { get; set; }
    }
}

