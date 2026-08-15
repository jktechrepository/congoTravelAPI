using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>Règles de gestion centralisées par société (relation 1:1).</summary>
    public class ConfigSociete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdConfigSociete { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Jours de validité billet à partir du jour de départ ; 0 = jour du départ uniquement.</summary>
        public int DureeValiditeBilletJours { get; set; } = 0;

        /// <summary>Pénalité de réaffectation en pourcentage (0–100) du montant payé pour le billet.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaliteReaffectationPourcentage { get; set; } = 0m;

        /// <summary>Horizon max de réservation en jours à partir d'aujourd'hui (UTC) ; null = illimité.</summary>
        public int? JoursAvanceMaxReservation { get; set; }

        /// <summary>Heures avant départ source pour autoriser une réaffectation (0–72).</summary>
        public int HeuresLimiteReaffectation { get; set; } = 2;

        public int HeuresOuvertureEmbarquementAvantDepart { get; set; } = 3;

        public int HeuresFermetureEmbarquementApresJourDepart { get; set; } = 24;

        /// <summary>Heures avant <c>StartAtUtc</c> pour ouvrir le contrôle d'entrée événement (0–72).</summary>
        public int HeuresOuvertureEntreeEvenementAvantDebut { get; set; } = 3;

        /// <summary>Heures avant <c>StartAtUtc</c> créneau pour ouvrir le contrôle d'entrée restaurant (0–72).</summary>
        public int HeuresOuvertureEntreeRestaurantAvantDebut { get; set; } = 1;

        public int DureeHoldFlexPayMinutes { get; set; } = 15;

        /// <summary>Durée du hold réservation événementielle (minutes) ; indépendant du hold FlexPay transport.</summary>
        public int DureeHoldEvenementMinutes { get; set; } = 15;

        /// <summary>Durée du hold réservation site touristique (minutes) ; indépendant du hold FlexPay transport.</summary>
        public int DureeHoldSiteTouristiqueMinutes { get; set; } = 15;

        /// <summary>Durée du hold réservation restaurant (minutes) ; indépendant des holds transport / événement / site touristique.</summary>
        public int DureeHoldRestaurantMinutes { get; set; } = 15;

        public bool ReaffectationActive { get; set; } = true;

        /// <summary>Déclenche un PayOut automatique vers NumeroMobileMoney après confirmation paiement électronique FlexPay.</summary>
        public bool AutoReversementPaiementElectronique { get; set; }

        /// <summary>Part du MontantPaye à reverser au site (0–100 %).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PourcentageReversementSite { get; set; } = 100m;

        /// <summary>Montant fixe prélevé par la plateforme sur chaque reversement auto (0 = aucun).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal FraisPlateforme { get; set; }

        /// <summary>Devise du frais plateforme ; null = devise du paiement.</summary>
        [MaxLength(3)]
        public string? CodeDeviseFraisPlateforme { get; set; }

        /// <summary>Supplément par place ajouté au tarif voyage pour MOBILE_MONEY / CARTE_BANCAIRE (0 = aucun).</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontAddPaieElectronique { get; set; }

        /// <summary>Devise du supplément électronique ; null = devise du voyage.</summary>
        [MaxLength(3)]
        public string? CodeDeviseMontAddPaieElectronique { get; set; }

        /// <summary>Poids de bagage offert (kg) par société ; 0 = aucun.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PoidsBagageParKiloOffert { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        public Societe? Societe { get; set; }
    }
}
