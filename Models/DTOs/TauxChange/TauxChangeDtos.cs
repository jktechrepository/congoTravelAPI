using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.TauxChange
{
    public class UpsertTauxChangeDto : IValidatableObject
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSociete { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDeviseSource { get; set; } = "USD";

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDeviseCible { get; set; } = "CDF";

        [Required]
        public decimal Taux { get; set; }

        public DateTime? DateEffet { get; set; }

        /// <summary>
        /// Bornes du taux sans <see cref="RangeAttribute"/> sur <see cref="decimal"/> (évite le parse des chaînes
        /// dépendant de la culture, ex. « 0.00000001 » en <c>fr-FR</c>).
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            const decimal minTaux = 0.00000001m;
            const decimal maxTaux = 999999999m;
            if (Taux < minTaux || Taux > maxTaux)
            {
                yield return new ValidationResult(
                    $"Le taux doit être compris entre {minTaux} et {maxTaux}.",
                    new[] { nameof(Taux) });
            }
        }
    }
}

