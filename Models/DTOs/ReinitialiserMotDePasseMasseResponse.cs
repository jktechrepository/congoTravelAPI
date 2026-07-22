namespace CongoTravel.Models.DTOs
{
    public class ReinitialiserMotDePasseMasseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int NombreUtilisateursAffectes { get; set; }
        public int NombreUtilisateurs { get; set; }
        public DetailsReinitialisation? Details { get; set; }
    }

    public class DetailsReinitialisation
    {
        public string Societe { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool MotDePasseChange { get; set; }
        public bool DoitChangerMotDePasse { get; set; }
    }
}

