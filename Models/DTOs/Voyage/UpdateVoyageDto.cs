using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.DTOs.VoyageTarification;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Mise à jour voyage. Multi-étapes : renseigner <see cref="EtapesDestinations"/> ; sinon comportement legacy avec <see cref="IdDestination"/>.
    /// </summary>
    public class UpdateVoyageDto : IValidatableObject
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public DateTime DateDepart { get; set; }

        [Required]
        public TimeSpan HeureDepart { get; set; }

        /// <summary>
        /// Prix de référence (généralement ECO). Ne pas modifier seul si des tarifs catégorie existent :
        /// utiliser <see cref="Tarifs"/> ou les endpoints tarifs-categorie-siege.
        /// </summary>
        [Required]
        public int Prix { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevisePrix { get; set; } = "CDF";

        [Required]
        public int IdVehicule { get; set; }

        /// <summary>Obligatoire si <see cref="EtapesDestinations"/> est vide (trajet une étape).</summary>
        public int? IdDestination { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSite { get; set; }

        public bool Statut { get; set; }

        /// <summary>Si défini (≥ 1), remplace toutes les lignes <c>VoyageDestinations</c> du voyage.</summary>
        public List<CreateVoyageEtapeDto>? EtapesDestinations { get; set; }

        /// <summary>
        /// Tarifs par catégorie (remplace toutes les lignes). Obligatoire pour toute modification de prix
        /// lorsque le voyage possède déjà des tarifs catégorie.
        /// </summary>
        public List<VoyageTarifCategorieSiegeItemDto>? Tarifs { get; set; }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var etapes = EtapesDestinations?.Where(e => e != null).ToList() ?? new List<CreateVoyageEtapeDto>();
            if (etapes.Count == 0 && (!IdDestination.HasValue || IdDestination.Value <= 0))
            {
                yield return new ValidationResult(
                    "Indiquez soit etapesDestinations (≥ 1 étape), soit idDestination (> 0).",
                    new[] { nameof(IdDestination), nameof(EtapesDestinations) });
                yield break;
            }

            var doublonsOrdre = etapes.GroupBy(e => e.Ordre).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (doublonsOrdre.Count > 0)
            {
                yield return new ValidationResult(
                    $"Ordre dupliqué dans etapesDestinations : {string.Join(", ", doublonsOrdre)}.",
                    new[] { nameof(EtapesDestinations) });
            }

            if (Tarifs is { Count: > 0 })
            {
                var duplicateCategories = Tarifs
                    .GroupBy(t => t.IdCategorieSiege)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();
                if (duplicateCategories.Count > 0)
                {
                    yield return new ValidationResult(
                        $"Catégorie de siège dupliquée dans tarifs : {string.Join(", ", duplicateCategories)}.",
                        new[] { nameof(Tarifs) });
                }
            }
        }
    }
}
