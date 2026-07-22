using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.PlainteClient
{
    /// <summary>
    /// DTO pour mettre à jour une plainte client
    /// </summary>
    public class UpdatePlainteClientDto
    {
        [MaxLength(200)]
        public string? Titre { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? TypePanne { get; set; }

        [MaxLength(50)]
        public string? NiveauImportance { get; set; }

        [MaxLength(500)]
        public string? RisquesPrincipaux { get; set; }

        [MaxLength(50)]
        public string? StatutPlainte { get; set; }

        [MaxLength(50)]
        public string? Priorite { get; set; }

        public int? IdAgentAssigné { get; set; }

        [MaxLength(1000)]
        public string? CommentaireResolution { get; set; }

        public DateTime? DateResolution { get; set; }

        public bool? EstUrgente { get; set; }
    }
}

