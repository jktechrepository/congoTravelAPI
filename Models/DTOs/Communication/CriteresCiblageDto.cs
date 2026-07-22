namespace CongoTravel.Models.DTOs.Communication
{
    /// <summary>
    /// DTO représentant les critères de ciblage pour une campagne de communication
    /// </summary>
    public class CriteresCiblageDto
    {
        /// <summary>
        /// Liste des IDs de catégories clients à cibler
        /// </summary>
        public int[]? IdCategorieClients { get; set; }

        /// <summary>
        /// Filtrer uniquement les clients actifs (IsActif = true)
        /// </summary>
        public bool? ClientsActifs { get; set; }

        /// <summary>
        /// Identifiant de la société (optionnel)
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Liste des usages à cibler
        /// </summary>
        public string[]? Usage { get; set; }

        /// <summary>
        /// Liste spécifique d'IDs clients (si fourni, les autres critères sont ignorés)
        /// </summary>
        public int[]? ListeIdClients { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Nombre minimum de factures en arriérés (inclusif)
        /// Si spécifié, seuls les clients ayant au moins ce nombre de factures avec MontantDu > 0 seront ciblés
        /// </summary>
        public int? NombreFacturesArrieresMin { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Nombre maximum de factures en arriérés (inclusif)
        /// Si spécifié, seuls les clients ayant au plus ce nombre de factures avec MontantDu > 0 seront ciblés
        /// </summary>
        public int? NombreFacturesArrieresMax { get; set; }
    }
}

