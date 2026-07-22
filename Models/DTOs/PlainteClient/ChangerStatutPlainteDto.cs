using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.PlainteClient
{
    /// <summary>
    /// DTO pour changer le statut d'une plainte
    /// </summary>
    public class ChangerStatutPlainteDto
    {
        [Required(ErrorMessage = "Le statut est requis")]
        [MaxLength(50)]
        public string StatutPlainte { get; set; } = string.Empty;
    }
}

