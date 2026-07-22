using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Voyage
{
    public class ReporterVoyageDto
    {
        [Required]
        public DateTime DateDepart { get; set; }

        [Required]
        public TimeSpan HeureDepart { get; set; }

        [MaxLength(500)]
        public string? Motif { get; set; }

        public bool NotifierClients { get; set; } = true;

        public bool ConfirmerAvecBilletsUtilises { get; set; }
    }
}
