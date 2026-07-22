using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.VoyageTarification;

namespace CongoTravel.Models.DTOs.PlanificationVoyage
{
    public class PlanificationVoyageEtapeDto
    {
        [Range(1, int.MaxValue)]
        public int Ordre { get; set; }

        [Range(1, int.MaxValue)]
        public int IdDestination { get; set; }
    }

    public class CreatePlanificationVoyageDto : IValidatableObject
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdSite { get; set; }

        [Required]
        public int IdVehicule { get; set; }

        [Required]
        public TimeSpan HeureDepart { get; set; }

        [Required]
        public int Prix { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevisePrix { get; set; } = "CDF";

        [Required]
        [MinLength(1)]
        public List<int> JoursSemaine { get; set; } = new();

        public bool Statut { get; set; } = true;

        public int? IdDestination { get; set; }

        public List<PlanificationVoyageEtapeDto>? EtapesDestinations { get; set; }

        public List<VoyageTarifCategorieSiegeItemDto>? Tarifs { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var etapes = EtapesDestinations?.Where(e => e != null).ToList() ?? new List<PlanificationVoyageEtapeDto>();
            var hasEtapes = etapes.Count > 0;
            var legacyOk = IdDestination.HasValue && IdDestination.Value > 0;

            if (!hasEtapes && !legacyOk)
            {
                yield return new ValidationResult(
                    "Indiquez soit etapesDestinations (≥ 1 étape), soit idDestination (> 0).",
                    new[] { nameof(IdDestination), nameof(EtapesDestinations) });
            }

            if (JoursSemaine == null || JoursSemaine.Count == 0)
            {
                yield return new ValidationResult(
                    "Au moins un jour de la semaine est requis (0=Dimanche … 6=Samedi).",
                    new[] { nameof(JoursSemaine) });
            }
            else if (JoursSemaine.Any(j => j < 0 || j > 6))
            {
                yield return new ValidationResult(
                    "JoursSemaine doit contenir des valeurs entre 0 (Dimanche) et 6 (Samedi).",
                    new[] { nameof(JoursSemaine) });
            }

            if (hasEtapes)
            {
                var doublonsOrdre = etapes.GroupBy(e => e.Ordre).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                if (doublonsOrdre.Count > 0)
                {
                    yield return new ValidationResult(
                        $"Ordre dupliqué dans etapesDestinations : {string.Join(", ", doublonsOrdre)}.",
                        new[] { nameof(EtapesDestinations) });
                }
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

    public class UpdatePlanificationVoyageDto : CreatePlanificationVoyageDto
    {
        [Required]
        public int IdPlanificationVoyage { get; set; }
    }
}
