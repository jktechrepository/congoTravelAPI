using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Une étape du trajet (référence table <c>Destinations</c>), avec ordre sur le parcours.
    /// </summary>
    public class CreateVoyageEtapeDto
    {
        /// <summary>Position dans le parcours (1 = première étape).</summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Ordre { get; set; }

        /// <summary>Identifiant dans le référentiel <c>Destinations</c>.</summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int IdDestination { get; set; }
    }
}
