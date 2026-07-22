namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'autorisation pour gérer les permissions par rôle
    /// </summary>
    public interface IUserAuthorizationService
    {
        /// <summary>
        /// Vérifie si un utilisateur peut accéder à une ressource avec une action donnée
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="resource">Ressource (ex: "Societe", "Eleve", "Note")</param>
        /// <param name="action">Action (ex: "Create", "Read", "Update", "Delete")</param>
        /// <returns>True si autorisé</returns>
        Task<bool> CanAccessAsync(int userId, string resource, string action);

        /// <summary>
        /// Filtre les données selon le scope de l'utilisateur
        /// </summary>
        /// <typeparam name="T">Type d'entité</typeparam>
        /// <param name="data">Données à filtrer</param>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Données filtrées</returns>
        Task<IEnumerable<T>> FilterByScopeAsync<T>(IEnumerable<T> data, int userId);

        /// <summary>
        /// Vérifie si un utilisateur peut accéder à une entité spécifique
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="targetId">ID de l'entité cible</param>
        /// <param name="scope">Scope de vérification ("Societe", "Classe", "Cours")</param>
        /// <returns>True si dans le scope</returns>
        Task<bool> IsInScopeAsync(int userId, int targetId, string scope);

        /// <summary>
        /// Obtient les informations de scope d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Informations de scope</returns>
        Task<UserScope> GetUserScopeAsync(int userId);

        /// <summary>
        /// Applique le filtrage automatique pour une requête IQueryable
        /// </summary>
        /// <typeparam name="T">Type d'entité</typeparam>
        /// <param name="query">Requête à filtrer</param>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Requête filtrée</returns>
        Task<IQueryable<T>> ApplyScopeFilterAsync<T>(IQueryable<T> query, int userId);
    }

    /// <summary>
    /// Informations de scope d'un utilisateur
    /// </summary>
    public class UserScope
    {
        public int UserId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int? SocieteId { get; set; }
        public int? DirectionId { get; set; }
        public int? SectionId { get; set; }
        public int? OptionId { get; set; }
        public int? ClasseId { get; set; }
        public List<int> CoursIds { get; set; } = new();
        public List<int> EleveIds { get; set; } = new();
    }
}
