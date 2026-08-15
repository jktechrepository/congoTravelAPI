using System.Collections.Generic;
using CongoTravel.Models;

namespace CongoTravel.Services.Notifications
{
    public enum NotificationKind
    {
        Generic,
        Facture,
        Paiement,
        Communication
    }

    public class NotificationContext
    {
        public NotificationKind Kind { get; init; }
        public Utilisateur? UtilisateurDestinataire { get; init; }
        public Societe? Societe { get; init; }
        public bool AcceptsSms { get; init; }
        public bool AllowPush { get; init; }
        public bool AllowInApp { get; init; }
        public bool AllowSms { get; init; }
        public bool UtilisateurActif { get; init; }
        public IReadOnlyDictionary<string, bool>? Preferences { get; init; }
        public string? RaisonSkip { get; init; }
    }

    public class PushNotificationMessage
    {
        public string Title { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public Dictionary<string, string> Data { get; init; } = new();
        public bool IsEnabled { get; init; }
    }

    public class SmsNotificationMessage
    {
        public string Body { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
    }

    public class InAppNotificationMessage
    {
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public string? Icon { get; init; }
        public string? ActionLink { get; init; }
        public Dictionary<string, string> Metadata { get; init; } = new();
        public bool IsEnabled { get; init; }
    }

    public class EmailNotificationMessage
    {
        public string Subject { get; init; } = string.Empty;
        public string HtmlBody { get; init; } = string.Empty;
        public string PlainTextBody { get; init; } = string.Empty;
        public bool IsEnabled { get; init; }
    }

    public class NotificationMessage
    {
        public PushNotificationMessage? Push { get; init; }
        public SmsNotificationMessage? Sms { get; init; }
        public InAppNotificationMessage? InApp { get; init; }
        public EmailNotificationMessage? Email { get; init; }
    }

    public class NotificationDispatchResult
    {
        public NotificationDispatchResult(NotificationContext context, NotificationMessage message)
        {
            Context = context;
            Message = message;
        }

        public NotificationContext Context { get; }
        public NotificationMessage Message { get; }
    }
}

