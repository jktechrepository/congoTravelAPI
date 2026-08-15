using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    public abstract class Adresse
    {
        public string? Province { get; set; }
        public string? Ville { get; set; }
        public string? Commune { get; set; }
        public string? Quartier { get; set; }
        public string? Avenue { get; set; }
        public string? Numero { get; set; }
    }
}
