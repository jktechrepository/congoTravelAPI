using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant un paiement pour une réservation
    /// </summary>
    public class Paiement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPaiement { get; set; }

        /// <summary>
        /// Montant total à payer pour la réservation
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantAPaye { get; set; }

        /// <summary>
        /// Montant déjà payé (peut être null pour un paiement partiel)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Montant restant à payer (calculé automatiquement)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ResteAPaye { get; set; }

        /// <summary>
        /// Devise du paiement saisi (ex: CDF, USD).
        /// </summary>
        [Required]
        [MaxLength(3)]
        public string CodeDevisePaiement { get; set; } = "CDF";

        /// <summary>
        /// Devise principale de la société au moment de la transaction (snapshot).
        /// </summary>
        [Required]
        [MaxLength(3)]
        public string CodeDevisePrincipale { get; set; } = "CDF";

        /// <summary>
        /// Taux appliqué pour convertir de CodeDevisePaiement vers CodeDevisePrincipale.
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePrincipale { get; set; } = 1m;

        /// <summary>
        /// Montant total à payer converti dans la devise principale (snapshot).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantAPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Montant payé converti dans la devise principale (snapshot).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Reste à payer converti dans la devise principale (snapshot).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ResteAPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Date métier du paiement (utilisée pour déterminer le taux).
        /// </summary>
        public DateTime DatePaiement { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Méthode de paiement utilisée (Espèces, Carte, Mobile Money, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence unique de la transaction (numéro de transaction, ID de paiement mobile, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Statut du paiement (true = payé/complété, false = en attente/annulé)
        /// </summary>
        [Required]
        public bool Statut { get; set; }

        /// <summary>
        /// Statut métier détaillé (<see cref="Enums.StatutPaiementMetier"/>). Null = legacy (dérivé de <see cref="Statut"/>).
        /// </summary>
        public int? StatutPaiementMetier { get; set; }

        /// <summary>
        /// Date de création du paiement
        /// </summary>
        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière modification du paiement
        /// </summary>
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Indique si le paiement a été supprimé (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Identifiant de l'utilisateur qui a effectué le paiement
        /// </summary>
        [Required]
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// Identifiant de la réservation concernée par le paiement (peut être null pour un paiement général)
        /// </summary>
        public int? IdReservation { get; set; }

        /// <summary>
        /// Agrégat aller-retour (null = paiement single-leg). Pour AR, <see cref="IdReservation"/> pointe vers l'aller.
        /// </summary>
        public int? IdReservationAllerRetour { get; set; }

        /// <summary>
        /// Identifiant de la société (pour le multi-tenant)
        /// </summary>
        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site associée au paiement (optionnel, même société).</summary>
        public int? IdSite { get; set; }

        /// <summary>
        /// Canal d'origine du paiement (session client vs rôle staff). Snapshot serveur.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Origine { get; set; } = Enums.OrigineOperation.Default;

        /// <summary>
        /// Date d'émission du billet (si le billet a été émis automatiquement)
        /// </summary>
        public DateTime? DateEmissionBillet { get; set; }

        /// <summary>
        /// Identifiant du billet émis automatiquement suite à ce paiement
        /// </summary>
        public int? IdBilletEmis { get; set; }

        // Navigation properties
        /// <summary>
        /// Utilisateur qui a effectué le paiement
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdUtilisateur")]
        public virtual Utilisateur? Utilisateur { get; set; }

        /// <summary>
        /// Réservation concernée par le paiement
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdReservation")]
        public virtual Reservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdReservationAllerRetour))]
        public virtual ReservationAllerRetour? ReservationAllerRetour { get; set; }

        /// <summary>
        /// Société à laquelle appartient le paiement
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public virtual Societe? Societe { get; set; }

        /// <summary>
        /// Site liée au paiement
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSite")]
        public virtual Site? Site { get; set; }

        /// <summary>
        /// Billet émis automatiquement suite à ce paiement
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdBilletEmis")]
        public virtual Billet? BilletEmis { get; set; }

        /// <summary>
        /// Calcule automatiquement le reste à payer
        /// </summary>
        [NotMapped]
        public decimal ResteAPayeCalcule => MontantAPaye - (MontantPaye ?? 0);

        /// <summary>
        /// Vérifie si le paiement est complètement payé
        /// </summary>
        [NotMapped]
        public bool EstComplet => ResteAPayeCalcule <= 0;

        /// <summary>
        /// Vérifie si le paiement est partiellement payé
        /// </summary>
        [NotMapped]
        public bool EstPartiel => MontantPaye.HasValue && MontantPaye > 0 && MontantPaye < MontantAPaye;

        /// <summary>
        /// Met à jour le reste à payer automatiquement
        /// </summary>
        public void MettreAJourResteAPaye()
        {
            ResteAPaye = ResteAPayeCalcule;
            DateModification = DateTime.UtcNow;
        }
    }
}
