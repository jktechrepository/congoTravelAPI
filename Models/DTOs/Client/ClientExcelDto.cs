namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// DTO représentant les données enrichies avec les IDs après validation
    /// </summary>
    public class ClientExcelDto
    {
        public int NumeroLigne { get; set; }
        public string? NomClient { get; set; }
        public string? AdresseClient { get; set; }
        public string? Telephone { get; set; }
        public string? EmailClient { get; set; }
        public string? GenreClient { get; set; }
        public string? CodeCons { get; set; }
        public List<UsageInfo> Usages { get; set; } = new List<UsageInfo>(); // Liste des usages avec nombreBatiment
        public List<string> Erreurs { get; set; } = new List<string>();
        
        /// <summary>
        /// Information sur un usage pour l'import Excel
        /// </summary>
        public class UsageInfo
        {
            public int IdUsage { get; set; }
            public string Libelle { get; set; } = string.Empty;
            public int nombreBatiment { get; set; } = 1;
        }
    }
}
