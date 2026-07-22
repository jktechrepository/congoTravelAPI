using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la mise à jour d'un paiement
    /// </summary>
    public class UpdatePaiementDto
    {
        /// <summary>
        /// Montant total à payer pour la réservation
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? MontantAPaye { get; set; }

        /// <summary>
        /// Montant déjà payé
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant payé doit être supérieur à 0")]
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Devise du paiement (ex: CDF, USD).
        /// </summary>
        [StringLength(3, MinimumLength = 3, ErrorMessage = "La devise doit être un code ISO 3 lettres")]
        public string? CodeDevisePaiement { get; set; }

        /// <summary>
        /// Date métier de paiement. Sert à choisir le taux de change applicable.
        /// </summary>
        public DateTime? DatePaiement { get; set; }

        /// <summary>
        /// Méthode de paiement utilisée
        /// </summary>
        [MaxLength(50, ErrorMessage = "La méthode de paiement ne peut pas dépasser 50 caractères")]
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence unique de la transaction
        /// </summary>
        [MaxLength(100, ErrorMessage = "La référence de transaction ne peut pas dépasser 100 caractères")]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Statut du paiement
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur qui a effectué le paiement
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit être valide")]
        public int? IdUtilisateur { get; set; }

        /// <summary>
        /// Identifiant de la réservation concernée par le paiement
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la réservation doit être valide")]
        public int? IdReservation { get; set; }

        /// <summary>
        /// Identifiant de la société
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
        public int? IdSociete { get; set; }

        /// <summary>Mise à jour du site (optionnel). Non renseigné = inchangé.</summary>
        public int? IdSite { get; set; }

        /// <summary>Si true, retire le site du paiement.</summary>
        public bool DesassocierSite { get; set; }
    }
}
