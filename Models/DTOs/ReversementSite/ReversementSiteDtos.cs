using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Enums;

namespace CongoTravel.Models.DTOs.ReversementSite
{
    public class InitierReversementSiteDto
    {
        [Required]
        public int IdSite { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Montant { get; set; }

        [Required]
        [MaxLength(10)]
        public string CodeDevise { get; set; } = "CDF";

        [MaxLength(500)]
        public string? Motif { get; set; }
    }

    public class ReversementSiteResponseDto
    {
        public int IdReversementSite { get; set; }
        public int? IdPaiement { get; set; }
        public int? IdReservation { get; set; }
        public string? ModulePaiement { get; set; }
        public int? IdPaiementSource { get; set; }
        public string Origine { get; set; } = string.Empty;
        public int IdSite { get; set; }
        public int IdSociete { get; set; }
        public int IdUtilisateur { get; set; }
        public string NumeroMobileMoney { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string Reference { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public string? ProviderReference { get; set; }
        public string? CodeMarchand { get; set; }
        public StatutReversementSite Statut { get; set; }
        public string? CodeFlexPay { get; set; }
        public string? MessageFlexPay { get; set; }
        public string? Channel { get; set; }
        public string? Motif { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateCallback { get; set; }
    }

    public class FlexPayPayOutCallbackProcessResultDto
    {
        public bool Success { get; set; }
        public bool AlreadyProcessed { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdReversementSite { get; set; }
        public StatutReversementSite? Statut { get; set; }
    }
}
