namespace CongoTravel.Models.Options
{
    public class AppleAuthOptions
    {
        public const string SectionName = "AppleAuth";

        /// <summary>Audiences acceptées (Services ID / Bundle IDs) pour le claim aud du identity token.</summary>
        public List<string> ClientIds { get; set; } = new();
    }
}
