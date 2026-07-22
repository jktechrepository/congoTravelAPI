namespace CongoTravel.Models.DTOs.FeuilleDeRoute
{
    /// <summary>En-tête de feuille de route (sans lignes passagers) pour listes / historique.</summary>
    public class FeuilleDeRouteListItemDto
    {
        public int IdFeuilleDeRoute { get; set; }

        public int IdSociete { get; set; }

        public int IdVoyage { get; set; }

        public DateTime DateEmbarquement { get; set; }

        public DateTime DateGenerationUtc { get; set; }

        public int? IdUtilisateurGeneration { get; set; }

        public string? SocieteNom { get; set; }

        public string? DestinationLibelle { get; set; }

        public DateTime VoyageDateDepart { get; set; }

        public TimeSpan VoyageHeureDepart { get; set; }

        public string? VehiculeImmatriculation { get; set; }

        public string? VehiculeAlias { get; set; }

        public string? SiteNom { get; set; }

        public int NombrePassagers { get; set; }
    }
}
