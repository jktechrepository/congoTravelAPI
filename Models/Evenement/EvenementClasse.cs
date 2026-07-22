using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Evenement
{
    public class EvenementClasse
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementClasse { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodeClasse { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionClassQuota> SessionClassQuotas { get; set; } = new List<EvenementSessionClassQuota>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionSeat> SessionSeats { get; set; } = new List<EvenementSessionSeat>();
    }
}
