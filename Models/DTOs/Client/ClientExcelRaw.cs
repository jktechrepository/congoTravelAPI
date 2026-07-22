namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// DTO représentant les données brutes lues depuis le fichier Excel
    /// </summary>
    public class ClientExcelRaw
    {
        public string? NomClient { get; set; }
        public string? AdresseClient { get; set; }
        public string? Telephone { get; set; }
        public string? EmailClient { get; set; }
        public string? GenreClient { get; set; }
        public string? CodeCons { get; set; }
        public string? LibelleUsage { get; set; } // Format: "Usage1, Usage2" ou "Usage1; Usage2"
    }
}
