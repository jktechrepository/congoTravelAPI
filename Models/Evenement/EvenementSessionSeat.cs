using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Models.Evenement
{
    public class EvenementSessionSeat
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementSessionSeat { get; set; }

        [Required]
        public int IdEvenementSession { get; set; }

        [Required]
        [MaxLength(50)]
        public string SeatCode { get; set; } = string.Empty;

        public int? IdEvenementSessionSection { get; set; }

        public int? IdEvenementClasse { get; set; }

        [Required]
        public EvenementSessionSeatStatus SeatStatus { get; set; } = EvenementSessionSeatStatus.Available;

        public int? IdEvenementReservationCourante { get; set; }

        public DateTime? HoldExpireAtUtc { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [JsonIgnore]
        [ValidateNever]
        public EvenementSession? Session { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementSessionSection? Section { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementClasse? Classe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public EvenementReservation? ReservationCourante { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementReservationLine> ReservationLines { get; set; } = new List<EvenementReservationLine>();
    }
}
