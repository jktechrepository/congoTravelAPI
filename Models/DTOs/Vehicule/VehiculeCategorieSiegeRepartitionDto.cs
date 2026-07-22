namespace CongoTravel.Models.DTOs
{
    /// <summary>Répartition effective des sièges actifs d'un véhicule par catégorie.</summary>
    public class VehiculeCategorieSiegeRepartitionDto
    {
        public int IdCategorieSiege { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string CodeCategorieSiege { get; set; } = string.Empty;
        public int NombreSiegeParCategorie { get; set; }
    }
}
