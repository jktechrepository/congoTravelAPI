using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant une plainte déposée par un client pour l'équipe d'intervention
    /// </summary>
    public class PlainteClient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlainte { get; set; }

        /// <summary>
        /// Identifiant du client qui dépose la plainte (OBLIGATOIRE)
        /// </summary>
        [Required]
        public int IdClient { get; set; }

        /// <summary>
        /// Titre de la plainte
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Description détaillée de la plainte (OPTIONNEL)
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Type de panne ou problème signalé
        /// </summary>
        [MaxLength(200)]
        public string? TypePanne { get; set; }

        /// <summary>
        /// Niveau d'importance (Faible, Moyen, Élevé, Critique)
        /// </summary>
        [MaxLength(50)]
        public string? NiveauImportance { get; set; }

        /// <summary>
        /// Risques principaux identifiés
        /// </summary>
        [MaxLength(500)]
        public string? RisquesPrincipaux { get; set; }

        /// <summary>
        /// Statut de la plainte (En attente, En cours, Résolu, Fermé)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string StatutPlainte { get; set; } = "En attente";

        /// <summary>
        /// Priorité de la plainte (Faible, Moyenne, Élevée, Urgente)
        /// </summary>
        [MaxLength(50)]
        public string? Priorite { get; set; }

        /// <summary>
        /// Identifiant de l'agent/équipe assigné à la plainte (OPTIONNEL)
        /// </summary>
        public int? IdAgentAssigné { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur créateur (client via IdClient)
        /// </summary>
        public int? IdUtilisateurCreateur { get; set; }

        /// <summary>
        /// Commentaire de résolution
        /// </summary>
        [MaxLength(1000)]
        public string? CommentaireResolution { get; set; }

        /// <summary>
        /// Date de résolution de la plainte
        /// </summary>
        public DateTime? DateResolution { get; set; }

        /// <summary>
        /// Indique si la plainte est urgente
        /// </summary>
        public bool EstUrgente { get; set; } = false;

        /// <summary>
        /// Date de création de la plainte
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime DateDerniereModification { get; set; } = DateTime.Now;

        /// <summary>
        /// Statut de la plainte (actif/inactif) pour soft delete
        /// </summary>
        public bool Statut { get; set; } = true;

        // Navigation properties
        [JsonIgnore]
        public Client? Client { get; set; }

        [JsonIgnore]
        public Agent? AgentAssigné { get; set; }

        [JsonIgnore]
        public Utilisateur? UtilisateurCreateur { get; set; }
    }
}

