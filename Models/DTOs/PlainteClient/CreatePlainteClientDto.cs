using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.PlainteClient
{
    /// <summary>
    /// DTO pour créer une nouvelle plainte client
    /// </summary>
    public class CreatePlainteClientDto
    {
        [Required(ErrorMessage = "L'ID du client est requis")]
        public int IdClient { get; set; }

        /// <summary>
        /// ID du signalement de panne référencé (optionnel)
        /// Si fourni, référence un PanneSignalement existant
        /// </summary>
        public int? IdPanneSignalement { get; set; }

        [Required(ErrorMessage = "Le titre de la plainte est requis")]
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Titre { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "La description ne peut pas dépasser 2000 caractères")]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? TypePanne { get; set; }

        [MaxLength(50)]
        public string? NiveauImportance { get; set; }

        [MaxLength(500)]
        public string? RisquesPrincipaux { get; set; }

        [MaxLength(50)]
        public string? Priorite { get; set; }

        /// <summary>
        /// Marquer la plainte comme urgente
        /// </summary>
        public bool EstUrgente { get; set; } = false;
    }
}

