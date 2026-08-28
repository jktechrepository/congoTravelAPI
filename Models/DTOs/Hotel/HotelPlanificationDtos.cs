using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Enums;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelCreatePlanificationLigneDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdHotelRoomType { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal PrixNuit { get; set; }
    }

    public class HotelCreatePlanificationGlobalQuotaDto
    {
        [Range(1, int.MaxValue)]
        public int CapaciteTotale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PrixNuit { get; set; }
    }

    public class HotelCreatePlanificationRequestDto : IValidatableObject
    {
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int IdHotel { get; set; }

        [Required]
        [MinLength(1)]
        public List<int> JoursSemaine { get; set; } = new();

        /// <summary>Défaut ClassQuota pour compatibilité Phase 7a.</summary>
        public HotelInventoryMode InventoryMode { get; set; } = HotelInventoryMode.ClassQuota;

        [StringLength(3, MinimumLength = 3)]
        public string? CodeDevise { get; set; }

        public bool Statut { get; set; } = true;

        /// <summary>Obligatoire si InventoryMode = ClassQuota.</summary>
        public List<HotelCreatePlanificationLigneDto>? Lignes { get; set; }

        /// <summary>Obligatoire si InventoryMode = GlobalQuota.</summary>
        public HotelCreatePlanificationGlobalQuotaDto? GlobalQuota { get; set; }

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

            if (InventoryMode == HotelInventoryMode.GlobalQuota)
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

            if (InventoryMode == HotelInventoryMode.ClassQuota)
            {
                if (Lignes == null || Lignes.Count == 0)
                {
                    yield return new ValidationResult(
                        "Au moins une ligne (type de chambre × capacité × prix) est requise.",
                        new[] { nameof(Lignes) });
                }
                else
                {
                    var dup = Lignes
                        .GroupBy(q => q.IdHotelRoomType)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (dup.Count > 0)
                    {
                        yield return new ValidationResult(
                            $"Lignes contient un doublon de type de chambre : {string.Join(", ", dup)}.",
                            new[] { nameof(Lignes) });
                    }
                }
            }
        }
    }

    public class HotelUpdatePlanificationRequestDto : HotelCreatePlanificationRequestDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int IdHotelPlanification { get; set; }
    }

    public class HotelPlanificationLigneResponseDto
    {
        public int IdHotelPlanificationLigne { get; set; }
        public int IdHotelRoomType { get; set; }
        public string? CodeRoomType { get; set; }
        public string? LibelleRoomType { get; set; }
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
    }

    public class HotelPlanificationGlobalQuotaResponseDto
    {
        public int CapaciteTotale { get; set; }
        public decimal PrixNuit { get; set; }
    }

    public class HotelPlanificationListItemDto
    {
        public int IdHotelPlanification { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public string? HotelNom { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public List<int> JoursSemaine { get; set; } = new();
        public HotelInventoryMode InventoryMode { get; set; }
        public string? CodeDevise { get; set; }
        public bool Statut { get; set; }
        public int NombreAllotmentsGeneres { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class HotelPlanificationResponseDto : HotelPlanificationListItemDto
    {
        public List<HotelPlanificationLigneResponseDto> Lignes { get; set; } = new();
        public HotelPlanificationGlobalQuotaResponseDto? GlobalQuota { get; set; }
    }

    public class GenererHotelPlanificationDto : IValidatableObject
    {
        [Required]
        public PlanificationGenerationMode Mode { get; set; } = PlanificationGenerationMode.MoisCourant;

        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Si true, chaque allotment/nuit nouvellement créé est publié immédiatement.
        /// Défaut false : reste Draft. Les nuits déjà existantes (ignorées) ne sont pas republishées.
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

    public class HotelPlanificationGenerationDetailDto
    {
        public DateOnly NightDate { get; set; }
        public int IdHotelRoomType { get; set; }
        public PlanificationGenerationItemStatut Statut { get; set; }
        public int? IdHotelNightAllotment { get; set; }
        public int? IdHotelNight { get; set; }
        public string? Message { get; set; }
        public bool Publiee { get; set; }
    }

    public class HotelPlanificationGenerationResumeDto
    {
        public int Creees { get; set; }
        public int Ignorees { get; set; }
        public int Echecs { get; set; }
        public int Publiees { get; set; }
    }

    public class HotelPlanificationGenerationPeriodeDto
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
    }

    public class HotelPlanificationGenerationPlanifSummaryDto
    {
        public int Id { get; set; }
        public string Libelle { get; set; } = string.Empty;
    }

    public class HotelPlanificationGenerationResultDto
    {
        public int IdGeneration { get; set; }
        public HotelPlanificationGenerationPlanifSummaryDto Planification { get; set; } = new();
        public HotelPlanificationGenerationPeriodeDto Periode { get; set; } = new();
        public HotelPlanificationGenerationResumeDto Resume { get; set; } = new();
        public List<HotelPlanificationGenerationDetailDto> Details { get; set; } = new();
    }
}
