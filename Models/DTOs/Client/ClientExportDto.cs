using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// DTO pour l'export des clients avec leurs usages
    /// </summary>
    public class ClientExportDto
    {
        /// <summary>
        /// ID du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom complet du client
        /// </summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Adresse du client
        /// </summary>
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Téléphone du client
        /// </summary>
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        public string? EmailClient { get; set; }

        /// <summary>
        /// Genre du client
        /// </summary>
        public string? GenreClient { get; set; }

        /// <summary>
        /// Code consommateur
        /// </summary>
        public string? CodeCons { get; set; }

        /// <summary>
        /// Statut du client
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Client actif
        /// </summary>
        public bool IsActif { get; set; }

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Code de l'axe
        /// </summary>
        public string? CodeAxe { get; set; }

        /// <summary>
        /// Nom de l'axe
        /// </summary>
        public string? NomAxe { get; set; }

        /// <summary>
        /// Description de l'axe
        /// </summary>
        public string? DescriptionAxe { get; set; }

        /// <summary>
        /// Code de la cabine
        /// </summary>
        public string? CodeCabine { get; set; }

        /// <summary>
        /// Nom de la cabine
        /// </summary>
        public string? NomCabine { get; set; }

        /// <summary>
        /// Description de la cabine
        /// </summary>
        public string? DescriptionCabine { get; set; }

        /// <summary>
        /// Liste des usages du client (séparés par des points-virgules)
        /// </summary>
        public string UsagesLibelles { get; set; } = string.Empty;

        /// <summary>
        /// Montants unitaires des usages (séparés par des points-virgules)
        /// </summary>
        public string UsagesMontants { get; set; } = string.Empty;

        /// <summary>
        /// Catégories des usages (séparées par des points-virgules)
        /// </summary>
        public string UsagesCategories { get; set; } = string.Empty;

        /// <summary>
        /// Nombre total d'usages
        /// </summary>
        public int NombreUsages { get; set; }
    }

    /// <summary>
    /// Paramètres de requête pour l'export des clients
    /// </summary>
    public class ClientExportRequest
    {
        /// <summary>
        /// Type de fichier d'export (excel, pdf)
        /// </summary>
        public string FileType { get; set; } = "excel";

        /// <summary>
        /// Filtre optionnel par Axe
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Inclure les clients inactifs
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Terme de recherche optionnel
        /// </summary>
        public string? SearchTerm { get; set; }
    }
}
