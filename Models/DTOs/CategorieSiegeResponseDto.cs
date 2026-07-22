namespace CongoTravel.Models.DTOs
{
    /// <summary>Réponse API pour une ligne du référentiel catégories de siège.</summary>
    public class CategorieSiegeResponseDto
    {
        public int IdCategorieSiege { get; set; }
        public int IdSociete { get; set; }
        public string CodeCategorieSiege { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public bool Statut { get; set; }
    }
}
