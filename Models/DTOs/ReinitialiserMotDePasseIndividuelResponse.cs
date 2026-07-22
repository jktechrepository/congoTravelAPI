namespace CongoTravel.Models.DTOs
{
    public class ReinitialiserMotDePasseIndividuelResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UtilisateurReinitialise? Utilisateur { get; set; }
    }

    public class UtilisateurReinitialise
    {
        public int IdUtilisateur { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telephone { get; set; }
        public bool DoitChangerMotDePasse { get; set; }
    }
}

