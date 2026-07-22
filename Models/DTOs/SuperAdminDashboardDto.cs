using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO représentant le dashboard Super-Admin avec vue globale multi-sociétés
    /// </summary>
    public class SuperAdminDashboardDto
    {
        /// <summary>
        /// Statistiques globales toutes sociétés confondues
        /// </summary>
        public GlobalStatistiquesDto GlobalStatistiques { get; set; } = new();

        /// <summary>
        /// Liste des sociétés avec leurs statistiques individuelles
        /// </summary>
        public List<SocieteSummaryDto> Societes { get; set; } = new();

        /// <summary>
        /// Top 5 des sociétés par chiffre d'affaires
        /// </summary>
        public List<TopSocieteDto> Top5SocietesCA { get; set; } = new();

        /// <summary>
        /// Top 5 des sociétés par taux de recouvrement
        /// </summary>
        public List<TopSocieteDto> Top5SocietesRecouvrement { get; set; } = new();

        /// <summary>
        /// Alertes critiques à traiter
        /// </summary>
        public List<AlerteCritiqueDto> AlertesCritiques { get; set; } = new();

        /// <summary>
        /// Tendances sur les 12 derniers mois
        /// </summary>
        public TendancesDto Tendances { get; set; } = new();

        /// <summary>
        /// Statistiques des utilisateurs par rôle
        /// </summary>
        public UtilisateursStatistiquesDto UtilisateursStatistiques { get; set; } = new();

        /// <summary>
        /// Date de génération du dashboard
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Statistiques globales toutes sociétés confondues
    /// </summary>
    public class GlobalStatistiquesDto
    {
        /// <summary>
        /// Nombre total de sociétés (actives + inactives)
        /// </summary>
        public int TotalSocietes { get; set; }

        /// <summary>
        /// Nombre de sociétés actives
        /// </summary>
        public int SocietesActives { get; set; }

        /// <summary>
        /// Nombre total de clients toutes sociétés confondues
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre total de clients actifs
        /// </summary>
        public int ClientsActifs { get; set; }

        /// <summary>
        /// Chiffre d'affaires global toutes sociétés
        /// </summary>
        public decimal ChiffreAffairesGlobal { get; set; }

        /// <summary>
        /// Montant total des arriérés global
        /// </summary>
        public decimal MontantTotalArrieresGlobal { get; set; }

        /// <summary>
        /// Montant total des paiements enregistrés
        /// </summary>
        public decimal MontantTotalPaiementsGlobal { get; set; }

        /// <summary>
        /// Taux de recouvrement global
        /// </summary>
        public decimal TauxRecouvrementGlobal { get; set; }

        /// <summary>
        /// Nombre total de factures émises
        /// </summary>
        public int TotalFactures { get; set; }

        /// <summary>
        /// Nombre total de paiements enregistrés
        /// </summary>
        public int TotalPaiements { get; set; }
    }

    /// <summary>
    /// Résumé d'une société pour le dashboard Super-Admin
    /// </summary>
    public class SocieteSummaryDto
    {
        /// <summary>
        /// Identifiant de la société
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Nom de la société
        /// </summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Ville de la société
        /// </summary>
        public string? Ville { get; set; }

        /// <summary>
        /// Type de société (Privée, Publique, Conventionnée)
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Devise utilisée par la société
        /// </summary>
        public string Devise { get; set; } = string.Empty;

        /// <summary>
        /// Statut de la société (active/inactive)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Nombre de clients actifs
        /// </summary>
        public int NombreClientsActifs { get; set; }

        /// <summary>
        /// Chiffre d'affaires du mois
        /// </summary>
        public decimal ChiffreAffairesMois { get; set; }

        /// <summary>
        /// Montant total des arriérés
        /// </summary>
        public decimal MontantArrieres { get; set; }

        /// <summary>
        /// Taux de recouvrement
        /// </summary>
        public decimal TauxRecouvrement { get; set; }

        /// <summary>
        /// Nombre total d'utilisateurs
        /// </summary>
        public int NombreUtilisateurs { get; set; }

        /// <summary>
        /// Date de dernière activité
        /// </summary>
        public DateTime? DerniereActivite { get; set; }

        /// <summary>
        /// Performance globale (score de 0 à 100)
        /// </summary>
        public decimal ScorePerformance { get; set; }
    }

    /// <summary>
    /// Top des sociétés par critère
    /// </summary>
    public class TopSocieteDto
    {
        /// <summary>
        /// Rang dans le classement
        /// </summary>
        public int Rang { get; set; }

        /// <summary>
        /// Identifiant de la société
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Nom de la société
        /// </summary>
        public string Nom { get; set; } = string.Empty;

        /// <summary>
        /// Valeur du critère (CA, taux recouvrement, etc.)
        /// </summary>
        public decimal Valeur { get; set; }

        /// <summary>
        /// Variation par rapport au mois précédent (en %)
        /// </summary>
        public decimal VariationMoisPrecedent { get; set; }
    }

    /// <summary>
    /// Alerte critique pour le Super-Admin
    /// </summary>
    public class AlerteCritiqueDto
    {
        /// <summary>
        /// Identifiant de l'alerte
        /// </summary>
        public int IdAlerte { get; set; }

        /// <summary>
        /// Type d'alerte
        /// </summary>
        public string TypeAlerte { get; set; } = string.Empty;

        /// <summary>
        /// Niveau de criticité (Critique, Élevée, Moyenne)
        /// </summary>
        public string NiveauCriticite { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'alerte
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de la société concernée
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Nom de la société concernée
        /// </summary>
        public string? NomSociete { get; set; }

        /// <summary>
        /// Date de déclenchement de l'alerte
        /// </summary>
        public DateTime DateAlerte { get; set; }

        /// <summary>
        /// Statut de l'alerte (Non lue, En cours, Résolue)
        /// </summary>
        public string Statut { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tendances sur les 12 derniers mois
    /// </summary>
    public class TendancesDto
    {
        public List<TendanceMensuelleDto> EvolutionChiffreAffaires { get; set; } = new();
        public List<TendanceMensuelleDto> EvolutionTauxRecouvrement { get; set; } = new();
        public List<TendanceMensuelleDto> EvolutionNombreClients { get; set; } = new();
        public List<TendanceMensuelleDto> EvolutionMontantArrieres { get; set; } = new();
        public List<TendanceMensuelleDto> EvolutionTauxPaiement { get; set; } = new();
        public List<TendanceMensuelleDto> EvolutionReservationsConfirmees { get; set; } = new();
    }

    /// <summary>
    /// Donnée de tendance mensuelle
    /// </summary>
    public class TendanceMensuelleDto
    {
        /// <summary>
        /// Mois (format: "2024-01")
        /// </summary>
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année
        /// </summary>
        public int Annee { get; set; }

        /// <summary>
        /// Valeur pour le mois
        /// </summary>
        public decimal Valeur { get; set; }

        /// <summary>
        /// Variation par rapport au mois précédent (en %)
        /// </summary>
        public decimal Variation { get; set; }
    }

    /// <summary>
    /// Statistiques des utilisateurs par rôle
    /// </summary>
    public class UtilisateursStatistiquesDto
    {
        /// <summary>
        /// Nombre total d'utilisateurs
        /// </summary>
        public int TotalUtilisateurs { get; set; }

        /// <summary>
        /// Répartition des utilisateurs par rôle
        /// </summary>
        public List<UtilisateurParRoleDto> RepartitionParRole { get; set; } = new();

        /// <summary>
        /// Nombre d'utilisateurs connectés actuellement
        /// </summary>
        public int UtilisateursConnectes { get; set; }

        /// <summary>
        /// Nombre d'utilisateurs actifs ce mois-ci
        /// </summary>
        public int UtilisateursActifsMois { get; set; }
    }

    /// <summary>
    /// Statistiques des utilisateurs pour un rôle
    /// </summary>
    public class UtilisateurParRoleDto
    {
        /// <summary>
        /// Nom du rôle
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Nombre d'utilisateurs pour ce rôle
        /// </summary>
        public int NombreUtilisateurs { get; set; }

        /// <summary>
        /// Pourcentage par rapport au total
        /// </summary>
        public decimal Pourcentage { get; set; }
    }
}
