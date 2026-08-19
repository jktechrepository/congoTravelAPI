namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>
    /// En-tête session événement pour les listes (sans inventaire imbriqué).
    /// Enrichi : couverture, résumé prix, nom société.
    /// </summary>
    public class EvenementSessionListItemDto
    {
        public int IdEvenementSession { get; set; }

        public int IdSociete { get; set; }

        public string? NomSociete { get; set; }

        public int? IdSite { get; set; }

        public string? NomSite { get; set; }

        public string CodeSession { get; set; } = string.Empty;

        public string Libelle { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartAtUtc { get; set; }

        public DateTime? EndAtUtc { get; set; }

        public string InventoryMode { get; set; } = string.Empty;

        public string TypeEvenement { get; set; } = string.Empty;

        public string? NomOrganisateur { get; set; }

        public string? TelephoneOrganisateur { get; set; }

        public string? MailOrganisateur { get; set; }

        public string? LogoOrganisateur { get; set; }

        public string? Ville { get; set; }

        public string? Commune { get; set; }

        public string? Quartier { get; set; }

        public string? Avenue { get; set; }

        public string? Numero { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }

        /// <summary>Première photo active (ordre min), base64 data-URL comme le détail.</summary>
        public EvenementSessionPhotoDto? PhotoCouverture { get; set; }

        public decimal? PrixMin { get; set; }

        public decimal? PrixMax { get; set; }

        public string? CodeDevise { get; set; }
    }
}
