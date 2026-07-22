using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Création d'une société avec provisionnement : Admin automatique, site initial et compte Gérant
    /// généré automatiquement à partir des champs du bloc <c>site</c> (comme <c>POST /api/Site</c>).
    /// </summary>
    public class CreateSocieteWithBootstrapDto
    {
        [Required]
        public CreateSocieteBootstrapSocieteDto Societe { get; set; } = null!;

        [Required]
        public CreateSocieteBootstrapSiteDto Site { get; set; } = null!;
    }

    public class CreateSocieteBootstrapSocieteDto
    {
        [Required]
        [StringLength(150)]
        public string Nom { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Devise { get; set; }

        [StringLength(50)]
        public string? Type { get; set; }

        public string? Logo { get; set; }

        [StringLength(20)]
        public string? Telephone { get; set; }

        [StringLength(200)]
        public string? EmailContact { get; set; }

        [StringLength(200)]
        public string? SiteWeb { get; set; }

        [StringLength(200)]
        public string? NomCompletResponsable { get; set; }

        [StringLength(10)]
        public string? GenreResponsable { get; set; }

        public string? Description { get; set; }

        [StringLength(500)]
        public string? AdresseResidence { get; set; }

        public bool? Statut { get; set; } = true;
    }

    public class CreateSocieteBootstrapSiteDto
    {
        [Required]
        [StringLength(40)]
        public string CodeSite { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string NomSite { get; set; } = string.Empty;

        [StringLength(120)]
        public string? Ville { get; set; }

        [StringLength(500)]
        public string? Adresse { get; set; }

        /// <summary>
        /// Téléphone du responsable de site. Au moins un contact avec <see cref="Email"/> est requis pour créer le gérant.
        /// Si <see cref="Email"/> est vide, ce numéro sert d’identifiant pour le compte gérant (EmailAgent / Utilisateur.Email).
        /// </summary>
        [StringLength(30)]
        public string? Telephone { get; set; }

        [StringLength(30)]
        public string? NumeroMobileMoney { get; set; }

        [Required]
        [StringLength(200)]
        public string NomResponsableSite { get; set; } = string.Empty;

        /// <summary>
        /// Email du responsable de site. Au moins un contact avec <see cref="Telephone"/> est requis pour créer le gérant.
        /// </summary>
        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        [StringLength(10)]
        public string Genre { get; set; } = "Masculin";

        public bool Statut { get; set; } = true;
    }
}
