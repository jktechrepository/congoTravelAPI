using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Remboursement
{
    public class CreateRemboursementDto : IValidatableObject
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdPaiement { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdSociete { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int IdUtilisateur { get; set; }

        [Required]
        public decimal MontantRembourse { get; set; }

        [StringLength(3, MinimumLength = 3)]
        public string? CodeDeviseRemboursement { get; set; }

        public bool ForcerDevisePrincipale { get; set; }

        public DateTime? DateRemboursement { get; set; }

        [MaxLength(250)]
        public string? Motif { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const decimal min = 0.01m;
            const decimal max = 999999999m;
            if (MontantRembourse < min || MontantRembourse > max)
            {
                yield return new ValidationResult(
                    $"Le montant doit être compris entre {min} et {max}.",
                    new[] { nameof(MontantRembourse) });
            }
        }
    }
}

