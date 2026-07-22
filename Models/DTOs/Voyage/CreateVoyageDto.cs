using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.DTOs.VoyageTarification;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Création d’un voyage. Workflow V2 : utiliser <see cref="EtapesDestinations"/> (≥ 1 étape).
    /// Sinon, fournir uniquement <see cref="IdDestination"/> (une étape, comportement historique).
    /// </summary>
    public class CreateVoyageDto : IValidatableObject
    {
        [Required]
        public DateTime DateDepart { get; set; }

        [Required]
        public TimeSpan HeureDepart { get; set; }

        [Required]
        public int Prix { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevisePrix { get; set; } = "CDF";

        [Required]
        public int IdVehicule { get; set; }

        /// <summary>
        /// Obligatoire si <see cref="EtapesDestinations"/> est vide ou absent : une seule étape (legacy).
        /// Sinon ignoré en faveur de la première étape triée par <see cref="CreateVoyageEtapeDto.Ordre"/>.
        /// </summary>
        public int? IdDestination { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSite { get; set; }

        public bool Statut { get; set; } = true;

        /// <summary>
        /// Étapes ordonnées du trajet (multi-destinations). Si au moins une entrée est fournie,
        /// elle définit tout le parcours et remplit aussi la colonne legacy <c>Voyages.IdDestination</c>
        /// avec la destination de la première étape (plus petit ordre).
        /// </summary>
        public List<CreateVoyageEtapeDto>? EtapesDestinations { get; set; }

        /// <summary>
        /// Tarifs initiaux par catégorie de siège. Si omis, un tarif ECO par défaut est créé à partir de <see cref="Prix"/>.
        /// </summary>
        public List<VoyageTarifCategorieSiegeItemDto>? Tarifs { get; set; }

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var etapes = EtapesDestinations?.Where(e => e != null).ToList() ?? new List<CreateVoyageEtapeDto>();
            var hasEtapes = etapes.Count > 0;
            var legacyOk = IdDestination.HasValue && IdDestination.Value > 0;

            if (!hasEtapes && !legacyOk)
            {
                yield return new ValidationResult(
                    "Indiquez soit etapesDestinations (≥ 1 étape), soit idDestination (> 0) pour un trajet à une étape.",
                    new[] { nameof(IdDestination), nameof(EtapesDestinations) });
                yield break;
            }

            if (!hasEtapes)
                yield break;

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

