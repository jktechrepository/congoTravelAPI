namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface du service d'accès aux informations de l'utilisateur connecté
    /// Facilite l'accès aux claims JWT sans répéter le code partout
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// ID de l'utilisateur connecté
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// Méthode pour obtenir l'ID de l'utilisateur (compatibilité)
        /// </summary>
        int GetUserId();

        /// <summary>
        /// Rôle de l'utilisateur connecté
        /// </summary>
        string UserRole { get; }

        /// <summary>
        /// Méthode pour obtenir le rôle de l'utilisateur (compatibilité)
        /// </summary>
        string GetUserRole();

        /// <summary>
        /// ID de l'école de l'utilisateur
        /// </summary>
        int SocieteId { get; }

        /// <summary>
        /// Méthode pour obtenir l'ID de la société (compatibilité)
        /// </summary>
        int GetSocieteId();

        /// <summary>
        /// Nom de l'école de l'utilisateur
        /// </summary>
        string? SocieteNom { get; }

        /// <summary>
        /// Méthode pour obtenir le nom de la société (compatibilité)
        /// </summary>
        string? GetSocieteNom();

        /// <summary>
        /// Méthode pour obtenir le nom d'utilisateur (compatibilité)
        /// </summary>
        string? GetUserName();

        /// <summary>
        /// ID du tuteur (si l'utilisateur est un parent)
        /// </summary>
        int? TuteurId { get; }

        /// <summary>
        /// ID de l'agent (si l'utilisateur est un membre du personnel)
        /// </summary>
        int? AgentId { get; }

        /// <summary>
        /// ID du site de l'utilisateur (si applicable)
        /// </summary>
        int? SiteId { get; }

        /// <summary>
        /// ID du client lié à l'utilisateur (si applicable)
        /// </summary>
        int? ClientId { get; }

        /// <summary>
        /// ID de l'élève (si l'utilisateur est un élève)
        /// </summary>
        int? EleveId { get; }

        /// <summary>
        /// Email de l'utilisateur
        /// </summary>
        string? Email { get; }

        /// <summary>
        /// Nom d'utilisateur
        /// </summary>
        string? UserName { get; }

        /// <summary>
        /// Indique si l'utilisateur est authentifié
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Indique si l'utilisateur est Super-Admin
        /// </summary>
        bool IsSuperAdmin { get; }

        /// <summary>
        /// Indique si l'utilisateur est un rôle d'administration (Super-Admin, Gerant, Sous-Directeur)
        /// </summary>
        bool IsAdmin { get; }

        /// <summary>
        /// Indique si l'utilisateur est un membre du personnel
        /// </summary>
        bool IsStaff { get; }

        /// <summary>
        /// Indique si l'utilisateur a accès à la gestion financière
        /// </summary>
        bool HasFinanceAccess { get; }

        /// <summary>
        /// Indique si l'utilisateur a accès à la gestion pédagogique
        /// </summary>
        bool HasPedagogieAccess { get; }
    }
}

