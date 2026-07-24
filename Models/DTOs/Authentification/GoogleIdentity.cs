namespace CongoTravel.Models.DTOs.Authentification
{
    public class GoogleIdentity
    {
        public string Sub { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool EmailVerified { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
    }
}
