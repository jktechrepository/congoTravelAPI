namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// Étape ordonnée d’un voyage (liste destinations).
    /// </summary>
    public class VoyageEtapeReadDto
    {
        public int IdVoyageDestination { get; set; }
        public int Ordre { get; set; }
        public int IdDestination { get; set; }
        public string VilleDepart { get; set; } = string.Empty;
        public string VilleArrivee { get; set; } = string.Empty;
    }

    /// <summary>
    /// Siège du bus du voyage encore libre pour ce voyage (pas d’allocation CONFIRME).
    /// </summary>
    public class SiegeLibreReadDto
    {
        public int IdSiege { get; set; }
        public int NumeroOrdre { get; set; }
        public string CodeSiege { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sièges disponibles pour un voyage, regroupés par catégorie.
    /// </summary>
    public class VoyageSiegesDisponiblesResponseDto
    {
        public int IdVoyage { get; set; }
        public int NombreSiegesDisponibles { get; set; }
        public List<VoyageCategorieSiegeDisponiblesDto> RepartitionCategorieSieges { get; set; } = new();
    }

    /// <summary>
    /// Résumé des sièges libres par catégorie (sans détail siège par siège).
    /// </summary>
    public class VoyageCategorieSiegeDisponiblesSummaryDto
    {
        public int IdCategorieSiege { get; set; }
        public string CodeCategorieSiege { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        /// <summary>Nombre de sièges disponibles dans cette catégorie pour ce voyage.</summary>
        public int NombreSiege { get; set; }
    }

    /// <summary>
    /// Sièges libres d’une catégorie pour un voyage donné.
    /// </summary>
    public class VoyageCategorieSiegeDisponiblesDto
    {
        public int IdCategorieSiege { get; set; }
        public string CodeCategorieSiege { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        /// <summary>Nombre de sièges disponibles dans cette catégorie pour ce voyage.</summary>
        public int NombreSiege { get; set; }
        public List<SiegeLibreReadDto> Sieges { get; set; } = new();
    }

    /// <summary>
    /// Siège déjà attribué sur ce voyage (allocation CONFIRME).
    /// </summary>
    public class SiegeIndisponibleReadDto
    {
        public int IdSiege { get; set; }
        public int NumeroOrdre { get; set; }
        public string CodeSiege { get; set; } = string.Empty;
        public int IdVoyageSeatAllocation { get; set; }
        public int IdReservationPassenger { get; set; }
        public string NomPassager { get; set; } = string.Empty;
    }
}
