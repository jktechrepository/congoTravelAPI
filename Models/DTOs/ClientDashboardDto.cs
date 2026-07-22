using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO du dashboard Client adapté au workflow de transport
    /// Maintient la compatibilité API tout en utilisant les entités de transport
    /// </summary>
    public class ClientDashboardDto
    {
        public ClientStatistiquesDto Statistiques { get; set; } = new();
        public List<ReservationRecenteDto> ReservationsRecentes { get; set; } = new();
        public List<PaiementClientRecentDto> PaiementsRecents { get; set; } = new();
        public List<VoyageClientDto> VoyagesClient { get; set; } = new();
        public List<AlerteClientDto> AlertesClient { get; set; } = new();
        public ResumeClientDto ResumeClient { get; set; } = new();
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Statistiques du client pour le transport
    /// </summary>
    public class ClientStatistiquesDto
    {
        /// <summary>
        /// Montant total des réservations de transport
        /// </summary>
        public decimal MontantTotalReservations { get; set; }

        /// <summary>
        /// Montant total payé pour les réservations
        /// </summary>
        public decimal MontantTotalPaye { get; set; }

        /// <summary>
        /// Montant total dû (réservations non payées)
        /// </summary>
        public decimal MontantTotalDu { get; set; }

        /// <summary>
        /// Nombre total de réservations
        /// </summary>
        public int NombreReservations { get; set; }

        /// <summary>
        /// Nombre de réservations payées
        /// </summary>
        public int NombreReservationsPayees { get; set; }

        /// <summary>
        /// Nombre de réservations en retard de paiement
        /// </summary>
        public int NombreReservationsEnRetard { get; set; }

        /// <summary>
        /// Taux de paiement des réservations
        /// </summary>
        public decimal TauxPaiement { get; set; }

        /// <summary>
        /// Nombre de voyages effectués
        /// </summary>
        public int NombreVoyagesEffectues { get; set; }

        /// <summary>
        /// Destination favorite la plus visitée
        /// </summary>
        public string DestinationFavorite { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour les réservations récentes du client
    /// </summary>
    public class ReservationRecenteDto
    {
        public int IdReservation { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string VoyageInfo { get; set; } = string.Empty;
        public decimal MontantTotal { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal MontantDu { get; set; }
        public DateTime DateReservation { get; set; }
        public DateTime DateVoyage { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string StatutPaiement { get; set; } = string.Empty;
        public int NombrePlaces { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string HeureDepart { get; set; } = string.Empty;
        public bool PossedeBillet { get; set; }
        public string QrCodeBillet { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour les paiements récents du client avec informations de réservation
    /// </summary>
    public class PaiementClientRecentDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal MontantPaye { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ReferenceReservation { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur la réservation associée
        /// </summary>
        public string VoyageInfo { get; set; } = string.Empty;
        public DateTime DateVoyage { get; set; }
        public string Destination { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour les voyages du client
    /// </summary>
    public class VoyageClientDto
    {
        public int IdVoyage { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string VilleDepart { get; set; } = string.Empty;
        public string VilleArrivee { get; set; } = string.Empty;
        public DateTime DateDepart { get; set; }
        public TimeSpan HeureDepart { get; set; }
        public decimal Prix { get; set; }
        public string TypeVehicule { get; set; } = string.Empty;
        public string StatutVoyage { get; set; } = string.Empty;
        public int NombrePlacesReservees { get; set; }
        public int NombrePlacesTotal { get; set; }
        public decimal TauxRemplissage { get; set; }
        public DateTime DateVoyageEffectue { get; set; }
        public bool EstEffectue { get; set; }
    }

    /// <summary>
    /// DTO pour les alertes spécifiques au transport pour le client
    /// </summary>
    public class AlerteClientDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
        
        /// <summary>
        /// Informations sur le voyage concerné
        /// </summary>
        public DateTime DateVoyage { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string HeureDepart { get; set; } = string.Empty;
        
        /// <summary>
        /// Action suggérée pour le client
        /// </summary>
        public string ActionSuggeree { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour le résumé du compte client adapté au transport
    /// </summary>
    public class ResumeClientDto
    {
        public string StatutCompte { get; set; } = string.Empty;
        public int NombreReservationsActives { get; set; }
        
        /// <summary>
        /// Informations spécifiques au transport
        /// </summary>
        public int NombreVoyagesCeMois { get; set; }
        public decimal DepensesCeMois { get; set; }
        
        public string DestinationFavorite { get; set; } = string.Empty;
    }
}
