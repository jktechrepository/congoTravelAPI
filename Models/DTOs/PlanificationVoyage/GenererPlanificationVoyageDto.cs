using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Enums;

namespace CongoTravel.Models.DTOs.PlanificationVoyage
{
    public class GenererPlanificationVoyageDto : IValidatableObject
    {
        [Required]
        public PlanificationGenerationMode Mode { get; set; } = PlanificationGenerationMode.MoisCourant;

        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Mode == PlanificationGenerationMode.PeriodePersonnalisee)
            {
                if (!DateDebut.HasValue || !DateFin.HasValue)
                {
                    yield return new ValidationResult(
                        "DateDebut et DateFin sont requis pour PeriodePersonnalisee.",
                        new[] { nameof(DateDebut), nameof(DateFin) });
                }
                else if (DateFin.Value.Date < DateDebut.Value.Date)
                {
                    yield return new ValidationResult(
                        "DateFin doit être postérieure ou égale à DateDebut.",
                        new[] { nameof(DateFin) });
                }
            }
        }
    }

    public class PlanificationGenerationDetailDto
    {
        public DateTime DateDepart { get; set; }
        public PlanificationGenerationItemStatut Statut { get; set; }
        public int? IdVoyage { get; set; }
        public string? Message { get; set; }
    }

    public class PlanificationGenerationResumeDto
    {
        public int Creees { get; set; }
        public int Ignorees { get; set; }
        public int Echecs { get; set; }
    }

    public class PlanificationGenerationPeriodeDto
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class PlanificationGenerationPlanifSummaryDto
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    public class PlanificationGenerationResultDto
    {
        public int IdGeneration { get; set; }
        public PlanificationGenerationPlanifSummaryDto Planification { get; set; } = new();
        public PlanificationGenerationPeriodeDto Periode { get; set; } = new();
        public PlanificationGenerationResumeDto Resume { get; set; } = new();
        public List<string> Avertissements { get; set; } = new();
        public List<PlanificationGenerationDetailDto> Details { get; set; } = new();
    }
}
