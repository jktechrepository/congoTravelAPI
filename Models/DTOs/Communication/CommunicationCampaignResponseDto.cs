namespace CongoTravel.Models.DTOs.Communication
{
    /// <summary>
    /// DTO de réponse pour une campagne de communication avec statistiques
    /// </summary>
    public class CommunicationCampaignResponseDto
    {
        public int IdCampagne { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Contenu { get; set; } = string.Empty;
        public string TypeCampagne { get; set; } = string.Empty;
        public int? IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public int IdUtilisateurCreateur { get; set; }
        public string? NomUtilisateurCreateur { get; set; }
        public bool ActiverPush { get; set; }
        public bool ActiverSms { get; set; }
        public bool ActiverEmail { get; set; }
        public bool ActiverInApp { get; set; }
        public DateTime? DateEnvoi { get; set; }
        public bool EstProgrammee { get; set; }
        public bool EstEnCours { get; set; }
        public bool EstTerminee { get; set; }
        public int NombreDestinataires { get; set; }
        public int NombreEnvoyes { get; set; }
        public int NombreSucces { get; set; }
        public int NombreEchecs { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateDerniereModification { get; set; }
        public DateTime? DateEnvoiEffectif { get; set; }
    }
}

