using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.PlainteClient
{
    /// <summary>
    /// DTO pour assigner une plainte à un agent
    /// </summary>
    public class AssignerPlainteDto
    {
        [Required(ErrorMessage = "L'ID de l'agent est requis")]
        public int IdAgentAssigné { get; set; }
    }
}

