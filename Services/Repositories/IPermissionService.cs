using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface du service de gestion des permissions RBAC
    /// </summary>
    public interface IPermissionService
    {
        // ═══════════════════════════════════════════════════════════════════
        // VÉRIFICATION DES PERMISSIONS UTILISATEUR
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vérifie si un utilisateur possède une permission spécifique
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="permissionName">Nom de la permission (ex: "Societe.Create")</param>
        /// <returns>True si l'utilisateur a la permission</returns>
        Task<bool> UserHasPermissionAsync(int userId, string permissionName);

        /// <summary>
        /// Récupère toutes les permissions d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des noms de permissions</returns>
        Task<IEnumerable<string>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// Récupère les permissions détaillées d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des objets Permission</returns>
        Task<IEnumerable<Permission>> GetUserPermissionsDetailedAsync(int userId);

        // ═══════════════════════════════════════════════════════════════════
        // GESTION DES PERMISSIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupère toutes les permissions du système
        /// </summary>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// Récupère une permission par son ID
        /// </summary>
        Task<Permission?> GetPermissionByIdAsync(int permissionId);

        /// <summary>
        /// Récupère une permission par son nom
        /// </summary>
        Task<Permission?> GetPermissionByNameAsync(string permissionName);

        /// <summary>
        /// Récupère les permissions d'une catégorie
        /// </summary>
        Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(string category);

        /// <summary>
        /// Crée une nouvelle permission
        /// </summary>
        Task<Permission> CreatePermissionAsync(Permission permission);

        /// <summary>
        /// Met à jour une permission existante
        /// </summary>
        Task<Permission?> UpdatePermissionAsync(Permission permission);

        /// <summary>
        /// Supprime une permission
        /// </summary>
        Task<bool> DeletePermissionAsync(int permissionId);

        // ═══════════════════════════════════════════════════════════════════
        // ASSOCIATION RÔLE-PERMISSION
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Assigne une permission à un rôle
        /// </summary>
        /// <param name="roleId">ID du rôle</param>
        /// <param name="permissionId">ID de la permission</param>
        /// <param name="userId">ID de l'utilisateur effectuant l'opération (pour audit)</param>
        /// <returns>True si l'assignation a réussi</returns>
        Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, int? userId = null);

        /// <summary>
        /// Retire une permission d'un rôle
        /// </summary>
        Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId);

        /// <summary>
        /// Récupère toutes les permissions d'un rôle
        /// </summary>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// Récupère tous les rôles ayant une permission spécifique
        /// </summary>
        Task<IEnumerable<Role>> GetRolesWithPermissionAsync(int permissionId);

        /// <summary>
        /// Assigne plusieurs permissions à un rôle en une seule opération
        /// </summary>
        Task<int> AssignMultiplePermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, int? userId = null);

        /// <summary>
        /// Remplace toutes les permissions d'un rôle par un nouveau ensemble
        /// </summary>
        Task<bool> ReplaceRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, int? userId = null);

        // ═══════════════════════════════════════════════════════════════════
        // GESTION DES PERMISSIONS PERSONNALISÉES PAR UTILISATEUR
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Ajoute une permission personnalisée à un utilisateur (en plus de celles de son rôle)
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="permissionId">ID de la permission à accorder</param>
        /// <param name="grantedByUserId">ID de l'utilisateur qui accorde la permission</param>
        /// <param name="expiresAt">Date d'expiration (optionnel, pour permissions temporaires)</param>
        /// <param name="comment">Commentaire/Raison (optionnel)</param>
        /// <returns>True si la permission a été accordée</returns>
        Task<bool> GrantUserPermissionAsync(int userId, int permissionId, int? grantedByUserId = null, DateTime? expiresAt = null, string? comment = null);

        /// <summary>
        /// Retire une permission personnalisée d'un utilisateur (override du rôle)
        /// Même si le rôle a cette permission, l'utilisateur ne l'aura plus
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="permissionId">ID de la permission à retirer</param>
        /// <param name="deniedByUserId">ID de l'utilisateur qui retire la permission</param>
        /// <param name="comment">Commentaire/Raison (optionnel)</param>
        /// <returns>True si la permission a été retirée</returns>
        Task<bool> DenyUserPermissionAsync(int userId, int permissionId, int? deniedByUserId = null, string? comment = null);

        /// <summary>
        /// Supprime une permission personnalisée (retour aux permissions du rôle)
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="permissionId">ID de la permission</param>
        /// <returns>True si l'override a été supprimé</returns>
        Task<bool> RemoveUserPermissionOverrideAsync(int userId, int permissionId);

        /// <summary>
        /// Récupère toutes les permissions personnalisées d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des UserPermission (Granted et Denied)</returns>
        Task<IEnumerable<UserPermission>> GetUserCustomPermissionsAsync(int userId);

        /// <summary>
        /// Récupère toutes les permissions EFFECTIVES d'un utilisateur
        /// Calcul : (Permissions du Rôle + Permissions Granted) - Permissions Denied
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des noms de permissions effectives</returns>
        Task<IEnumerable<string>> GetEffectiveUserPermissionsAsync(int userId);

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Gestion des rôles utilisateur
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupère tous les rôles actifs d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Liste des rôles actifs</returns>
        Task<IEnumerable<Role>> GetUserRolesAsync(int userId);

        /// <summary>
        /// Récupère le rôle principal d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Rôle principal ou null</returns>
        Task<Role?> GetUserPrimaryRoleAsync(int userId);
    }
}

