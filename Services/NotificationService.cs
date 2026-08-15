using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de gestion des notifications avancées (convocations, réunions, alertes)
    /// </summary>
    public class NotificationService : INotificationService, INotificationRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IFirebaseNotificationService _notificationService;
        private readonly ISmsNotificationService _smsService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            CongoTravelDbContext context,
            IFirebaseNotificationService notificationService,
            ISmsNotificationService smsService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _smsService = smsService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════
        // 📨 CONVOCATIONS
        // ═══════════════════════════════════════════════════════════════

        public async Task<bool> EnvoyerConvocationAsync(
            int idUtilisateur,
            string motif,
            DateTime dateRdv,
            string lieu,
            string? contact = null,
            string priorite = "NORMALE")
        {
            try
            {
                var utilisateur = await _context.Utilisateurs.FindAsync(idUtilisateur);
                if (utilisateur == null)
                {
                    _logger.LogWarning($"Utilisateur {idUtilisateur} introuvable pour convocation");
                    return false;
                }

                // Préparer le message
                string titre = "📩 Convocation";
                string dateFormatee = dateRdv.ToString("dd/MM/yyyy 'à' HH:mm");
                string corps = $"Motif: {motif}\nDate: {dateFormatee}\nLieu: {lieu}";
                if (!string.IsNullOrWhiteSpace(contact))
                {
                    corps += $"\nContact: {contact}";
                }

                var donnees = new Dictionary<string, string>
                {
                    { "type", "CONVOCATION_PARENT" },
                    { "motif", motif },
                    { "dateRdv", dateRdv.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "lieu", lieu },
                    { "priorite", priorite }
                };

                // Envoyer selon la priorité
                bool success = false;

                if (priorite == "HAUTE")
                {
                    // Priorité HAUTE : PUSH + SMS systématique (double envoi)
                    var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                        idUtilisateur, titre, corps, donnees);

                    // SMS même si PUSH réussit (garantie de lecture)
                    string messageSms = $"Convocation: {motif} le {dateFormatee} à {lieu}.";
                    if (!string.IsNullOrWhiteSpace(contact))
                    {
                        messageSms += $" Tel: {contact}";
                    }
                    var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                        idUtilisateur, messageSms, "CONVOCATION_PARENT");

                    success = pushSuccess || (smsLog != null && smsLog.Statut != "failed");
                }
                else if (priorite == "NORMALE")
                {
                    // Priorité NORMALE : PUSH + SMS fallback
                    var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                        idUtilisateur, titre, corps, donnees);

                    if (!pushSuccess)
                    {
                        // Fallback SMS
                        string messageSms = $"Convocation: {motif} le {dateFormatee} à {lieu}.";
                        if (!string.IsNullOrWhiteSpace(contact))
                        {
                            messageSms += $" Tel: {contact}";
                        }
                        var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                            idUtilisateur, messageSms, "CONVOCATION_PARENT");
                        success = smsLog != null && smsLog.Statut != "failed";
                    }
                    else
                    {
                        success = true;
                    }
                }
                else // BASSE
                {
                    // Priorité BASSE : PUSH uniquement (économie SMS)
                    success = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                        idUtilisateur, titre, corps, donnees);
                }

                if (success)
                {
                    _logger.LogInformation($"✅ Convocation envoyée à utilisateur {idUtilisateur} (Priorité: {priorite})");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur envoi convocation à utilisateur {idUtilisateur}: {ex.Message}");
                return false;
            }
        }

        public async Task<int> EnvoyerConvocationEnMasseAsync(
            List<int> idsUtilisateurs,
            string motif,
            DateTime dateRdv,
            string lieu,
            string? contact = null,
            string priorite = "NORMALE")
        {
            int count = 0;
            foreach (var idUtilisateur in idsUtilisateurs)
            {
                var success = await EnvoyerConvocationAsync(
                    idUtilisateur, motif, dateRdv, lieu, contact, priorite);
                if (success) count++;
            }
            _logger.LogInformation($"📨 Convocations en masse: {count}/{idsUtilisateurs.Count} envoyées");
            return count;
        }

        // ═══════════════════════════════════════════════════════════════
        // 🏫 RÉUNIONS
        // ═══════════════════════════════════════════════════════════════

        public async Task<int> EnvoyerInvitationReunionAsync(
            string titre,
            string description,
            DateTime dateReunion,
            string lieu,
            int? idSociete = null,
            int? idClasse = null,
            string priorite = "NORMALE")
        {
            try
            {
                // Récupérer les utilisateurs cibles (parents)
                List<int> idsUtilisateurs = new List<int>();

                // ⚠️ NOTE: Les modèles Tuteur et Eleve ont été supprimés
                // Pour l'instant, on retourne une liste vide ou tous les utilisateurs actifs de la société
                if (idSociete.HasValue)
                {
                    idsUtilisateurs = await _context.Utilisateurs
                        .Where(u => u.Statut == true && u.IdSociete == idSociete)
                        .Select(u => u.IdUtilisateur)
                        .Distinct()
                        .ToListAsync();
                }
                else
                {
                    // Aucun destinataire si pas de société spécifiée
                    idsUtilisateurs = new List<int>();
                }

                if (idsUtilisateurs.Count == 0)
                {
                    _logger.LogWarning($"Aucun destinataire trouvé pour réunion (École: {idSociete}, Classe: {idClasse})");
                    return 0;
                }

                // Préparer le message
                string dateFormatee = dateReunion.ToString("dd/MM/yyyy 'à' HH:mm");
                string titreComplet = $"🏫 Réunion: {titre}";
                string corps = $"{description}\n\nDate: {dateFormatee}\nLieu: {lieu}";

                var donnees = new Dictionary<string, string>
                {
                    { "type", "REUNION_ECOLE" },
                    { "titre", titre },
                    { "dateReunion", dateReunion.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "lieu", lieu },
                    { "priorite", priorite }
                };

                if (idSociete.HasValue) donnees.Add("idSociete", idSociete.Value.ToString());
                if (idClasse.HasValue) donnees.Add("idClasse", idClasse.Value.ToString());

                // Envoyer aux destinataires
                int count = 0;
                foreach (var idUtilisateur in idsUtilisateurs)
                {
                    bool success = false;

                    if (priorite == "HAUTE")
                    {
                        // PUSH + SMS systématique
                        var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, corps, donnees);

                        string messageSms = $"Réunion: {titre} le {dateFormatee} à {lieu}.";
                        var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                            idUtilisateur, messageSms, "REUNION_ECOLE");

                        success = pushSuccess || (smsLog != null && smsLog.Statut != "failed");
                    }
                    else if (priorite == "NORMALE")
                    {
                        // PUSH + SMS fallback
                        var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, corps, donnees);

                        if (!pushSuccess)
                        {
                            string messageSms = $"Réunion: {titre} le {dateFormatee} à {lieu}.";
                            var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                                idUtilisateur, messageSms, "REUNION_ECOLE");
                            success = smsLog != null && smsLog.Statut != "failed";
                        }
                        else
                        {
                            success = true;
                        }
                    }
                    else // BASSE
                    {
                        // PUSH uniquement
                        success = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, corps, donnees);
                    }

                    if (success) count++;
                }

                _logger.LogInformation($"🏫 Invitation réunion '{titre}': {count}/{idsUtilisateurs.Count} envoyées");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur envoi invitation réunion: {ex.Message}");
                return 0;
            }
        }

        public async Task<int> EnvoyerRappelReunionAsync(
            string titre,
            DateTime dateReunion,
            string lieu,
            int? idSociete = null,
            int? idClasse = null)
        {
            try
            {
                // Récupérer les utilisateurs cibles
                List<int> idsUtilisateurs = new List<int>();

                // ⚠️ NOTE: Les modèles Tuteur et Eleve ont été supprimés
                // Pour l'instant, on retourne une liste vide ou tous les utilisateurs actifs de la société
                if (idSociete.HasValue)
                {
                    idsUtilisateurs = await _context.Utilisateurs
                        .Where(u => u.Statut == true && u.IdSociete == idSociete)
                        .Select(u => u.IdUtilisateur)
                        .Distinct()
                        .ToListAsync();
                }
                else
                {
                    // Aucun destinataire si pas de société spécifiée
                    idsUtilisateurs = new List<int>();
                }

                if (idsUtilisateurs.Count == 0)
                {
                    return 0;
                }

                // Préparer le message de rappel
                string dateFormatee = dateReunion.ToString("dd/MM/yyyy 'à' HH:mm");
                string titreRappel = $"🔔 Rappel: {titre}";
                string corps = $"Rappel de la réunion\n\nDate: {dateFormatee}\nLieu: {lieu}\n\nNous comptons sur votre présence.";

                var donnees = new Dictionary<string, string>
                {
                    { "type", "RAPPEL_REUNION" },
                    { "titre", titre },
                    { "dateReunion", dateReunion.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "lieu", lieu }
                };

                // Envoyer PUSH uniquement pour les rappels (économie SMS)
                int count = 0;
                foreach (var idUtilisateur in idsUtilisateurs)
                {
                    var success = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                        idUtilisateur, titreRappel, corps, donnees);
                    if (success) count++;
                }

                _logger.LogInformation($"🔔 Rappel réunion '{titre}': {count}/{idsUtilisateurs.Count} envoyés");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur envoi rappel réunion: {ex.Message}");
                return 0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 🚨 ALERTES URGENTES
        // ═══════════════════════════════════════════════════════════════

        public async Task<int> EnvoyerAlerteUrgenteAsync(
            string titre,
            string message,
            int? idSociete = null,
            int? idClasse = null)
        {
            try
            {
                // Récupérer TOUS les utilisateurs concernés
                List<int> idsUtilisateurs = new List<int>();

                // ⚠️ NOTE: Les modèles Tuteur et Eleve ont été supprimés
                // Pour l'instant, on retourne tous les utilisateurs actifs de la société
                if (idSociete.HasValue)
                {
                    idsUtilisateurs = await _context.Utilisateurs
                        .Where(u => u.Statut == true && u.IdSociete == idSociete)
                        .Select(u => u.IdUtilisateur)
                        .Distinct()
                        .ToListAsync();
                }
                else
                {
                    // Tous les utilisateurs actifs du système
                    idsUtilisateurs = await _context.Utilisateurs
                        .Where(u => u.Statut == true)
                        .Select(u => u.IdUtilisateur)
                        .Distinct()
                        .ToListAsync();
                }

                if (idsUtilisateurs.Count == 0)
                {
                    _logger.LogWarning($"Aucun destinataire trouvé pour alerte urgente");
                    return 0;
                }

                // Préparer le message
                string titreUrgent = $"🚨 ALERTE URGENTE: {titre}";
                var donnees = new Dictionary<string, string>
                {
                    { "type", "ALERTE_URGENTE" },
                    { "titre", titre },
                    { "priorite", "HAUTE" }
                };

                // PUSH + SMS SYSTÉMATIQUE pour TOUS (garantie maximale de lecture)
                int count = 0;
                foreach (var idUtilisateur in idsUtilisateurs)
                {
                    // PUSH
                    var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                        idUtilisateur, titreUrgent, message, donnees);

                    // SMS même si PUSH réussit (double envoi pour urgence)
                    string messageSms = $"ALERTE URGENTE: {titre}. {message}";
                    if (messageSms.Length > 160)
                    {
                        messageSms = messageSms.Substring(0, 157) + "...";
                    }
                    var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                        idUtilisateur, messageSms, "ALERTE_URGENTE");

                    if (pushSuccess || (smsLog != null && smsLog.Statut != "failed"))
                    {
                        count++;
                    }
                }

                _logger.LogInformation($"🚨 Alerte urgente '{titre}': {count}/{idsUtilisateurs.Count} envoyées");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur envoi alerte urgente: {ex.Message}");
                return 0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📢 COMMUNICATION ADMINISTRATIVE
        // ═══════════════════════════════════════════════════════════════

        public async Task<int> EnvoyerCommunicationAdminAsync(
            string titre,
            string message,
            List<int> idsUtilisateurs,
            string priorite = "NORMALE")
        {
            try
            {
                if (idsUtilisateurs.Count == 0)
                {
                    return 0;
                }

                string titreComplet = $"📢 {titre}";
                var donnees = new Dictionary<string, string>
                {
                    { "type", "COMMUNICATION_ADMIN" },
                    { "titre", titre },
                    { "priorite", priorite }
                };

                int count = 0;
                foreach (var idUtilisateur in idsUtilisateurs)
                {
                    bool success = false;

                    if (priorite == "HAUTE")
                    {
                        // PUSH + SMS systématique
                        var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, message, donnees);

                        string messageSms = message.Length > 160 ? message.Substring(0, 157) + "..." : message;
                        var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                            idUtilisateur, messageSms, "COMMUNICATION_ADMIN");

                        success = pushSuccess || (smsLog != null && smsLog.Statut != "failed");
                    }
                    else if (priorite == "NORMALE")
                    {
                        // PUSH + SMS fallback
                        var pushSuccess = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, message, donnees);

                        if (!pushSuccess)
                        {
                            string messageSms = message.Length > 160 ? message.Substring(0, 157) + "..." : message;
                            var smsLog = await _smsService.EnvoyerSmsAUtilisateurAsync(
                                idUtilisateur, messageSms, "COMMUNICATION_ADMIN");
                            success = smsLog != null && smsLog.Statut != "failed";
                        }
                        else
                        {
                            success = true;
                        }
                    }
                    else // BASSE
                    {
                        // PUSH uniquement
                        success = await _notificationService.EnvoyerNotificationAUtilisateurAsync(
                            idUtilisateur, titreComplet, message, donnees);
                    }

                    if (success) count++;
                }

                _logger.LogInformation($"📢 Communication admin '{titre}': {count}/{idsUtilisateurs.Count} envoyées");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur envoi communication admin: {ex.Message}");
                return 0;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // 📋 IMPLÉMENTATION INotificationRepository (CRUD de base)
        // ═══════════════════════════════════════════════════════════════

        public async Task<IEnumerable<Notification>> GetAllAsync()
        {
            return await _context.Notifications
                .Where(n => n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<Notification?> GetByIdAsync(int id)
        {
            return await _context.Notifications
                .Where(n => n.Statut == true)
                .FirstOrDefaultAsync(n => n.IdNotification == id);
        }

        public async Task<IEnumerable<Notification>> GetByDestinataireAsync(int idDestinataire)
        {
            return await _context.Notifications
                .Where(n => n.IdDestinataire == idDestinataire && n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByExpediteurAsync(int idExpediteur)
        {
            return await _context.Notifications
                .Where(n => n.IdExpediteur == idExpediteur && n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetBySocieteAsync(int idSociete)
        {
            return await _context.Notifications
                .Where(n => n.IdSociete == idSociete && n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByClasseAsync(int idClasse)
        {
            // ⚠️ NOTE: Le modèle Classe a été supprimé
            // Pour l'instant, on retourne une liste vide
            return await _context.Notifications
                .Where(n => false) // Aucune notification car Classe n'existe plus
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetByTypeAsync(string type)
        {
            return await _context.Notifications
                .Where(n => n.TypeNotification == type && n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetNonLuesAsync(int idDestinataire)
        {
            return await _context.Notifications
                .Where(n => n.IdDestinataire == idDestinataire && !n.EstLue && n.Statut == true)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            notification.DateCreation = DateTime.Now;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<Notification?> UpdateAsync(Notification notification)
        {
            var existing = await _context.Notifications.FindAsync(notification.IdNotification);
            if (existing == null) return null;

            existing.Titre = notification.Titre;
            existing.Contenu = notification.Contenu;
            existing.TypeNotification = notification.TypeNotification;
            existing.EstLue = notification.EstLue;
            existing.Statut = notification.Statut;
            existing.LienAction = notification.LienAction;
            existing.Icone = notification.Icone;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> MarquerCommeLueAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            notification.EstLue = true;
            notification.DateLecture = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarquerToutesCommeLuesAsync(int idDestinataire)
        {
            var notifications = await _context.Notifications
                .Where(n => n.IdDestinataire == idDestinataire && !n.EstLue)
                .ToListAsync();

            foreach (var notif in notifications)
            {
                notif.EstLue = true;
                notif.DateLecture = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return false;

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            notification.Statut = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Notifications.AnyAsync(n => n.IdNotification == id);
        }
    }
}
