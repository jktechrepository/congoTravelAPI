using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    /// <summary>Template récurrent pour génération batch d'inventaire (Class ou Global).</summary>
    public class HotelPlanification
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelPlanification { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdHotel { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>Jours de la semaine (.NET DayOfWeek: 0=Dimanche … 6=Samedi), stockés en JSON.</summary>
        [Required]
        public List<int> JoursSemaine { get; set; } = new();

        [Required]
        public HotelInventoryMode InventoryMode { get; set; } = HotelInventoryMode.ClassQuota;

        [MaxLength(3)]
        public string? CodeDevise { get; set; }

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Hotel? Hotel { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public HotelPlanifGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<HotelPlanificationLigne> Lignes { get; set; } = new List<HotelPlanificationLigne>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<HotelNightAllotment> AllotmentsGeneres { get; set; } = new List<HotelNightAllotment>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<HotelNight> NightsGenerees { get; set; } = new List<HotelNight>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<HotelPlanifGenerationLog> GenerationLogs { get; set; } = new List<HotelPlanifGenerationLog>();
    }
}
