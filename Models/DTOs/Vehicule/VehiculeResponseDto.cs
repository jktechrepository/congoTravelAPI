namespace CongoTravel.Models.DTOs
{
    public class VehiculeResponseDto
    {
        public int IdVehicule { get; set; }
        public string? Marques { get; set; }
        public string AliasVehicule { get; set; } = string.Empty;
        public int IdTypeVehicule { get; set; }
        public string? LibelleTypeVehicule { get; set; }
        public int NombreSiege { get; set; }
        public int IdSociete { get; set; }
        public string? NomSociete { get; set; }
        public string NumeroDePlaque { get; set; } = string.Empty;
        public List<PhotoVehiculeDto> Photos { get; set; } = new();
        public bool? Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        /// <summary>Répartition des sièges actifs par catégorie (source : table Siege).</summary>
        public List<VehiculeCategorieSiegeRepartitionDto> RepartitionCategorieSieges { get; set; } = new();
    }
}
