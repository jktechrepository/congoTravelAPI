using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Reservation
{
    /// <summary>
    /// Création cash aller-retour : 2 voyages, 1 liste passagers, 1 paiement.
    /// </summary>
    public class CreateReservationAllerRetourWithPaiementDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdVoyageAller { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdVoyageRetour { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdClient { get; set; }

        [Required]
        [Range(1, 50)]
        public int NombreDePlace { get; set; } = 1;

        [Required]
        [Range(0, int.MaxValue)]
        public int IdUtilisateur { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int IdSociete { get; set; }

        public int? IdSite { get; set; }

        /// <summary>Passagers identiques sur aller et retour.</summary>
        [Required]
        public List<ReservationPassengerInputDto> Passagers { get; set; } = new();

        [Required]
        public PaiementDataDto Paiement { get; set; } = new();
    }

    /// <summary>
    /// Initiation FlexPay aller-retour.
    /// </summary>
    public class InitiateFlexPayReservationAllerRetourDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdVoyageAller { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdVoyageRetour { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdClient { get; set; }

        [Required]
        [Range(1, 50)]
        public int NombreDePlace { get; set; } = 1;

        [Required]
        [Range(0, int.MaxValue)]
        public int IdUtilisateur { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int IdSociete { get; set; }

        public int? IdSite { get; set; }

        [Required]
        public List<ReservationPassengerInputDto> Passagers { get; set; } = new();

        [Required]
        public FlexPayPaiementDataDto Paiement { get; set; } = new();
    }

    public class ReservationAllerRetourResponseDto
    {
        public int IdReservationAllerRetour { get; set; }
        public int IdVoyageAller { get; set; }
        public int IdVoyageRetour { get; set; }
        public int? IdReservationAller { get; set; }
        public int? IdReservationRetour { get; set; }
        public int? IdPaiement { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int IdSociete { get; set; }
        public int IdClient { get; set; }
        public int IdUtilisateur { get; set; }
        public int? IdSite { get; set; }
        public string Origine { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        public ReservationResponseDto? ReservationAller { get; set; }
        public ReservationResponseDto? ReservationRetour { get; set; }
        public PaiementResponseDto? Paiement { get; set; }
        public List<CongoTravel.Models.DTOs.BilletResponseDto> BilletsAller { get; set; } = new();
        public List<CongoTravel.Models.DTOs.BilletResponseDto> BilletsRetour { get; set; } = new();
    }

    /// <summary>Réponse création cash / FlexPay AR.</summary>
    public class ReservationAllerRetourWithPaiementResponseDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public TransactionStatut Statut { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public ReservationAllerRetourResponseDto? AllerRetour { get; set; }

        public Guid? IdCommandeReservationEnAttente { get; set; }
        public string? OrderNumberFlexPay { get; set; }
        public string? ReferenceFlexPay { get; set; }
        public decimal? MontantVoyage { get; set; }
        public string? CodeDeviseVoyage { get; set; }
        public decimal? MontantFlexPay { get; set; }
        public string? CodeDevisePaiement { get; set; }
        public decimal? TauxApplique { get; set; }
        public DateTime? HoldExpireAt { get; set; }
        public string? PaymentUrl { get; set; }
        public bool? FlexPayAccepted { get; set; }
    }
}
