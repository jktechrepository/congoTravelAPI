using System.Globalization;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'envoi de notifications lors de l'enregistrement d'un paiement
    /// Adapté au nouveau workflow : Paiement -> Réservation -> Utilisateur
    /// </summary>
    public class PaiementNotificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<PaiementNotificationService> _logger;
        private readonly string _baseUrl;

        public PaiementNotificationService(
            CongoTravelDbContext context,
            INotificationSender notificationSender,
            ILogger<PaiementNotificationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _notificationSender = notificationSender;
            _logger = logger;

            // Récupérer la configuration du frontend
            _baseUrl = configuration["FrontendSettings:BaseUrl"] ?? "https://congotravel.cd";
        }

        /// <summary>
        /// Envoie une notification à l'utilisateur suite à un paiement
        /// </summary>
        public async Task<bool> NotifierPaiementAsync(Paiement paiement)
        {
            try
            {
                // Charger la réservation et l'utilisateur associés
                var reservation = await _context.Reservations
                    .Include(r => r.Utilisateur)
                    .FirstOrDefaultAsync(r => r.IdReservation == paiement.IdReservation);

                if (reservation == null || reservation.Utilisateur == null)
                {
                    _logger.LogWarning("Impossible de trouver la réservation ou l'utilisateur pour le paiement {Id}", paiement.IdPaiement);
                    return false;
                }

                var utilisateur = reservation.Utilisateur;

                // Préparer les données de notification
                var notificationData = new
                {
                    PaiementId = paiement.IdPaiement,
                    MontantPaye = paiement.MontantPaye?.ToString("C", new CultureInfo("fr-FR")),
                    MontantAPaye = paiement.MontantAPaye.ToString("C", new CultureInfo("fr-FR")),
                    MethodePaiement = paiement.MethodePaiement,
                    Statut = paiement.Statut,
                    DatePaiement = paiement.DateCreation.ToString("dd/MM/yyyy HH:mm", new CultureInfo("fr-FR")),
                    ReferenceReservation = $"RES-{reservation.IdReservation:D6}",
                    NomUtilisateur = utilisateur.NomComplet,
                    EmailUtilisateur = utilisateur.Email,
                    TelephoneUtilisateur = utilisateur.Telephone,
                    UrlReservation = $"{_baseUrl}/reservations/{reservation.IdReservation}",
                    NomSociete = "CongoTravel"
                };

                // Envoyer l'email de confirmation de paiement
                await EnvoyerEmailConfirmationPaiementAsync(notificationData);

                // Envoyer la notification SMS (si numéro disponible)
                if (!string.IsNullOrEmpty(utilisateur.Telephone))
                {
                    await EnvoyerSmsConfirmationPaiementAsync(notificationData);
                }

                // Envoyer la notification interne (SignalR/Firebase)
                await EnvoyerNotificationInterneAsync(notificationData);

                _logger.LogInformation("Notification de paiement envoyée avec succès - Paiement ID: {Id}, Utilisateur: {Utilisateur}", 
                    paiement.IdPaiement, utilisateur.NomComplet);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de la notification de paiement - Paiement ID: {Id}", paiement.IdPaiement);
                return false;
            }
        }

        /// <summary>
        /// Envoie un email de confirmation de paiement
        /// </summary>
        private async Task EnvoyerEmailConfirmationPaiementAsync(dynamic data)
        {
            var subject = $"Confirmation de paiement - Réservation {data.ReferenceReservation}";
            
            var body = $@"
Cher/Chère {data.NomUtilisateur},

Nous vous confirmons avoir bien reçu votre paiement pour la réservation {data.ReferenceReservation}.

Détails du paiement :
- Montant payé : {data.MontantPaye}
- Montant total : {data.MontantAPaye}
- Méthode de paiement : {data.MethodePaiement}
- Date : {data.DatePaiement}
- Statut : {data.Statut}

Vous pouvez consulter les détails de votre réservation en cliquant sur le lien suivant :
{data.UrlReservation}

Merci de votre confiance dans les services de {data.NomSociete}.

Cordialement,
L'équipe {data.NomSociete}
";

            // TODO: Implémenter l'envoi d'email lorsque le service sera disponible
            _logger.LogInformation("Email de confirmation de paiement prêt à envoyer à {Email}", (string)data.EmailUtilisateur);
        }

        /// <summary>
        /// Envoie un SMS de confirmation de paiement
        /// </summary>
        private async Task EnvoyerSmsConfirmationPaiementAsync(dynamic data)
        {
            var message = $"{data.NomSociete}: Paiement de {data.MontantPaye} reçu pour votre réservation {data.ReferenceReservation}. Merci de votre confiance.";
            
            // TODO: Implémenter l'envoi SMS lorsque le service sera disponible
            _logger.LogInformation("SMS de confirmation de paiement prêt à envoyer à {Telephone}", (string)data.TelephoneUtilisateur);
        }

        /// <summary>
        /// Envoie une notification interne (SignalR/Firebase)
        /// </summary>
        private async Task EnvoyerNotificationInterneAsync(dynamic data)
        {
            var notification = new
            {
                Type = "Paiement",
                Titre = "Nouveau paiement enregistré",
                Message = $"Paiement de {data.MontantPaye} reçu pour la réservation {data.ReferenceReservation}",
                IdPaiement = data.PaiementId,
                IdReservation = data.ReferenceReservation,
                IdUtilisateur = data.EmailUtilisateur,
                DateCreation = DateTime.Now,
                Statut = data.Statut
            };

            // TODO: Implémenter l'envoi de notification interne lorsque le service sera disponible
            _logger.LogInformation("Notification interne de paiement préparée - Type: {Type}, Titre: {Titre}", (string)notification.Type, (string)notification.Titre);
        }

        
        
   
    }
}
