using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Communication
{
    /// <summary>
    /// DTO pour créer une nouvelle campagne de communication
    /// </summary>
    public class CreateCommunicationCampaignDto
    {
        [Required(ErrorMessage = "Le titre de la campagne est requis")]
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Titre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu de la campagne est requis")]
        [MaxLength(2000, ErrorMessage = "Le contenu ne peut pas dépasser 2000 caractères")]
        public string Contenu { get; set; } = string.Empty;

        [MaxLength(50)]
        public string TypeCampagne { get; set; } = "INFO";

        public int? IdSociete { get; set; }

        /// <summary>
        /// Critères de ciblage des clients
        /// </summary>
        public CriteresCiblageDto? CriteresCiblage { get; set; }

        /// <summary>
        /// Activer les notifications push
        /// </summary>
        public bool ActiverPush { get; set; } = true;

        /// <summary>
        /// Activer les SMS
        /// </summary>
        public bool ActiverSms { get; set; } = false;

        /// <summary>
        /// Activer les emails
        /// </summary>
        public bool ActiverEmail { get; set; } = false;

        /// <summary>
        /// Activer les notifications in-app
        /// </summary>
        public bool ActiverInApp { get; set; } = true;

        /// <summary>
        /// Date d'envoi programmé (null = envoi immédiat)
        /// </summary>
        public DateTime? DateEnvoi { get; set; }
    }
}

