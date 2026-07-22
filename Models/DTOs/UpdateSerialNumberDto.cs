using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class UpdateSerialNumberDto
    {
        [Required(ErrorMessage = "Le numéro de série est obligatoire")]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;
    }
}

