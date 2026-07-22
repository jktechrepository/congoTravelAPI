using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.PlainteClient
{
    /// <summary>
    /// DTO pour résoudre une plainte
    /// </summary>
    public class ResoudrePlainteDto
    {
        [MaxLength(1000, ErrorMessage = "Le commentaire ne peut pas dépasser 1000 caractères")]
        public string? CommentaireResolution { get; set; }
    }
}

