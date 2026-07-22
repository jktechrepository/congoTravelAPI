using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Communication
{
    /// <summary>
    /// DTO pour mettre à jour une campagne de communication
    /// </summary>
    public class UpdateCommunicationCampaignDto
    {
        [MaxLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string? Titre { get; set; }

        [MaxLength(2000, ErrorMessage = "Le contenu ne peut pas dépasser 2000 caractères")]
        public string? Contenu { get; set; }

        [MaxLength(50)]
        public string? TypeCampagne { get; set; }

        public int? IdSociete { get; set; }

        public CriteresCiblageDto? CriteresCiblage { get; set; }

        public bool? ActiverPush { get; set; }

        public bool? ActiverSms { get; set; }

        public bool? ActiverEmail { get; set; }

        public bool? ActiverInApp { get; set; }

        public DateTime? DateEnvoi { get; set; }
    }
}

