using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Evenement
{
    public class EvenementSessionSection
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEvenementSessionSection { get; set; }

        [Required]
        public int IdEvenementSession { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodeSection { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Libelle { get; set; } = string.Empty;

        [JsonIgnore]
        [ValidateNever]
        public EvenementSession? Session { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<EvenementSessionSeat> Seats { get; set; } = new List<EvenementSessionSeat>();
    }
}
