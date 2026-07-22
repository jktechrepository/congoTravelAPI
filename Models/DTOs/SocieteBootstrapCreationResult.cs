namespace CongoTravel.Models.DTOs
{
    /// <summary>Résultat interne après création transactionnelle société + site + gérant.</summary>
    public class SocieteBootstrapCreationResult
    {
        public Models.Societe Societe { get; set; } = null!;
        public Models.Site Site { get; set; } = null!;
        public Models.Utilisateur? AdminUtilisateur { get; set; }
        public Models.Utilisateur GerantUtilisateur { get; set; } = null!;
        public Models.Agent GerantAgent { get; set; } = null!;
        /// <summary>Mot de passe en clair pour la réponse API uniquement (ne pas journaliser).</summary>
        public string GerantMotDePasseParDefaut { get; set; } = string.Empty;

        /// <summary>Vrai si un email de bienvenue a été planifié (site avec email renseigné).</summary>
        public bool GerantWelcomeEmailQueued { get; set; }
    }
}
