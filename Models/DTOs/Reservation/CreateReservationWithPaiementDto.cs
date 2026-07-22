using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Reservation
{
    /// <summary>
    /// DTO pour la création d'une réservation avec paiement en une seule transaction
    /// </summary>
    public class CreateReservationWithPaiementDto
    {
        /// <summary>
        /// Données de la réservation
        /// </summary>
        [Required(ErrorMessage = "Les données de réservation sont requises")]
        public ReservationDataDto Reservation { get; set; } = new();

        /// <summary>
        /// Données du paiement
        /// </summary>
        [Required(ErrorMessage = "Les données de paiement sont requises")]
        public PaiementDataDto Paiement { get; set; } = new();
    }

    /// <summary>
    /// DTO pour les données de réservation
    /// </summary>
    public class ReservationDataDto
    {
        /// <summary>
        /// ID du voyage
        /// </summary>
        [Required(ErrorMessage = "L'ID du voyage est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du voyage doit être valide")]
        public int IdVoyage { get; set; }

        /// <summary>
        /// ID du client
        /// </summary>
        [Required(ErrorMessage = "L'ID du client est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du client doit être valide")]
        public int IdClient { get; set; }

        /// <summary>
        /// Nombre de places à réserver
        /// </summary>
        [Required(ErrorMessage = "Le nombre de places est requis")]
        [Range(1, 50, ErrorMessage = "Le nombre de places doit être entre 1 et 50")]
        public int NombreDePlace { get; set; } = 1;

        /// <summary>
        /// ID de l'utilisateur qui effectue la réservation
        /// </summary>
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être valide")] // TODO: Remettre à 1 après débogage
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// ID de la société (pour validation multi-tenant)
        /// </summary>
        [Required(ErrorMessage = "L'ID de la société est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")] // TODO: Remettre à 1 après débogage
        public int IdSociete { get; set; }

        /// <summary>
        /// Liste des passagers (obligatoire) : un passager par place avec catégorie de siège demandée.
        /// </summary>
        public List<ReservationPassengerInputDto>? Passagers { get; set; }

        /// <summary>Site (optionnel, doit appartenir à <see cref="IdSociete"/>).</summary>
        public int? IdSite { get; set; }
    }

    /// <summary>
    /// Passager pour une réservation multi-places (workflow V2).
    /// </summary>
    public class ReservationPassengerInputDto
    {
        public int? IdClient { get; set; }

        [Required(ErrorMessage = "La catégorie de siège est requise")]
        [Range(1, int.MaxValue, ErrorMessage = "La catégorie de siège doit être valide")]
        public int IdCategorieSiege { get; set; }

        [Required(ErrorMessage = "Le nom complet du passager est requis")]
        [MaxLength(200)]
        public string NomComplet { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [MaxLength(256)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? DocumentType { get; set; }

        [MaxLength(100)]
        public string? DocumentNumero { get; set; }

        [MaxLength(10)]
        public string? Genre { get; set; }
    }

    /// <summary>
    /// DTO pour les données de paiement
    /// </summary>
    public class PaiementDataDto
    {
        /// <summary>
        /// Montant total à payer
        /// </summary>
        [Required(ErrorMessage = "Le montant à payer est requis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal MontantAPaye { get; set; }

        /// <summary>
        /// Montant payé (peut être égal ou inférieur au montant à payer)
        /// </summary>
        [Required(ErrorMessage = "Le montant payé est requis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant payé doit être supérieur à 0")]
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Méthode de paiement utilisée
        /// </summary>
        [Required(ErrorMessage = "La méthode de paiement est requise")]
        [StringLength(50, ErrorMessage = "La méthode de paiement ne peut pas dépasser 50 caractères")]
        public string MethodePaiement { get; set; } = string.Empty;

        /// <summary>
        /// Référence unique de la transaction
        /// </summary>
        [StringLength(100, ErrorMessage = "La référence de transaction ne peut pas dépasser 100 caractères")]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// ID de l'utilisateur qui effectue le paiement
        /// </summary>
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être valide")] // TODO: Remettre à 1 après débogage
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// ID de la société (pour validation multi-tenant)
        /// </summary>
        [Required(ErrorMessage = "L'ID de la société est requis")]
        [Range(0, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")] // TODO: Remettre à 1 après débogage
        public int IdSociete { get; set; }

        /// <summary>Site (optionnel, doit appartenir à <see cref="IdSociete"/>).</summary>
        public int? IdSite { get; set; }
    }
}
