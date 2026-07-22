using System.Text.Json;
using CongoTravel.Models.DTOs.Communication;

namespace CongoTravel.Helpers
{
    public static class CommunicationChannelHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static string SerializeChannels(CommunicationChannelsDto channels)
        {
            var payload = new Dictionary<string, bool>
            {
                ["push"] = channels.Push,
                ["email"] = channels.Email,
                ["sms"] = channels.Sms,
                ["inApp"] = channels.InApp
            };
            return JsonSerializer.Serialize(payload, JsonOptions);
        }

        public static CommunicationChannelsDto DeserializeChannels(string channelsJson)
        {
            if (string.IsNullOrWhiteSpace(channelsJson))
            {
                return new CommunicationChannelsDto();
            }

            try
            {
                var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(channelsJson, JsonOptions)
                              ?? new Dictionary<string, bool>();

                return new CommunicationChannelsDto
                {
                    Push = payload.TryGetValue("push", out var push) ? push : true,
                    Email = payload.TryGetValue("email", out var email) && email,
                    Sms = payload.TryGetValue("sms", out var sms) && sms,
                    InApp = payload.TryGetValue("inApp", out var inApp) ? inApp : true
                };
            }
            catch
            {
                // En cas de JSON invalide, retourner les valeurs par défaut
                return new CommunicationChannelsDto();
            }
        }

        /// <summary>
        /// Retourne les canaux activés sous forme de liste ordonnée (InApp, Push, Email, Sms).
        /// </summary>
        public static IReadOnlyList<string> GetOrderedChannels(CommunicationChannelsDto channels)
        {
            var list = new List<string>(4);

            if (channels.InApp) list.Add("InApp");
            if (channels.Push) list.Add("Push");
            if (channels.Email) list.Add("Email");
            if (channels.Sms) list.Add("Sms");

            return list;
        }

        /// <summary>
        /// Détermine si un canal est activé dans le JSON stocké.
        /// </summary>
        public static bool IsChannelEnabled(string channelsJson, string channelName)
        {
            var channels = DeserializeChannels(channelsJson);
            return channelName.ToLowerInvariant() switch
            {
                "push" => channels.Push,
                "email" => channels.Email,
                "sms" => channels.Sms,
                "inapp" or "in_app" => channels.InApp,
                _ => false
            };
        }
    }
}

