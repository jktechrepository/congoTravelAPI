using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant une campagne de communication envoyée par la société à ses clients
    /// </summary>
    public class CommunicationCampaign
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCampagne { get; set; }

        /// <summary>
        /// Titre de la campagne
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Contenu du message principal
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Contenu { get; set; } = string.Empty;

        /// <summary>
        /// Type de campagne (INFO, ALERTE, PROMOTION, MAINTENANCE, etc.)
        /// </summary>
        [MaxLength(50)]
        public string TypeCampagne { get; set; } = "INFO";

        /// <summary>
        /// Identifiant de la société émettrice
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur créateur de la campagne
        /// </summary>
        [Required]
        public int IdUtilisateurCreateur { get; set; }

        /// <summary>
        /// Critères de ciblage au format JSON (catégories, zones, etc.)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? CriteresCiblage { get; set; }

        /// <summary>
        /// Liste spécifique d'IDs clients (optionnel, format JSON)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? ListeIdClients { get; set; }

        /// <summary>
        /// Activer les notifications push
        /// </summary>
        public bool ActiverPush { get; set; } = true;

        /// <summary>
        /// Activer les SMS
        /// </summary>
        public bool ActiverSms { get; set; } = false;

        /// <summary>
        /// Activer les emails
        /// </summary>
        public bool ActiverEmail { get; set; } = false;

        /// <summary>
        /// Activer les notifications in-app
        /// </summary>
        public bool ActiverInApp { get; set; } = true;

        /// <summary>
        /// Date d'envoi programmé (null = envoi immédiat)
        /// </summary>
        public DateTime? DateEnvoi { get; set; }

        /// <summary>
        /// Indique si la campagne est programmée
        /// </summary>
        public bool EstProgrammee { get; set; } = false;

        /// <summary>
        /// Indique si la campagne est en cours d'envoi
        /// </summary>
        public bool EstEnCours { get; set; } = false;

        /// <summary>
        /// Indique si la campagne est terminée
        /// </summary>
        public bool EstTerminee { get; set; } = false;

        /// <summary>
        /// Nombre de clients ciblés par la campagne
        /// </summary>
        public int NombreDestinataires { get; set; } = 0;

        /// <summary>
        /// Nombre total de notifications envoyées
        /// </summary>
        public int NombreEnvoyes { get; set; } = 0;

        /// <summary>
        /// Nombre de notifications envoyées avec succès
        /// </summary>
        public int NombreSucces { get; set; } = 0;

        /// <summary>
        /// Nombre de notifications en échec
        /// </summary>
        public int NombreEchecs { get; set; } = 0;

        /// <summary>
        /// Date de création de la campagne
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime DateDerniereModification { get; set; } = DateTime.Now;

        /// <summary>
        /// Date réelle d'envoi de la campagne
        /// </summary>
        public DateTime? DateEnvoiEffectif { get; set; }

        /// <summary>
        /// Statut de la campagne (actif/inactif) pour soft delete
        /// </summary>
        public bool Statut { get; set; } = true;

        // Navigation properties
        [JsonIgnore]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        public Utilisateur? UtilisateurCreateur { get; set; }
    }
}

