using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Site
{
    public class SiteCreateDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(40)]
        public string CodeSite { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string NomSite { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? Ville { get; set; }

        [MaxLength(500)]
        public string? Adresse { get; set; }

        [MaxLength(30)]
        /// <summary>
        /// Téléphone du responsable de site.
        /// Nullable individuellement, mais au moins un contact est requis avec Email.
        /// Si Email est vide, ce champ sera utilisé pour provisionner le compte gérant.
        /// </summary>
        public string? Telephone { get; set; }

        [MaxLength(30)]
        public string? NumeroMobileMoney { get; set; }

        [Required]
        [MaxLength(200)]
        public string NomResponsableSite { get; set; } = string.Empty;

        [MaxLength(200)]
        [EmailAddress]
        /// <summary>
        /// Email du responsable de site.
        /// Nullable individuellement, mais au moins un contact est requis avec Telephone.
        /// </summary>
        public string? Email { get; set; }

        [Required]
        [MaxLength(10)]
        public string Genre { get; set; } = "Masculin";

        public bool Statut { get; set; } = true;

        /// <summary>Si true, devient l'unique site principal de la société (transfert automatique).</summary>
        public bool IsSitePrincipal { get; set; } = false;
    }
}
