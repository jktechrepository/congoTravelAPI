namespace CongoTravel.Models.DTOs.FeuilleDeRoute
{
    /// <summary>Feuille de route complète : infos société, voyage et passagers embarqués.</summary>
    public class FeuilleDeRouteDetailDto
    {
        public int IdFeuilleDeRoute { get; set; }

        public int IdSociete { get; set; }

        public int IdVoyage { get; set; }

        public DateTime DateEmbarquement { get; set; }

        public DateTime DateGenerationUtc { get; set; }

        public int? IdUtilisateurGeneration { get; set; }

        // Société
        public string? SocieteNom { get; set; }

        public string? SocieteTelephone { get; set; }

        public string? SocieteEmail { get; set; }

        public string? SocieteAdresse { get; set; }

        public string? SocieteLogo { get; set; }

        // Voyage
        public DateTime VoyageDateDepart { get; set; }

        public TimeSpan VoyageHeureDepart { get; set; }

        public int VoyagePrix { get; set; }

        public string VoyageCodeDevise { get; set; } = "CDF";

        public int IdDestination { get; set; }

        public string? DestinationLibelle { get; set; }

        public int IdVehicule { get; set; }

        public string? VehiculeImmatriculation { get; set; }

        public string? VehiculeAlias { get; set; }

        public int? IdSite { get; set; }

        public string? SiteNom { get; set; }

        public int NombrePassagers { get; set; }

        public IReadOnlyList<FeuilleDeRoutePassagerDto> Passagers { get; set; } =
            Array.Empty<FeuilleDeRoutePassagerDto>();
    }
}
