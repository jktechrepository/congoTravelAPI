using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace CongoTravel.Models
{
    public class TimeSpanValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (value is TimeSpan)
                return ValidationResult.Success;

            if (value is string stringValue)
            {
                // Formats acceptés
                string[] formats = { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss", "HHhmm", "Hhmm" };
                
                foreach (var format in formats)
                {
                    if (TimeSpan.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, out _))
                    {
                        return ValidationResult.Success;
                    }
                }

                // Essayer de parser avec le format standard
                if (TimeSpan.TryParse(stringValue, out _))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult("Format de temps invalide. Utilisez le format HH:mm (ex: 07:30) ou HHhmm (ex: 7h30)");
        }
    }
}
