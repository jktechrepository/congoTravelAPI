using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO du dashboard Caissier adapté au workflow de transport
    /// Maintient la compatibilité API tout en utilisant les entités de transport
    /// </summary>
    public class CaissierDashboardDto
    {
        public CaissierStatistiquesDto StatistiquesJournalieres { get; set; } = new();
        public List<PaiementEnCoursDto> PaiementsEnCours { get; set; } = new();
        public List<PaiementRecentDto> PaiementsRecents { get; set; } = new();
        public List<RecetteJournaliereDto> RecettesJournalieres { get; set; } = new();
        public List<AlerteCaissierDto> AlertesCaissier { get; set; } = new();
        public ResumeCaisseDto ResumeCaisse { get; set; } = new();
        public CaissierPerformancesMensuellesDto PerformancesMensuelles { get; set; } = new();
        public string CodeDevisePrincipale { get; set; } = "CDF";
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    /// <summary>Performances du caissier : mois UTC en cours vs mois précédent.</summary>
    public class CaissierPerformancesMensuellesDto
    {
        public CaissierPeriodeStatistiquesDto MoisEnCours { get; set; } = new();
        public CaissierPeriodeStatistiquesDto MoisPrecedent { get; set; } = new();
        public CaissierPerformancesMensuellesSyntheseDto Synthese { get; set; } = new();
    }

    /// <summary>KPIs caissier sur une période mensuelle UTC.</summary>
    public class CaissierPeriodeStatistiquesDto
    {
        public DateTime PeriodeDebut { get; set; }
        public DateTime PeriodeFin { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public decimal TotalEncaissements { get; set; }
        public int NombreTransactions { get; set; }
        public decimal MoyenneTransaction { get; set; }
        public int NombrePassagers { get; set; }
        public int ReservationsConfirmees { get; set; }
        public int BilletsEmis { get; set; }

        /// <summary>Jours écoulés dans le mois (renseigné uniquement pour <c>moisEnCours</c>).</summary>
        public int? JoursEcoules { get; set; }

        /// <summary>Moyenne journalière d'encaissements (renseignée uniquement pour <c>moisEnCours</c>).</summary>
        public decimal? MoyenneEncaissementsJournaliers { get; set; }

        public decimal RecetteEspece { get; set; }
        public decimal RecetteMobileMoney { get; set; }
        public decimal RecetteVirement { get; set; }
        public decimal RecetteCarte { get; set; }
        public decimal RecetteAutre { get; set; }
    }

    /// <summary>Variations % entre le mois en cours et le mois précédent.</summary>
    public class CaissierPerformancesMensuellesSyntheseDto
    {
        public decimal VariationEncaissementsPourcentage { get; set; }
        public decimal VariationTransactionsPourcentage { get; set; }
        public decimal VariationReservationsPourcentage { get; set; }
        public decimal VariationBilletsEmisPourcentage { get; set; }
    }

    /// <summary>
    /// Statistiques journalières du caissier pour le transport
    /// </summary>
    public class CaissierStatistiquesDto
    {
        /// <summary>
        /// Total des revenus de transport du jour
        /// </summary>
        public decimal TotalRevenusTransport { get; set; }

        /// <summary>
        /// Nombre total de transactions (paiements) du jour
        /// </summary>
        public int NombreTransactions { get; set; }

        /// <summary>
        /// Montant moyen des transactions du jour
        /// </summary>
        public decimal MoyenneTransaction { get; set; }

        /// <summary>
        /// Plus gros montant payé du jour
        /// </summary>
        public decimal PlusGrosMontant { get; set; }

        /// <summary>
        /// Plus petit montant payé du jour
        /// </summary>
        public decimal PlusPetitMontant { get; set; }

        /// <summary>
        /// Nombre de personnes (lignes passager ou places réservées) sur les réservations payées aujourd'hui par le caissier.
        /// </summary>
        public int NombrePassagers { get; set; }

        /// <summary>
        /// Total des réservations non payées (remplace les arriérés)
        /// </summary>
        public decimal TotalReservationsNonPayees { get; set; }

        /// <summary>
        /// Nombre de billets vendus aujourd'hui
        /// </summary>
        [Obsolete("Utiliser reservationsConfirmeesJour — compte les réservations confirmées, pas la table Billets.")]
        public int NombreBilletsVendus { get; set; }

        /// <summary>Réservations confirmées créées aujourd'hui par le caissier (sémantique exacte du KPI).</summary>
        public int ReservationsConfirmeesJour { get; set; }

        /// <summary>Billets réellement émis aujourd'hui (table Billets) pour les réservations du caissier.</summary>
        public int BilletsEmisJour { get; set; }

        /// <summary>
        /// Taux de remplissage moyen des bus aujourd'hui
        /// </summary>
        public decimal TauxRemplissageMoyen { get; set; }
    }

    /// <summary>
    /// DTO pour les paiements en cours liés aux réservations de transport
    /// </summary>
    public class PaiementEnCoursDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomPassager { get; set; } = string.Empty;
        public decimal MontantAPaye { get; set; }
        public decimal MontantVerse { get; set; }
        public decimal ResteAPayer { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur la réservation associée
        /// </summary>
        public int IdReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur le voyage
        /// </summary>
        public string VoyageInfo { get; set; } = string.Empty;
        public DateTime DateVoyage { get; set; }
    }

    /// <summary>
    /// DTO pour les paiements récents liés aux réservations de transport
    /// </summary>
    public class PaiementRecentDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomPassager { get; set; } = string.Empty;
        public decimal MontantPaye { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string UtilisateurEnregistrement { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur la réservation associée
        /// </summary>
        public int IdReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur le voyage
        /// </summary>
        public string VoyageInfo { get; set; } = string.Empty;
        public DateTime DateVoyage { get; set; }
    }

    /// <summary>
    /// DTO pour les recettes journalières de transport
    /// </summary>
    public class RecetteJournaliereDto
    {
        public DateTime Date { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombreTransactions { get; set; }
        public decimal RecetteEspece { get; set; }
        public decimal RecetteMobileMoney { get; set; }
        public decimal RecetteVirement { get; set; }
        public decimal RecetteCarte { get; set; }

        /// <summary>Encaissements dont la méthode ne correspond à aucun bucket standard.</summary>
        public decimal RecetteAutre { get; set; }
        
        /// <summary>
        /// Nombre de billets vendus ce jour
        /// </summary>
        public int NombreBilletsVendus { get; set; }
        
        /// <summary>
        /// Revenus par type de bus
        /// </summary>
        public decimal RecetteVehiculeStandard { get; set; }
        public decimal RecetteVehiculeVIP { get; set; }
        
        /// <summary>
        /// Revenus par destination principale
        /// </summary>
        public decimal RecetteDestinationPrincipale { get; set; }
    }

    /// <summary>
    /// DTO pour les alertes spécifiques au transport pour le caissier
    /// </summary>
    public class AlerteCaissierDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdPassager { get; set; }
        public string NomPassager { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
        
        /// <summary>
        /// Informations sur la réservation concernée
        /// </summary>
        public int? IdReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations sur le voyage concerné
        /// </summary>
        public DateTime DateVoyage { get; set; }
        public string Destination { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour le résumé de caisse adapté au transport
    /// </summary>
    public class ResumeCaisseDto
    {
        public decimal TotalEntrees { get; set; }
        public DateTime DateCloture { get; set; }
        public string StatutCaisse { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre total de billets vendus dans la journée
        /// </summary>
        [Obsolete("Utiliser reservationsConfirmeesJour ou billetsEmisJour.")]
        public int TotalBilletsVendus { get; set; }

        /// <summary>Réservations confirmées du jour par le caissier.</summary>
        public int ReservationsConfirmeesJour { get; set; }

        /// <summary>Billets émis (table Billets) liés aux réservations du caissier aujourd'hui.</summary>
        public int BilletsEmisJour { get; set; }
        
        /// <summary>
        /// Nombre de réservations confirmées
        /// </summary>
        public int ReservationsConfirmees { get; set; }
        
        /// <summary>
        /// Nombre de réservations en attente de paiement
        /// </summary>
        public int ReservationsEnAttente { get; set; }
        
        /// <summary>
        /// Taux de remplissage moyen des bus traités
        /// </summary>
        public decimal TauxRemplissageMoyen { get; set; }
    }
}
