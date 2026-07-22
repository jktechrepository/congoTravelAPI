namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// Résultat de l'import en masse de clients depuis Excel
    /// </summary>
    public class BulkClientResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalLignes { get; set; }
        public int LignesReussies { get; set; }
        public int LignesEchouees { get; set; }
        public int DoublonsDetectes { get; set; }
        public List<LigneErreurClient> LignesAvecErreurs { get; set; } = new List<LigneErreurClient>();
        public List<ClientCree> ClientsCrees { get; set; } = new List<ClientCree>();
        public DateTime DateTraitement { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Représente une ligne avec des erreurs
    /// </summary>
    public class LigneErreurClient
    {
        public int NumeroLigne { get; set; }
        public string? NomClient { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Représente un client créé avec succès
    /// </summary>
    public class ClientCree
    {
        public bool Success { get; set; }
        public int IdClient { get; set; }
        public string? NomClient { get; set; }
        public string? Message { get; set; }
    }
}
