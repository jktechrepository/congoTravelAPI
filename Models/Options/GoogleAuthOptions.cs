namespace CongoTravel.Models.Options
{
    public class GoogleAuthOptions
    {
        public const string SectionName = "GoogleAuth";

        /// <summary>Client IDs Google (Android / iOS / Web) acceptés comme audience de l'ID token.</summary>
        public List<string> ClientIds { get; set; } = new();
    }
}
