using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service de gestion des permissions RBAC
    /// Implémente la logique métier pour vérifier, assigner et gérer les permissions
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(
            CongoTravelDbContext context,
            ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // VÉRIFICATION DES PERMISSIONS UTILISATEUR
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> UserHasPermissionAsync(int userId, string permissionName)
        {
            try
            {
                // ═══════════════════════════════════════════════════════════════════
                // LOGIQUE HYBRIDE : Permissions Personnalisées + Permissions du Rôle
                // Priorité : DENIED > GRANTED > ROLE
                // ═══════════════════════════════════════════════════════════════════

                // 1️⃣ PRIORITÉ HAUTE : Vérifier les permissions DENIED personnalisées
                var deniedCustom = await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .Where(up => up.IdUtilisateur == userId 
                              && up.Permission.Nom == permissionName 
                              && !up.IsGranted 
                              && up.Permission.Statut == true)
                    .FirstOrDefaultAsync();
                
                if (deniedCustom != null && deniedCustom.IsValid())
                {
                    _logger.LogInformation($"🚫 Permission '{permissionName}' EXPLICITEMENT RETIRÉE pour utilisateur {userId}");
                    return false; // Permission explicitement retirée (override du rôle)
                }
                
                // 2️⃣ PRIORITÉ MOYENNE : Vérifier les permissions GRANTED personnalisées
                var grantedCustom = await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .Where(up => up.IdUtilisateur == userId 
                              && up.Permission.Nom == permissionName 
                              && up.IsGranted 
                              && up.Permission.Statut == true)
                    .FirstOrDefaultAsync();
                
                if (grantedCustom != null && grantedCustom.IsValid())
                {
                    _logger.LogInformation($"✨ Permission '{permissionName}' PERSONNALISÉE ACCORDÉE pour utilisateur {userId}");
                    return true; // Permission personnalisée accordée
                }
                
                // 3️⃣ PRIORITÉ BASSE : Vérifier les permissions via TOUS les rôles actifs de l'utilisateur
                var userRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .Where(ur => ur.IdUtilisateur == userId && ur.Statut == true)
                    .ToListAsync();

                if (userRoles == null || userRoles.Count == 0)
                {
                    _logger.LogWarning($"❌ Utilisateur {userId} n'a aucun rôle actif");
                    return false;
                }

                var hasPermission = userRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Any(rp => rp.Permission.Nom == permissionName && rp.Permission.Statut == true);

                var rolesNames = string.Join(", ", userRoles.Select(ur => ur.Role.Nom));
                _logger.LogInformation($"🔐 Permission '{permissionName}' via rôles [{rolesNames}] pour utilisateur {userId}: {(hasPermission ? "✅ ACCORDÉE" : "❌ REFUSÉE")}");

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la vérification de la permission '{permissionName}' pour l'utilisateur {userId}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(int userId)
        {
            // ✨ MISE À JOUR : Utilise maintenant GetEffectiveUserPermissionsAsync
            // pour retourner les permissions effectives (Rôle + Custom)
            try
            {
                var permissions = await GetEffectiveUserPermissionsAsync(userId);
                _logger.LogInformation($"✅ {permissions.Count()} permissions effectives récupérées pour utilisateur {userId}");
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des permissions pour l'utilisateur {userId}");
                return Enumerable.Empty<string>();
            }
        }

        public async Task<IEnumerable<Permission>> GetUserPermissionsDetailedAsync(int userId)
        {
            try
            {
                var user = await _context.Utilisateurs
                    .Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (user == null || user.Role == null)
                {
                    return Enumerable.Empty<Permission>();
                }

                return user.Role.RolePermissions
                    .Where(rp => rp.Permission.Statut == true)
                    .Select(rp => rp.Permission)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des permissions détaillées pour l'utilisateur {userId}");
                return Enumerable.Empty<Permission>();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // GESTION DES PERMISSIONS
        // ═══════════════════════════════════════════════════════════════════

        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Set<Permission>()
                .Where(p => p.Statut == true)
                .OrderBy(p => p.Categorie)
                .ThenBy(p => p.Action)
                .ToListAsync();
        }

        public async Task<Permission?> GetPermissionByIdAsync(int permissionId)
        {
            return await _context.Set<Permission>()
                .FirstOrDefaultAsync(p => p.IdPermission == permissionId);
        }

        public async Task<Permission?> GetPermissionByNameAsync(string permissionName)
        {
            return await _context.Set<Permission>()
                .FirstOrDefaultAsync(p => p.Nom == permissionName);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByCategoryAsync(string category)
        {
            return await _context.Set<Permission>()
                .Where(p => p.Categorie == category && p.Statut == true)
                .OrderBy(p => p.Action)
                .ToListAsync();
        }

        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            // Vérifier si la permission existe déjà
            var existing = await GetPermissionByNameAsync(permission.Nom);
            if (existing != null)
            {
                _logger.LogWarning($"⚠️ Permission '{permission.Nom}' existe déjà");
                throw new InvalidOperationException($"Une permission avec le nom '{permission.Nom}' existe déjà");
            }

            permission.DateCreation = DateTime.Now;
            _context.Set<Permission>().Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permission '{permission.Nom}' créée avec succès (ID: {permission.IdPermission})");

            return permission;
        }

        public async Task<Permission?> UpdatePermissionAsync(Permission permission)
        {
            var existing = await GetPermissionByIdAsync(permission.IdPermission);
            if (existing == null)
            {
                _logger.LogWarning($"❌ Permission {permission.IdPermission} non trouvée");
                return null;
            }

            existing.Nom = permission.Nom;
            existing.Description = permission.Description;
            existing.Categorie = permission.Categorie;
            existing.Action = permission.Action;
            existing.Statut = permission.Statut;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permission '{existing.Nom}' mise à jour avec succès");

            return existing;
        }

        public async Task<bool> DeletePermissionAsync(int permissionId)
        {
            var permission = await GetPermissionByIdAsync(permissionId);
            if (permission == null)
            {
                _logger.LogWarning($"❌ Permission {permissionId} non trouvée");
                return false;
            }

            // Vérifier si la permission est utilisée par des rôles
            var rolesCount = await _context.Set<RolePermission>()
                .Where(rp => rp.IdPermission == permissionId)
                .CountAsync();

            if (rolesCount > 0)
            {
                _logger.LogWarning($"⚠️ Impossible de supprimer la permission '{permission.Nom}': utilisée par {rolesCount} rôle(s)");
                throw new InvalidOperationException($"Impossible de supprimer la permission '{permission.Nom}': elle est utilisée par {rolesCount} rôle(s)");
            }

            _context.Set<Permission>().Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permission '{permission.Nom}' supprimée avec succès");

            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ASSOCIATION RÔLE-PERMISSION
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, int? userId = null)
        {
            // Vérifier que le rôle et la permission existent
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning($"❌ Rôle {roleId} non trouvé");
                return false;
            }

            var permission = await GetPermissionByIdAsync(permissionId);
            if (permission == null)
            {
                _logger.LogWarning($"❌ Permission {permissionId} non trouvée");
                return false;
            }

            // Vérifier si l'association existe déjà
            var exists = await _context.Set<RolePermission>()
                .AnyAsync(rp => rp.IdRole == roleId && rp.IdPermission == permissionId);

            if (exists)
            {
                _logger.LogWarning($"⚠️ Permission '{permission.Nom}' déjà assignée au rôle '{role.Nom}'");
                return false;
            }

            // Créer l'association
            var rolePermission = new RolePermission
            {
                IdRole = roleId,
                IdPermission = permissionId,
                DateAttribution = DateTime.Now,
                IdUtilisateurAttribution = userId
            };

            _context.Set<RolePermission>().Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permission '{permission.Nom}' assignée au rôle '{role.Nom}' avec succès");

            return true;
        }

        public async Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId)
        {
            var rolePermission = await _context.Set<RolePermission>()
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.IdRole == roleId && rp.IdPermission == permissionId);

            if (rolePermission == null)
            {
                _logger.LogWarning($"❌ Association rôle {roleId} - permission {permissionId} non trouvée");
                return false;
            }

            _context.Set<RolePermission>().Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permission '{rolePermission.Permission.Nom}' retirée du rôle '{rolePermission.Role.Nom}' avec succès");

            return true;
        }

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
        {
            return await _context.Set<RolePermission>()
                .Where(rp => rp.IdRole == roleId)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Permission.Statut == true)
                .Select(rp => rp.Permission)
                .OrderBy(p => p.Categorie)
                .ThenBy(p => p.Action)
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetRolesWithPermissionAsync(int permissionId)
        {
            return await _context.Set<RolePermission>()
                .Where(rp => rp.IdPermission == permissionId)
                .Include(rp => rp.Role)
                .Where(rp => rp.Role.Statut == true)
                .Select(rp => rp.Role)
                .OrderBy(r => r.Niveau)
                .ThenBy(r => r.Nom)
                .ToListAsync();
        }

        public async Task<int> AssignMultiplePermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, int? userId = null)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning($"❌ Rôle {roleId} non trouvé");
                return 0;
            }

            int count = 0;

            foreach (var permissionId in permissionIds)
            {
                var success = await AssignPermissionToRoleAsync(roleId, permissionId, userId);
                if (success)
                {
                    count++;
                }
            }

            _logger.LogInformation($"✅ {count}/{permissionIds.Count()} permissions assignées au rôle '{role.Nom}'");

            return count;
        }

        public async Task<bool> ReplaceRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, int? userId = null)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
            {
                _logger.LogWarning($"❌ Rôle {roleId} non trouvé");
                return false;
            }

            // Supprimer toutes les permissions existantes du rôle
            var existingPermissions = await _context.Set<RolePermission>()
                .Where(rp => rp.IdRole == roleId)
                .ToListAsync();

            _context.Set<RolePermission>().RemoveRange(existingPermissions);

            // Ajouter les nouvelles permissions
            foreach (var permissionId in permissionIds)
            {
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission != null)
                {
                    var rolePermission = new RolePermission
                    {
                        IdRole = roleId,
                        IdPermission = permissionId,
                        DateAttribution = DateTime.Now,
                        IdUtilisateurAttribution = userId
                    };

                    _context.Set<RolePermission>().Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ Permissions du rôle '{role.Nom}' remplacées avec succès ({permissionIds.Count()} nouvelles permissions)");

            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GESTION DES PERMISSIONS PERSONNALISÉES PAR UTILISATEUR
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> GrantUserPermissionAsync(int userId, int permissionId, int? grantedByUserId = null, DateTime? expiresAt = null, string? comment = null)
        {
            try
            {
                // Vérifier que l'utilisateur existe
                var user = await _context.Utilisateurs.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"❌ Utilisateur {userId} non trouvé");
                    return false;
                }

                // Vérifier que la permission existe
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null)
                {
                    _logger.LogWarning($"❌ Permission {permissionId} non trouvée");
                    return false;
                }

                // Vérifier si une permission personnalisée existe déjà pour cet utilisateur
                var existing = await _context.Set<UserPermission>()
                    .FirstOrDefaultAsync(up => up.IdUtilisateur == userId && up.IdPermission == permissionId);

                if (existing != null)
                {
                    // Mettre à jour l'existante
                    existing.IsGranted = true;
                    existing.DateAttribution = DateTime.UtcNow;
                    existing.DateExpiration = expiresAt;
                    existing.Commentaire = comment;
                    existing.AttribueParIdUtilisateur = grantedByUserId;

                    _logger.LogInformation($"✅ Permission '{permission.Nom}' mise à jour (GRANTED) pour utilisateur {userId}");
                }
                else
                {
                    // Créer une nouvelle permission personnalisée
                    var userPermission = new UserPermission
                    {
                        IdUtilisateur = userId,
                        IdPermission = permissionId,
                        IsGranted = true,
                        DateAttribution = DateTime.UtcNow,
                        DateExpiration = expiresAt,
                        Commentaire = comment,
                        AttribueParIdUtilisateur = grantedByUserId
                    };

                    _context.Set<UserPermission>().Add(userPermission);
                    _logger.LogInformation($"✅ Permission '{permission.Nom}' ACCORDÉE personnellement à utilisateur {userId}");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'octroi de la permission {permissionId} à l'utilisateur {userId}");
                return false;
            }
        }

        public async Task<bool> DenyUserPermissionAsync(int userId, int permissionId, int? deniedByUserId = null, string? comment = null)
        {
            try
            {
                // Vérifier que l'utilisateur existe
                var user = await _context.Utilisateurs.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"❌ Utilisateur {userId} non trouvé");
                    return false;
                }

                // Vérifier que la permission existe
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null)
                {
                    _logger.LogWarning($"❌ Permission {permissionId} non trouvée");
                    return false;
                }

                // Vérifier si une permission personnalisée existe déjà
                var existing = await _context.Set<UserPermission>()
                    .FirstOrDefaultAsync(up => up.IdUtilisateur == userId && up.IdPermission == permissionId);

                if (existing != null)
                {
                    // Mettre à jour l'existante
                    existing.IsGranted = false;
                    existing.DateAttribution = DateTime.UtcNow;
                    existing.DateExpiration = null; // Pas d'expiration pour un DENY
                    existing.Commentaire = comment;
                    existing.AttribueParIdUtilisateur = deniedByUserId;

                    _logger.LogInformation($"🚫 Permission '{permission.Nom}' mise à jour (DENIED) pour utilisateur {userId}");
                }
                else
                {
                    // Créer une nouvelle permission personnalisée (DENY)
                    var userPermission = new UserPermission
                    {
                        IdUtilisateur = userId,
                        IdPermission = permissionId,
                        IsGranted = false,
                        DateAttribution = DateTime.UtcNow,
                        DateExpiration = null,
                        Commentaire = comment,
                        AttribueParIdUtilisateur = deniedByUserId
                    };

                    _context.Set<UserPermission>().Add(userPermission);
                    _logger.LogInformation($"🚫 Permission '{permission.Nom}' RETIRÉE personnellement à utilisateur {userId}");
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors du retrait de la permission {permissionId} à l'utilisateur {userId}");
                return false;
            }
        }

        public async Task<bool> RemoveUserPermissionOverrideAsync(int userId, int permissionId)
        {
            try
            {
                var userPermission = await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .FirstOrDefaultAsync(up => up.IdUtilisateur == userId && up.IdPermission == permissionId);

                if (userPermission == null)
                {
                    _logger.LogWarning($"⚠️ Aucune permission personnalisée trouvée pour utilisateur {userId} et permission {permissionId}");
                    return false;
                }

                _context.Set<UserPermission>().Remove(userPermission);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Override de permission '{userPermission.Permission.Nom}' supprimé pour utilisateur {userId} (retour au rôle)");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la suppression de l'override de permission pour utilisateur {userId}");
                return false;
            }
        }

        public async Task<IEnumerable<UserPermission>> GetUserCustomPermissionsAsync(int userId)
        {
            try
            {
                return await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .Include(up => up.AttribuePar)
                    .Where(up => up.IdUtilisateur == userId)
                    .OrderByDescending(up => up.DateAttribution)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des permissions personnalisées pour utilisateur {userId}");
                return Enumerable.Empty<UserPermission>();
            }
        }

        public async Task<IEnumerable<string>> GetEffectiveUserPermissionsAsync(int userId)
        {
            try
            {
                var effectivePermissions = new HashSet<string>();

                // 1️⃣ Récupérer les permissions du rôle
                // Permissions via tous les rôles actifs
                var userRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .Where(ur => ur.IdUtilisateur == userId && ur.Statut == true)
                    .ToListAsync();

                foreach (var rolePermission in userRoles.SelectMany(ur => ur.Role.RolePermissions))
                {
                    if (rolePermission.Permission.Statut == true)
                    {
                        effectivePermissions.Add(rolePermission.Permission.Nom);
                    }
                }

                // 2️⃣ Ajouter les permissions GRANTED personnalisées (valides et non expirées)
                var grantedCustom = await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .Where(up => up.IdUtilisateur == userId 
                              && up.IsGranted 
                              && up.Permission.Statut == true)
                    .ToListAsync();

                foreach (var customPerm in grantedCustom.Where(cp => cp.IsValid()))
                {
                    effectivePermissions.Add(customPerm.Permission.Nom);
                }

                // 3️⃣ Retirer les permissions DENIED personnalisées (valides et non expirées)
                var deniedCustom = await _context.Set<UserPermission>()
                    .Include(up => up.Permission)
                    .Where(up => up.IdUtilisateur == userId 
                              && !up.IsGranted 
                              && up.Permission.Statut == true)
                    .ToListAsync();

                foreach (var customPerm in deniedCustom.Where(cp => cp.IsValid()))
                {
                    effectivePermissions.Remove(customPerm.Permission.Nom);
                }

                _logger.LogInformation($"✅ {effectivePermissions.Count} permissions effectives calculées pour utilisateur {userId}");

                return effectivePermissions.OrderBy(p => p).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors du calcul des permissions effectives pour utilisateur {userId}");
                return Enumerable.Empty<string>();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Gestion des rôles utilisateur
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupère tous les rôles actifs d'un utilisateur
        /// </summary>
        public async Task<IEnumerable<Role>> GetUserRolesAsync(int userId)
        {
            try
            {
                var userRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.IdUtilisateur == userId && ur.Statut == true)
                    .Select(ur => ur.Role)
                    .ToListAsync();

                _logger.LogInformation($"✅ {userRoles.Count} rôle(s) récupéré(s) pour utilisateur {userId}");
                return userRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des rôles pour l'utilisateur {userId}");
                return Enumerable.Empty<Role>();
            }
        }

        /// <summary>
        /// Récupère le rôle principal d'un utilisateur
        /// </summary>
        public async Task<Role?> GetUserPrimaryRoleAsync(int userId)
        {
            try
            {
                // Chercher d'abord le rôle marqué comme principal
                var primaryRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.IdUtilisateur == userId && ur.Statut == true && ur.IsPrimary)
                    .Select(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (primaryRole != null)
                {
                    _logger.LogInformation($"✅ Rôle principal '{primaryRole.Nom}' trouvé pour utilisateur {userId}");
                    return primaryRole;
                }

                // Sinon, prendre le rôle avec le niveau le plus élevé (niveau le plus bas = plus élevé)
                primaryRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => ur.IdUtilisateur == userId && ur.Statut == true)
                    .OrderBy(ur => ur.Role.Niveau ?? 999)
                    .Select(ur => ur.Role)
                    .FirstOrDefaultAsync();

                if (primaryRole != null)
                {
                    _logger.LogInformation($"✅ Rôle principal (par niveau) '{primaryRole.Nom}' trouvé pour utilisateur {userId}");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Aucun rôle actif trouvé pour utilisateur {userId}");
                }

                return primaryRole;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération du rôle principal pour l'utilisateur {userId}");
                return null;
            }
        }
    }
}

