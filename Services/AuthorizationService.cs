using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'autorisation implémentant la logique de permissions par rôle
    /// </summary>
    public class AuthorizationService : IUserAuthorizationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<AuthorizationService> _logger;

        public AuthorizationService(CongoTravelDbContext context, ILogger<AuthorizationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Matrice des permissions par rôle et ressource
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, List<string>>> _permissions = new()
        {
            ["Super-Admin"] = new()
            {
                ["Societe"] = new() { "Create", "Read", "Update", "Delete" },
                ["Direction"] = new() { "Create", "Read", "Update", "Delete" },
                ["Section"] = new() { "Create", "Read", "Update", "Delete" },
                ["Option"] = new() { "Create", "Read", "Update", "Delete" },
                ["Classe"] = new() { "Create", "Read", "Update", "Delete" },
                ["Eleve"] = new() { "Create", "Read", "Update", "Delete" },
                ["Agent"] = new() { "Create", "Read", "Update", "Delete" },
                ["Utilisateur"] = new() { "Create", "Read", "Update", "Delete" },
                ["Role"] = new() { "Create", "Read", "Update", "Delete" },
                ["Note"] = new() { "Create", "Read", "Update", "Delete" },
                ["Cours"] = new() { "Create", "Read", "Update", "Delete" },
                ["Presence"] = new() { "Create", "Read", "Update", "Delete" },
                ["Frais"] = new() { "Create", "Read", "Update", "Delete" },
                ["Paiement"] = new() { "Create", "Read", "Update", "Delete" },
                ["Inscription"] = new() { "Create", "Read", "Update", "Delete" }
            },
            ["Admin"] = new()
            {
                ["Societe"] = new() { "Read" }, // Son école uniquement
                ["Direction"] = new() { "Create", "Read", "Update" },
                ["Section"] = new() { "Create", "Read", "Update" },
                ["Option"] = new() { "Create", "Read", "Update" },
                ["Classe"] = new() { "Create", "Read", "Update" },
                ["Eleve"] = new() { "Create", "Read", "Update" },
                ["Agent"] = new() { "Create", "Read", "Update" },
                ["Utilisateur"] = new() { "Create", "Read", "Update" },
                ["Note"] = new() { "Create", "Read", "Update" },
                ["Cours"] = new() { "Create", "Read", "Update" },
                ["Presence"] = new() { "Create", "Read", "Update" },
                ["Frais"] = new() { "Create", "Read", "Update" },
                ["Paiement"] = new() { "Create", "Read", "Update" },
                ["Inscription"] = new() { "Create", "Read", "Update" }
            },
            ["Personnel"] = new()
            {
                ["Societe"] = new() { "Read" }, // Son école uniquement
                ["Direction"] = new() { "Read" },
                ["Section"] = new() { "Read" },
                ["Option"] = new() { "Read" },
                ["Classe"] = new() { "Read" },
                ["Eleve"] = new() { "Read" },
                ["Agent"] = new() { "Read" },
                ["Note"] = new() { "Read" },
                ["Cours"] = new() { "Read" },
                ["Presence"] = new() { "Read" },
                ["Frais"] = new() { "Read" },
                ["Paiement"] = new() { "Read" },
                ["Inscription"] = new() { "Read" }
            },
            ["Agent"] = new()
            {
                ["Eleve"] = new() { "Read" }, // Élèves de ses cours
                ["Note"] = new() { "Create", "Read", "Update" }, // Ses cours
                ["Cours"] = new() { "Read" }, // Ses cours
                ["Presence"] = new() { "Create", "Read", "Update" }, // Ses cours
                ["Inscription"] = new() { "Read" }
            },
            ["Tuteur"] = new()
            {
                ["Eleve"] = new() { "Read" }, // Ses enfants
                ["Note"] = new() { "Read" }, // Notes de ses enfants
                ["Cours"] = new() { "Read" }, // Cours de ses enfants
                ["Presence"] = new() { "Read" }, // Présences de ses enfants
                ["Paiement"] = new() { "Create", "Read", "Update" }, // Paiements pour ses enfants
                ["Inscription"] = new() { "Read" }
            },
            ["Eleve"] = new()
            {
                ["Eleve"] = new() { "Read" }, // Lui-même
                ["Note"] = new() { "Read" }, // Ses notes
                ["Cours"] = new() { "Read" }, // Ses cours
                ["Presence"] = new() { "Read" }, // Ses présences
                ["Paiement"] = new() { "Read" }, // Ses paiements
                ["Inscription"] = new() { "Read" }
            }
        };

        public async Task<bool> CanAccessAsync(int userId, string resource, string action)
        {
            try
            {
                var user = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (user?.Role == null)
                {
                    _logger.LogWarning("Utilisateur {UserId} non trouvé ou sans rôle", userId);
                    return false;
                }

                var hasPermission = _permissions.ContainsKey(user.Role.Nom) &&
                                   _permissions[user.Role.Nom].ContainsKey(resource) &&
                                   _permissions[user.Role.Nom][resource].Contains(action);

                _logger.LogInformation("Vérification permission: User {UserId} ({Role}) -> {Resource}.{Action} = {Result}",
                    userId, user.Role.Nom, resource, action, hasPermission);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des permissions pour l'utilisateur {UserId}", userId);
                return false;
            }
        }

        public async Task<IEnumerable<T>> FilterByScopeAsync<T>(IEnumerable<T> data, int userId)
        {
            try
            {
                var userScope = await GetUserScopeAsync(userId);
                
                _logger.LogInformation("Filtrage des données pour l'utilisateur {UserId} ({Role})", 
                    userId, userScope.RoleName);

                // Le filtrage spécifique sera implémenté selon le type d'entité
                return data; // Placeholder - sera implémenté selon les besoins
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du filtrage des données pour l'utilisateur {UserId}", userId);
                return new List<T>();
            }
        }

        public async Task<bool> IsInScopeAsync(int userId, int targetId, string scope)
        {
            try
            {
                var userScope = await GetUserScopeAsync(userId);

                return scope switch
                {
                    "Societe" => userScope.SocieteId.HasValue && 
                               await IsEntityInSociete(targetId, userScope.SocieteId.Value),
                    "Classe" => userScope.ClasseId.HasValue && 
                               targetId == userScope.ClasseId.Value,
                    "Cours" => userScope.CoursIds.Contains(targetId),
                    "Eleve" => userScope.EleveIds.Contains(targetId),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du scope pour l'utilisateur {UserId}", userId);
                return false;
            }
        }

        public async Task<UserScope> GetUserScopeAsync(int userId)
        {
            try
            {
                var user = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .Include(u => u.Societe)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (user?.Role == null)
                {
                    throw new InvalidOperationException($"Utilisateur {userId} non trouvé");
                }

                var scope = new UserScope
                {
                    UserId = userId,
                    RoleName = user.Role.Nom,
                    SocieteId = user.IdSociete
                };

                // Récupérer les informations spécifiques selon le rôle
                switch (user.Role.Nom)
                {
                    case "Caissier":
                        // TODO: Ajouter une relation Utilisateur -> Caissier
                        // Pour l'instant, on utilise une logique temporaire
                        break;

                    case "Tuteur":
                        // ⚠️ NOTE: Le modèle Eleve a été supprimé
                        // Pour l'instant, on laisse vide
                        scope.EleveIds = new List<int>();
                        break;

                    case "Eleve":
                        // Dans CongoTravelAPI, les élèves et utilisateurs sont séparés
                        // On ne peut pas faire de lien direct, donc on laisse vide pour l'instant
                        break;
                }

                _logger.LogInformation("Scope utilisateur {UserId}: {Role}, École: {SocieteId}, Classe: {ClasseId}, Élèves: {EleveCount}",
                    userId, scope.RoleName, scope.SocieteId, scope.ClasseId, scope.EleveIds.Count);

                return scope;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du scope pour l'utilisateur {UserId}", userId);
                throw;
            }
        }

        public async Task<IQueryable<T>> ApplyScopeFilterAsync<T>(IQueryable<T> query, int userId)
        {
            try
            {
                var userScope = await GetUserScopeAsync(userId);

                // Le filtrage spécifique sera implémenté selon le type d'entité
                // Pour l'instant, on retourne la requête sans modification
                return query;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'application du filtre de scope pour l'utilisateur {UserId}", userId);
                return query.Where(x => false); // Retourne une requête vide en cas d'erreur
            }
        }

        #region Méthodes privées

        private async Task<bool> IsEntityInSociete(int entityId, int societeId)
        {
            // Logique spécifique selon le type d'entité
            // À implémenter selon les besoins
            return await Task.FromResult(true);
        }

        #endregion
    }
}
