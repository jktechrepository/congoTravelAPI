using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de notifications pour les plaintes clients
    /// </summary>
    public class PlainteClientNotificationService : IPlainteClientNotificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly ISignalRNotificationService _signalRService;
        private readonly ISmsNotificationService _smsService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PlainteClientNotificationService> _logger;

        public PlainteClientNotificationService(
            CongoTravelDbContext context,
            IFirebaseNotificationService firebaseService,
            ISignalRNotificationService signalRService,
            ISmsNotificationService smsService,
            IEmailService emailService,
            ILogger<PlainteClientNotificationService> logger)
        {
            _context = context;
            _firebaseService = firebaseService;
            _signalRService = signalRService;
            _smsService = smsService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifierEquipeInterventionAsync(PlainteClient plainte)
        {
            try
            {
                // Charger les données nécessaires
                var plainteComplete = await _context.PlainteClients
                    .Include(p => p.Client)
                    .FirstOrDefaultAsync(p => p.IdPlainte == plainte.IdPlainte);

                if (plainteComplete == null)
                {
                    _logger.LogWarning("⚠️ Plainte {PlainteId} introuvable pour notification", plainte.IdPlainte);
                    return;
                }

                var client = plainteComplete.Client;
                if (client == null)
                {
                    _logger.LogWarning("⚠️ Client introuvable pour la plainte {PlainteId}", plainte.IdPlainte);
                    return;
                }

                // Construire le message de notification
                var titre = plainteComplete.EstUrgente 
                    ? $"🚨 PLAINTE URGENTE - {plainteComplete.Titre}"
                    : $"📋 Nouvelle plainte - {plainteComplete.Titre}";

                var message = $"Client: {client.NomClient}\n" +
                             $"Adresse: {client.AdresseClient ?? "Non spécifiée"}\n" +
                             $"Type: {plainteComplete.TypePanne ?? "Non spécifié"}\n" +
                             $"Priorité: {plainteComplete.Priorite ?? "Non spécifiée"}";

                if (!string.IsNullOrWhiteSpace(plainteComplete.Description))
                {
                    message += $"\n\n{plainteComplete.Description}";
                }

                // Récupérer les agents/équipe d'intervention
                // Option 1: Notifier tous les agents actifs
                var agents = await _context.Agents
                    .Where(a => a.Statut == true)
                    .Include(a => a.Utilisateurs)
                    .ToListAsync();

                var utilisateursIntervention = new List<Utilisateur>();

                foreach (var agent in agents)
                {
                    var utilisateursAgent = await _context.Utilisateurs
                        .Where(u => u.IdAgent == agent.IdAgent && u.Statut == true)
                        .ToListAsync();
                    
                    utilisateursIntervention.AddRange(utilisateursAgent);
                }

                // Option 2: Notifier par rôle (si un rôle "Intervention" ou "Technicien" existe)
                var roleIntervention = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom.ToLower().Contains("intervention") || 
                                              r.Nom.ToLower().Contains("technicien") ||
                                              r.Nom.ToLower().Contains("agent"));

                if (roleIntervention != null)
                {
                    var utilisateursParRole = await _context.Utilisateurs
                        .Where(u => u.IdRole == roleIntervention.IdRole && u.Statut == true)
                        .ToListAsync();
                    
                    utilisateursIntervention.AddRange(utilisateursParRole);
                }

                // Dédupliquer les utilisateurs
                utilisateursIntervention = utilisateursIntervention
                    .GroupBy(u => u.IdUtilisateur)
                    .Select(g => g.First())
                    .ToList();

                if (!utilisateursIntervention.Any())
                {
                    _logger.LogWarning("⚠️ Aucun utilisateur de l'équipe d'intervention trouvé pour notifier");
                    return;
                }

                _logger.LogInformation(
                    "📢 Notification de la plainte {PlainteId} à {Count} membre(s) de l'équipe d'intervention",
                    plainte.IdPlainte, utilisateursIntervention.Count);

                // Données pour la notification
                var donnees = new Dictionary<string, string>
                {
                    ["type"] = "PLAINTE_CLIENT",
                    ["plainteId"] = plainte.IdPlainte.ToString(),
                    ["clientId"] = plainte.IdClient.ToString(),
                    ["urgente"] = plainteComplete.EstUrgente.ToString().ToLower()
                };

                // Envoyer les notifications à chaque membre de l'équipe
                int succes = 0;
                foreach (var utilisateur in utilisateursIntervention)
                {
                    try
                    {
                        // Push notification
                        await _firebaseService.EnvoyerNotificationAUtilisateurAsync(
                            utilisateur.IdUtilisateur,
                            titre,
                            message,
                            donnees);

                        // SignalR (in-app)
                        await _signalRService.SendCustomNotificationAsync(
                            utilisateur.IdUtilisateur,
                            titre,
                            message,
                            "PLAINTE_CLIENT");

                        succes++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "❌ Erreur lors de l'envoi de notification à l'utilisateur {UserId} pour la plainte {PlainteId}",
                            utilisateur.IdUtilisateur, plainte.IdPlainte);
                    }
                }

                _logger.LogInformation(
                    "✅ Notifications envoyées pour la plainte {PlainteId}: {Succes}/{Total}",
                    plainte.IdPlainte, succes, utilisateursIntervention.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la notification de l'équipe d'intervention pour la plainte {PlainteId}", 
                    plainte.IdPlainte);
            }
        }
    }
}

