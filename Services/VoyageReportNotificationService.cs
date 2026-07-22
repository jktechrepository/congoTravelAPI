using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Notifications;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VoyageReportNotificationService : IVoyageReportNotificationService
    {
        private static readonly string[] StatutsReservationActifs = { "CONFIRMEE", "CONFIRME", "EN_ATTENTE" };

        private readonly CongoTravelDbContext _context;
        private readonly INotificationPreferenceRepository _preferenceRepository;
        private readonly INotificationSender _notificationSender;
        private readonly ILogger<VoyageReportNotificationService> _logger;

        public VoyageReportNotificationService(
            CongoTravelDbContext context,
            INotificationPreferenceRepository preferenceRepository,
            INotificationSender notificationSender,
            ILogger<VoyageReportNotificationService> logger)
        {
            _context = context;
            _preferenceRepository = preferenceRepository;
            _notificationSender = notificationSender;
            _logger = logger;
        }

        public async Task<(int Envoyees, int Echecs)> NotifyReservedClientsAsync(
            Voyage voyage,
            DateTime ancienneDateDepart,
            TimeSpan ancienneHeureDepart,
            string? motif,
            CancellationToken cancellationToken = default)
        {
            var clientIds = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.IdVoyage == voyage.Id && r.Statut && StatutsReservationActifs.Contains(r.StatutReservation))
                .Select(r => r.IdClient)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (clientIds.Count == 0)
                return (0, 0);

            var clients = await _context.Clients
                .AsNoTracking()
                .Where(c => clientIds.Contains(c.IdClient) && c.Statut)
                .ToListAsync(cancellationToken);

            var societe = await _context.Societes.AsNoTracking()
                .FirstOrDefaultAsync(s => s.IdSociete == voyage.IdSociete, cancellationToken);

            var trajet = FormatTrajet(voyage);
            var titre = "Report de votre voyage";
            var corps = BuildMessageCorps(
                trajet,
                ancienneDateDepart,
                ancienneHeureDepart,
                voyage.DateDepart,
                voyage.HeureDepart,
                motif);

            var envoyees = 0;
            var echecs = 0;

            foreach (var client in clients)
            {
                var utilisateurs = await _context.Utilisateurs
                    .Where(u => u.IdClient == client.IdClient && u.Statut == true)
                    .ToListAsync(cancellationToken);

                foreach (var utilisateur in utilisateurs)
                {
                    try
                    {
                        var prefs = await _preferenceRepository.GetByUtilisateurAsync(utilisateur.IdUtilisateur);
                        if (prefs?.OptOutGlobal == true)
                            continue;

                        var allowPush = prefs?.AllowPush ?? true;
                        var allowInApp = prefs?.AllowInApp ?? true;
                        var allowSms = prefs?.AllowSms ?? true;
                        var allowEmail = prefs?.AllowEmail ?? true;

                        var message = new NotificationMessage
                        {
                            Push = allowPush ? new PushNotificationMessage
                            {
                                Title = titre,
                                Body = corps,
                                Type = "VOYAGE_REPORT",
                                Data = new Dictionary<string, string>
                                {
                                    ["type"] = "VOYAGE_REPORT",
                                    ["idVoyage"] = voyage.Id.ToString()
                                },
                                IsEnabled = true
                            } : null,
                            Sms = allowSms ? new SmsNotificationMessage
                            {
                                Body = $"{titre}\n\n{corps}",
                                IsEnabled = true
                            } : null,
                            Email = allowEmail ? new EmailNotificationMessage
                            {
                                Subject = titre,
                                PlainTextBody = corps,
                                HtmlBody = $"<h2>{titre}</h2><p>{corps.Replace("\n", "<br/>")}</p>",
                                IsEnabled = true
                            } : null,
                            InApp = allowInApp ? new InAppNotificationMessage
                            {
                                Title = titre,
                                Content = corps,
                                Type = "VOYAGE_REPORT",
                                Metadata = new Dictionary<string, string>
                                {
                                    ["idVoyage"] = voyage.Id.ToString(),
                                    ["ancienneDate"] = ancienneDateDepart.ToString("yyyy-MM-dd"),
                                    ["nouvelleDate"] = voyage.DateDepart.ToString("yyyy-MM-dd")
                                },
                                IsEnabled = true
                            } : null
                        };

                        var context = new NotificationContext
                        {
                            Kind = NotificationKind.Generic,
                            UtilisateurDestinataire = utilisateur,
                            Societe = societe,
                            AcceptsSms = allowSms,
                            AllowPush = allowPush,
                            AllowInApp = allowInApp,
                            AllowSms = allowSms,
                            UtilisateurActif = true
                        };

                        await _notificationSender.SendAsync(new NotificationDispatchResult(context, message), cancellationToken);
                        envoyees++;
                    }
                    catch (Exception ex)
                    {
                        echecs++;
                        _logger.LogError(ex,
                            "Notification report voyage échouée — client {IdClient}, utilisateur {IdUtilisateur}",
                            client.IdClient, utilisateur.IdUtilisateur);
                    }
                }
            }

            return (envoyees, echecs);
        }

        private static string FormatTrajet(Voyage voyage)
        {
            if (voyage.Destination != null)
                return $"{voyage.Destination.VilleDepart} → {voyage.Destination.VilleArrivee}";
            return $"Voyage #{voyage.Id}";
        }

        private static string BuildMessageCorps(
            string trajet,
            DateTime ancienneDate,
            TimeSpan ancienneHeure,
            DateTime nouvelleDate,
            TimeSpan nouvelleHeure,
            string? motif)
        {
            var msg = $"Votre trajet {trajet} a été reporté.\n" +
                      $"Ancien départ : {ancienneDate:dd/MM/yyyy} à {ancienneHeure:hh\\:mm}\n" +
                      $"Nouveau départ : {nouvelleDate:dd/MM/yyyy} à {nouvelleHeure:hh\\:mm}";
            if (!string.IsNullOrWhiteSpace(motif))
                msg += $"\nMotif : {motif.Trim()}";
            msg += "\nVos billets restent valides pour le nouveau créneau.";
            return msg;
        }
    }
}
