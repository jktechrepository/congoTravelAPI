using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantCreatePlanificationGlobalQuotaDto
    {
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }

    public class RestaurantCreatePlanificationZoneQuotaDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurantZone { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PrixUnitaire { get; set; }
    }

    public class RestaurantCreatePlanificationPlageDto : IValidatableObject
    {
        public int Ordre { get; set; }

        [MaxLength(120)]
        public string? Libelle { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public RestaurantCreatePlanificationGlobalQuotaDto? GlobalQuota { get; set; }
        public List<RestaurantCreatePlanificationZoneQuotaDto>? ZoneQuotas { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult(
                    "EndTime doit être strictement postérieur à StartTime.",
                    new[] { nameof(EndTime) });
            }
        }
    }

    public class RestaurantCreatePlanificationRequestDto : IValidatableObject
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurant { get; set; }

        [Required]
        [MinLength(1)]
        public List<int> JoursSemaine { get; set; } = new();

        [Required]
        public RestaurantInventoryMode InventoryMode { get; set; } = RestaurantInventoryMode.GlobalQuota;

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string CodeDevise { get; set; } = "CDF";

        [Range(0, double.MaxValue)]
        public decimal? MontantAcompte { get; set; }

        public bool Statut { get; set; } = true;

        [Required]
        [MinLength(1)]
        public List<RestaurantCreatePlanificationPlageDto> Plages { get; set; } = new();

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

            if (Plages == null || Plages.Count == 0)
            {
                yield return new ValidationResult(
                    "Au moins une plage horaire est requise.",
                    new[] { nameof(Plages) });
                yield break;
            }

            for (var i = 0; i < Plages.Count; i++)
            {
                var plage = Plages[i];
                if (InventoryMode == RestaurantInventoryMode.GlobalQuota)
                {
                    if (plage.GlobalQuota == null)
                    {
                        yield return new ValidationResult(
                            $"GlobalQuota est obligatoire pour la plage index {i} (InventoryMode GlobalQuota).",
                            new[] { nameof(Plages) });
                    }
                    else if (plage.GlobalQuota.CapaciteTotale <= 0)
                    {
                        yield return new ValidationResult(
                            $"CapaciteTotale doit être strictement positive (plage index {i}).",
                            new[] { nameof(Plages) });
                    }
                }

                if (InventoryMode == RestaurantInventoryMode.ClassQuota)
                {
                    if (plage.ZoneQuotas == null || plage.ZoneQuotas.Count == 0)
                    {
                        yield return new ValidationResult(
                            $"ZoneQuotas est obligatoire pour la plage index {i} (InventoryMode ClassQuota).",
                            new[] { nameof(Plages) });
                    }
                    else
                    {
                        var dup = plage.ZoneQuotas
                            .GroupBy(q => q.IdRestaurantZone)
                            .Where(g => g.Count() > 1)
                            .Select(g => g.Key)
                            .ToList();
                        if (dup.Count > 0)
                        {
                            yield return new ValidationResult(
                                $"ZoneQuotas contient un doublon de zone (plage index {i}) : {string.Join(", ", dup)}.",
                                new[] { nameof(Plages) });
                        }
                    }
                }
            }
        }
    }

    public class RestaurantUpdatePlanificationRequestDto : RestaurantCreatePlanificationRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdRestaurantPlanification { get; set; }
    }

    public class RestaurantPlanificationGlobalQuotaResponseDto
    {
        public int CapaciteTotale { get; set; }
        public decimal PrixUnitaire { get; set; }
    }

    public class RestaurantPlanificationZoneQuotaResponseDto
    {
        public int IdRestaurantPlanifPlageZoneQuota { get; set; }
        public int IdRestaurantZone { get; set; }
        public string? ZoneLibelle { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixUnitaire { get; set; }
    }

    public class RestaurantPlanificationPlageResponseDto
    {
        public int IdRestaurantPlanificationPlage { get; set; }
        public int Ordre { get; set; }
        public string? Libelle { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public RestaurantPlanificationGlobalQuotaResponseDto? GlobalQuota { get; set; }
        public List<RestaurantPlanificationZoneQuotaResponseDto> ZoneQuotas { get; set; } = new();
    }

    public class RestaurantPlanificationListItemDto
    {
        public int IdRestaurantPlanification { get; set; }
        public int IdSociete { get; set; }
        public int IdRestaurant { get; set; }
        public string? RestaurantNom { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public List<int> JoursSemaine { get; set; } = new();
        public RestaurantInventoryMode InventoryMode { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public bool Statut { get; set; }
        public int NombrePlages { get; set; }
        public int NombreCreneauxGeneres { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class RestaurantPlanificationResponseDto : RestaurantPlanificationListItemDto
    {
        public decimal? MontantAcompte { get; set; }
        public List<RestaurantPlanificationPlageResponseDto> Plages { get; set; } = new();
    }

    public class GenererRestaurantPlanificationDto : IValidatableObject
    {
        [Required]
        public PlanificationGenerationMode Mode { get; set; } = PlanificationGenerationMode.MoisCourant;

        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Si true, chaque créneau nouvellement créé est publié immédiatement.
        /// Défaut false : les créneaux restent Draft. Les créneaux déjà existants (ignorés) ne sont pas republishés.
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

    public class RestaurantPlanificationGenerationDetailDto
    {
        public DateOnly DateService { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public PlanificationGenerationItemStatut Statut { get; set; }
        public int? IdCreneau { get; set; }
        public string? Message { get; set; }

        /// <summary>True uniquement si <c>publierApresGeneration</c> et publish réussi pour ce créneau créé.</summary>
        public bool Publiee { get; set; }
    }

    public class RestaurantPlanificationGenerationResumeDto
    {
        public int Creees { get; set; }
        public int Ignorees { get; set; }
        public int Echecs { get; set; }

        /// <summary>Nombre de créneaux créés puis publiés avec succès (flag opt-in).</summary>
        public int Publiees { get; set; }
    }

    public class RestaurantPlanificationGenerationPeriodeDto
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class RestaurantPlanificationGenerationPlanifSummaryDto
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    public class RestaurantPlanificationGenerationResultDto
    {
        public int IdGeneration { get; set; }
        public RestaurantPlanificationGenerationPlanifSummaryDto Planification { get; set; } = new();
        public RestaurantPlanificationGenerationPeriodeDto Periode { get; set; } = new();
        public RestaurantPlanificationGenerationResumeDto Resume { get; set; } = new();
        public List<RestaurantPlanificationGenerationDetailDto> Details { get; set; } = new();
    }
}
