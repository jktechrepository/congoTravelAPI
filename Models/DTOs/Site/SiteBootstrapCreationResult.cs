namespace CongoTravel.Models.DTOs.Site
{
    /// <summary>Résultat après création transactionnelle site + compte gérant.</summary>
    public class SiteBootstrapCreationResult
    {
        public Models.Site Site { get; set; } = null!;
        public Models.Agent GerantAgent { get; set; } = null!;
        public Models.Utilisateur GerantUtilisateur { get; set; } = null!;
        /// <summary>Mot de passe en clair pour la réponse API uniquement (ne pas journaliser).</summary>
        public string GerantMotDePasseParDefaut { get; set; } = string.Empty;
    }
}
