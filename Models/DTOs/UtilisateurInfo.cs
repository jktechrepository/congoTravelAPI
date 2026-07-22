namespace CongoTravel.Models.DTOs
{
    public class UtilisateurInfo
    {
        public int IdUtilisateur { get; set; }
        public int? IdAgent { get; set; }
        public string Email { get; set; } = string.Empty;
        public string DefaultUsername { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? MotDePasseParDefaut { get; set; }
        public string? NomComplet { get; set; }
        public string? Role { get; set; }
        public bool Created { get; set; }
        public string? Message { get; set; }
    }
}

