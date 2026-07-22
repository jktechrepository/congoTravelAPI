namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour la recherche et le filtrage des paiements
    /// </summary>
    public class PaiementSearchDto
    {
        /// <summary>
        /// Terme de recherche (référence transaction, méthode paiement, etc.)
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filtre par statut du paiement
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>
        /// Filtre par méthode de paiement
        /// </summary>
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Filtre par identifiant de l'utilisateur
        /// </summary>
        public int? IdUtilisateur { get; set; }

        /// <summary>
        /// Filtre par identifiant de la réservation
        /// </summary>
        public int? IdReservation { get; set; }

        /// <summary>
        /// Filtre par identifiant de la société
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Filtre par date de début (création)
        /// </summary>
        public DateTime? DateDebut { get; set; }

        /// <summary>
        /// Filtre par date de fin (création)
        /// </summary>
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Filtre pour les paiements avec reste à payer
        /// </summary>
        public bool? AvecResteAPaye { get; set; }

        /// <summary>
        /// Filtre pour les paiements complètement payés
        /// </summary>
        public bool? PayesComplet { get; set; }

        /// <summary>
        /// Inclure les paiements supprimés (soft delete)
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;
    }
}
