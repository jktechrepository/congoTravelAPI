using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// 📱 Modèle pour l'historique des SMS envoyés via Twilio
    /// Permet le tracking, le calcul des coûts, et le suivi des livraisons
    /// </summary>
    public class SmsLog
    {
        [Key]
        public int IdSmsLog { get; set; }

        // ═══════════════════════════════════════════════════════════════
        // 📞 INFORMATIONS DESTINATAIRE
        // ═══════════════════════════════════════════════════════════════

        [Required]
        [MaxLength(20)]
        public string NumeroDestinataire { get; set; } = string.Empty; // Format: +243999123456

        public int? IdUtilisateur { get; set; } // Utilisateur destinataire (si lié à un compte)

        // ═══════════════════════════════════════════════════════════════
        // 📝 CONTENU DU MESSAGE
        // ═══════════════════════════════════════════════════════════════

        [Required]
        [MaxLength(1600)] // Limite Twilio pour les SMS longs
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? TypeNotification { get; set; } // Ex: "PRESENCE_ELEVE", "PAIEMENT", "ALERTE"

        // ═══════════════════════════════════════════════════════════════
        // 🔄 STATUT ET TRAÇABILITÉ TWILIO
        // ═══════════════════════════════════════════════════════════════

        [Required]
        [MaxLength(20)]
        public string Statut { get; set; } = "PENDING"; 
        // Valeurs possibles:
        // - PENDING    : En attente d'envoi
        // - SENT       : Envoyé à Twilio
        // - DELIVERED  : Délivré au destinataire
        // - FAILED     : Échec d'envoi
        // - UNDELIVERED: Non délivré

        [MaxLength(100)]
        public string? MessageSid { get; set; } // ID unique Twilio (ex: SMxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx)

        [MaxLength(500)]
        public string? MessageErreur { get; set; } // Message d'erreur si échec

        public int? CodeErreur { get; set; } // Code d'erreur Twilio

        // ═══════════════════════════════════════════════════════════════
        // 💰 COÛTS
        // ═══════════════════════════════════════════════════════════════

        public double CoutUsd { get; set; } = 0.0467; // Prix par SMS RDC (Twilio)

        public double CoutFc { get; set; } = 0.0; // Équivalent en Francs Congolais

        // ═══════════════════════════════════════════════════════════════
        // 📅 DATES
        // ═══════════════════════════════════════════════════════════════

        [Required]
        public DateTime DateEnvoi { get; set; } = DateTime.Now;

        public DateTime? DateLivraison { get; set; } // Date de livraison effective

        public DateTime? DateEchec { get; set; } // Date d'échec si applicable

        // ═══════════════════════════════════════════════════════════════
        // 📊 MÉTADONNÉES
        // ═══════════════════════════════════════════════════════════════

        public int NombreSegments { get; set; } = 1; // Nombre de segments SMS (1 segment = 160 caractères)

        [MaxLength(10)]
        public string? Direction { get; set; } = "OUTBOUND"; // OUTBOUND (envoi) ou INBOUND (réception)

        [MaxLength(50)] // Augmenté pour supporter les SenderId Twilio (ex: MG20ae2559987c6b3822b3b3eaba81ec85 = 34 chars)
        public string? NumeroExpediteur { get; set; } // Numéro Twilio ou SenderId utilisé pour l'envoi

        // ═══════════════════════════════════════════════════════════════
        // 🔗 NAVIGATION
        // ═══════════════════════════════════════════════════════════════

        [JsonIgnore]
        [ValidateNever]
        public Utilisateur? Utilisateur { get; set; }
    }
}

