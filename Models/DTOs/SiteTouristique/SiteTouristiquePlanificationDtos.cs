using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Enums;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueCreatePlanificationGlobalQuotaDto
    {
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }

    public class SiteTouristiqueCreatePlanificationClassQuotaDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSiteTouristiqueClasse { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }

    public class SiteTouristiqueCreatePlanificationRequestDto : IValidatableObject
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int IdSiteTouristique { get; set; }

        [Required]
        [MinLength(1)]
        public List<int> JoursSemaine { get; set; } = new();

        [Required]
        public SiteTouristiqueInventoryMode InventoryMode { get; set; } = SiteTouristiqueInventoryMode.GlobalQuota;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevise { get; set; } = "CDF";

        public int? SalesOpenOffsetHours { get; set; }
        public int? SalesCloseOffsetHours { get; set; }

        public bool Statut { get; set; } = true;

        public SiteTouristiqueCreatePlanificationGlobalQuotaDto? GlobalQuota { get; set; }
        public List<SiteTouristiqueCreatePlanificationClassQuotaDto>? ClassQuotas { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
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

            if (InventoryMode == SiteTouristiqueInventoryMode.GlobalQuota)
            {
                if (GlobalQuota == null)
                {
                    yield return new ValidationResult(
                        "GlobalQuota est obligatoire pour InventoryMode GlobalQuota.",
                        new[] { nameof(GlobalQuota) });
                }
                else if (GlobalQuota.CapaciteTotale <= 0)
                {
                    yield return new ValidationResult(
                        "CapaciteTotale doit être strictement positive.",
                        new[] { nameof(GlobalQuota) });
                }
            }

            if (InventoryMode == SiteTouristiqueInventoryMode.ClassQuota)
            {
                if (ClassQuotas == null || ClassQuotas.Count == 0)
                {
                    yield return new ValidationResult(
                        "ClassQuotas est obligatoire pour InventoryMode ClassQuota.",
                        new[] { nameof(ClassQuotas) });
                }
                else
                {
                    var dup = ClassQuotas
                        .GroupBy(q => q.IdSiteTouristiqueClasse)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (dup.Count > 0)
                    {
                        yield return new ValidationResult(
                            $"ClassQuotas contient un doublon de classe : {string.Join(", ", dup)}.",
                            new[] { nameof(ClassQuotas) });
                    }
                }
            }
        }
    }

    public class SiteTouristiqueUpdatePlanificationRequestDto : SiteTouristiqueCreatePlanificationRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdSiteTouristiquePlanification { get; set; }
    }

    public class SiteTouristiquePlanificationGlobalQuotaResponseDto
    {
        public int CapaciteTotale { get; set; }
        public decimal PrixUnitaire { get; set; }
    }

    public class SiteTouristiquePlanificationClassQuotaResponseDto
    {
        public int IdSiteTouristiquePlanifClassQuota { get; set; }
        public int IdSiteTouristiqueClasse { get; set; }
        public string? ClasseLibelle { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixUnitaire { get; set; }
    }

    public class SiteTouristiquePlanificationListItemDto
    {
        public int IdSiteTouristiquePlanification { get; set; }
        public int IdSociete { get; set; }
        public int IdSiteTouristique { get; set; }
        public string? LieuNom { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public List<int> JoursSemaine { get; set; } = new();
        public SiteTouristiqueInventoryMode InventoryMode { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public bool Statut { get; set; }
        public int NombreJourneesGenerees { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class SiteTouristiquePlanificationResponseDto : SiteTouristiquePlanificationListItemDto
    {
        public int? SalesOpenOffsetHours { get; set; }
        public int? SalesCloseOffsetHours { get; set; }
        public SiteTouristiquePlanificationGlobalQuotaResponseDto? GlobalQuota { get; set; }
        public List<SiteTouristiquePlanificationClassQuotaResponseDto> ClassQuotas { get; set; } = new();
    }

    public class GenererSiteTouristiquePlanificationDto : IValidatableObject
    {
        [Required]
        public PlanificationGenerationMode Mode { get; set; } = PlanificationGenerationMode.MoisCourant;

        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Si true, chaque journée nouvellement créée est publiée immédiatement (même logique que PUT .../journees/{id}/publish).
        /// Défaut false : les journées restent Draft. Les dates déjà existantes (ignorées) ne sont pas republishées.
        /// </summary>
        public bool PublierApresGeneration { get; set; }

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

    public class SiteTouristiquePlanificationGenerationDetailDto
    {
        public DateOnly DateVisite { get; set; }
        public PlanificationGenerationItemStatut Statut { get; set; }
        public int? IdJournee { get; set; }
        public string? Message { get; set; }

        /// <summary>True uniquement si <c>publierApresGeneration</c> et publish réussi pour cette journée créée.</summary>
        public bool Publiee { get; set; }
    }

    public class SiteTouristiquePlanificationGenerationResumeDto
    {
        public int Creees { get; set; }
        public int Ignorees { get; set; }
        public int Echecs { get; set; }

        /// <summary>Nombre de journées créées puis publiées avec succès (flag opt-in).</summary>
        public int Publiees { get; set; }
    }

    public class SiteTouristiquePlanificationGenerationPeriodeDto
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class SiteTouristiquePlanificationGenerationPlanifSummaryDto
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    public class SiteTouristiquePlanificationGenerationResultDto
    {
        public int IdGeneration { get; set; }
        public SiteTouristiquePlanificationGenerationPlanifSummaryDto Planification { get; set; } = new();
        public SiteTouristiquePlanificationGenerationPeriodeDto Periode { get; set; } = new();
        public SiteTouristiquePlanificationGenerationResumeDto Resume { get; set; } = new();
        public List<SiteTouristiquePlanificationGenerationDetailDto> Details { get; set; } = new();
    }
}
