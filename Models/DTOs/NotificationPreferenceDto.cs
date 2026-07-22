using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    /// <summary>
    /// DTO pour les préférences de notification
    /// </summary>
    public class NotificationPreferenceDto
    {
        public bool AllowPush { get; set; } = true;
        public bool AllowInApp { get; set; } = true;
        public bool AllowSms { get; set; } = true;
        public bool AllowEmail { get; set; } = true;
        public bool OptOutGlobal { get; set; } = false;
        public bool OptOutFactures { get; set; } = false;
    }

    /// <summary>
    /// DTO pour créer/mettre à jour les préférences
    /// </summary>
    public class UpdateNotificationPreferenceDto
    {
        public bool? AllowPush { get; set; }
        public bool? AllowInApp { get; set; }
        public bool? AllowSms { get; set; }
        public bool? AllowEmail { get; set; }
        public bool? OptOutGlobal { get; set; }
        public bool? OptOutFactures { get; set; }
    }
}

