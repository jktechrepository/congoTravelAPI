using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services.Notifications
{
    public class NotificationSender : INotificationSender
    {
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly ISignalRNotificationService _signalRService;
        private readonly ISmsNotificationService _smsService;
        private readonly IEmailService _emailService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationSender> _logger;

        public NotificationSender(
            IFirebaseNotificationService firebaseService,
            ISignalRNotificationService signalRService,
            ISmsNotificationService smsService,
            IEmailService emailService,
            INotificationRepository notificationRepository,
            ILogger<NotificationSender> logger)
        {
            _firebaseService = firebaseService;
            _signalRService = signalRService;
            _smsService = smsService;
            _emailService = emailService;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task SendAsync(NotificationDispatchResult dispatchResult, CancellationToken cancellationToken = default)
        {
            var context = dispatchResult.Context;
            var message = dispatchResult.Message;
            var utilisateur = context.UtilisateurDestinataire;
            var kind = context.Kind.ToString();

            // Suivi des statuts
            var channelStatus = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // PUSH ----------------------------------------------------------------------
            if (message.Push?.IsEnabled == true && utilisateur != null)
            {
                try
                {
                    var pushSent = await _firebaseService.EnvoyerNotificationAUtilisateurAsync(
                        utilisateur.IdUtilisateur,
                        message.Push.Title,
                        message.Push.Body,
                        message.Push.Data);

                    if (pushSent)
                    {
                        _logger.LogInformation("✅ PUSH {Kind} envoyé à l'utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                        channelStatus["Push"] = "envoyé";
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Aucun device actif pour l'utilisateur {UtilisateurId} lors de l'envoi {Kind}", utilisateur.IdUtilisateur, kind);
                        channelStatus["Push"] = "échec (aucun device)";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur PUSH {Kind} pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                    channelStatus["Push"] = "erreur";
                }
            }
            else
            {
                channelStatus["Push"] = DescribeDisabled("push", context, utilisateur);
            }

            // IN-APP -------------------------------------------------------------------
            int? inAppNotificationId = null;

            if (message.InApp?.IsEnabled == true && utilisateur != null)
            {
                inAppNotificationId = await PersistInAppNotificationAsync(context, message.InApp, utilisateur.IdUtilisateur, cancellationToken);

                try
                {
                    await _signalRService.SendCustomNotificationAsync(
                        utilisateur.IdUtilisateur,
                        message.InApp.Title,
                        message.InApp.Content,
                        message.InApp.Type ?? kind);

                    _logger.LogInformation(
                        "✅ Notification SignalR {Kind} envoyée à l'utilisateur {UtilisateurId}",
                        kind,
                        utilisateur.IdUtilisateur);
                    channelStatus["InApp"] = inAppNotificationId.HasValue
                        ? $"persisté (Id={inAppNotificationId}) + SignalR"
                        : "SignalR envoyé (persist KO)";
                }
                catch (Exception signalREx)
                {
                    _logger.LogError(signalREx, "❌ Erreur SignalR {Kind} pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                    channelStatus["InApp"] = inAppNotificationId.HasValue
                        ? $"persisté (Id={inAppNotificationId}) / SignalR erreur"
                        : "échec";
                }
            }
            else
            {
                channelStatus["InApp"] = DescribeDisabled("in-app", context, utilisateur);
            }

            // SMS ----------------------------------------------------------------------
            if (message.Sms?.IsEnabled == true && utilisateur != null && !string.IsNullOrWhiteSpace(utilisateur.Telephone))
            {
                try
                {
                    var smsType = message.Push?.Type
                        ?? message.InApp?.Type
                        ?? (message.Push?.Data.TryGetValue("type", out var pushType) == true ? pushType : null)
                        ?? context.Kind.ToString();

                    var smsLog = await _smsService.EnvoyerSmsAsync(
                        utilisateur.Telephone,
                        message.Sms.Body,
                        smsType);

                    if (smsLog != null && smsLog.Statut != "failed")
                    {
                        _logger.LogInformation("✅ SMS {Kind} envoyé à l'utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                        channelStatus["Sms"] = "envoyé";
                    }
                    else if (smsLog != null && smsLog.Statut == "failed")
                    {
                        _logger.LogWarning("⚠️ SMS {Kind} échoué pour utilisateur {UtilisateurId}: {Erreur}", kind, utilisateur.IdUtilisateur, smsLog.MessageErreur);
                        channelStatus["Sms"] = "échec";
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ SMS {Kind} non envoyé (service désactivé ou numéro invalide) pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                        channelStatus["Sms"] = "non envoyé";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur SMS {Kind} pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                    channelStatus["Sms"] = "erreur";
                }
            }
            else
            {
                channelStatus["Sms"] = DescribeDisabled("sms", context, utilisateur);
            }

            // EMAIL ---------------------------------------------------------------------
            if (message.Email?.IsEnabled == true && utilisateur != null && !string.IsNullOrWhiteSpace(utilisateur.Email))
            {
                try
                {
                    var emailSent = await _emailService.SendGenericEmailAsync(
                        utilisateur.Email,
                        utilisateur.NomComplet ?? "Client",
                        message.Email.Subject,
                        message.Email.PlainTextBody,
                        message.Email.HtmlBody);

                    if (emailSent)
                    {
                        _logger.LogInformation("✅ EMAIL {Kind} envoyé à l'utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                        channelStatus["Email"] = "envoyé";
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Échec de l'envoi EMAIL {Kind} pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                        channelStatus["Email"] = "échec";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur EMAIL {Kind} pour utilisateur {UtilisateurId}", kind, utilisateur.IdUtilisateur);
                    channelStatus["Email"] = "erreur";
                }
            }
            else
            {
                channelStatus["Email"] = DescribeDisabled("email", context, utilisateur);
            }

            _logger.LogInformation("📡 Notification {Kind} -> Dest:{Dest} | Push:{Push} | InApp:{InApp} | Sms:{Sms} | Email:{Email}",
                kind,
                utilisateur?.IdUtilisateur,
                channelStatus["Push"],
                channelStatus["InApp"],
                channelStatus["Sms"],
                channelStatus["Email"]);
        }

        private string DescribeDisabled(string channel, NotificationContext context, Utilisateur? utilisateur)
        {
            return channel.ToLowerInvariant() switch
            {
                "push" => utilisateur == null
                    ? "ignoré (aucun utilisateur)"
                    : context.Preferences?.TryGetValue("push", out var optPush) == true && !optPush
                        ? "ignoré (opt-out)"
                        : "ignoré (désactivé)",
                "in-app" => utilisateur == null
                    ? "ignoré (aucun utilisateur)"
                    : context.Preferences?.TryGetValue("inapp", out var optInApp) == true && !optInApp
                        ? "ignoré (opt-out)"
                        : "ignoré (désactivé)",
                "sms" =>
                    context.AcceptsSms == false ? "ignoré (société refuse SMS)" :
                    context.Preferences?.TryGetValue("sms", out var optSms) == true && !optSms ? "ignoré (opt-out)" :
                    utilisateur == null || string.IsNullOrWhiteSpace(utilisateur.Telephone) ? "ignoré (aucun numéro)" :
                    "ignoré (désactivé)",
                "email" =>
                    utilisateur == null ? "ignoré (aucun utilisateur)" :
                    string.IsNullOrWhiteSpace(utilisateur.Email) ? "ignoré (pas d'email)" :
                    context.Preferences?.TryGetValue("email", out var optEmail) == true && !optEmail ? "ignoré (opt-out)" :
                    "ignoré (désactivé)",
                _ => "ignoré"
            };
        }

        private async Task<int?> PersistInAppNotificationAsync(NotificationContext context, InAppNotificationMessage inApp, int idDestinataire, CancellationToken cancellationToken)
        {
            try
            {
                var notification = new Notification
                {
                    Titre = inApp.Title,
                    Contenu = inApp.Content,
                    TypeNotification = string.IsNullOrWhiteSpace(inApp.Type) ? context.Kind.ToString() : inApp.Type,
                    EstLue = false,
                    EstActive = true,
                    Icone = inApp.Icon,
                    IdDestinataire = idDestinataire,
                    IdSociete = context.Societe?.IdSociete,
                    IdAgent = null,
                    CanalUtilise = "InApp",
                    Priorite = "INFO",
                    StatutEnvoi = "Enregistre",
                    PayloadJson = inApp.Metadata.Count > 0 ? JsonSerializer.Serialize(inApp.Metadata) : null
                };

                var created = await _notificationRepository.CreateAsync(notification);
                return created.IdNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'enregistrement de la notification in-app {Kind} pour l'utilisateur {UtilisateurId}", context.Kind, idDestinataire);
                return null;
            }
        }
    }
}

