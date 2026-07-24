namespace CongoTravel.Models.DTOs.Authentification
{
    /// <summary>Identité normalisée issue d'un provider OAuth (Google, Apple, …).</summary>
    public class ExternalAuthIdentity
    {
        public string Sub { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool EmailVerified { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }
}
