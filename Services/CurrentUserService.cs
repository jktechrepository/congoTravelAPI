using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service d'accès aux informations de l'utilisateur connecté via les claims JWT
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int UserId => GetClaimAsInt(ClaimTypes.NameIdentifier, JwtRegisteredClaimNames.Sub);

        public string UserRole => GetClaim(ClaimTypes.Role) ?? string.Empty;

        /// <inheritdoc />
        public string PrimaryRole => GetClaim("primaryRole") ?? UserRole;

        public int SocieteId => GetClaimAsInt("SocieteId", "idSociete");

        public string? SocieteNom => GetClaim("SocieteNom", "societe");

        public int? TuteurId => GetClaimAsIntOrNull("IdTuteur", "TuteurId");

        public int? AgentId => GetClaimAsIntOrNull("IdAgent", "AgentId");

        public int? SiteId => GetClaimAsIntOrNull("IdSite", "idSite");

        public int? ClientId => GetClaimAsIntOrNull("IdClient", "ClientId", "idClient");

        public int? EleveId => GetClaimAsIntOrNull("EleveId");

        public string? Email => GetClaim(ClaimTypes.Email, JwtRegisteredClaimNames.Email);

        public string? UserName => GetClaim(ClaimTypes.Name, JwtRegisteredClaimNames.Name);

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public bool IsSuperAdmin =>
            string.Equals(PrimaryRole, UserRoles.SUPER_ADMIN, StringComparison.OrdinalIgnoreCase)
            || string.Equals(UserRole, UserRoles.SUPER_ADMIN, StringComparison.OrdinalIgnoreCase);

        public bool IsAdmin => UserRoles.IsAdminRole(UserRole);

        public bool IsStaff => UserRoles.IsStaffRole(UserRole);

        public bool HasFinanceAccess => UserRoles.HasFinanceAccess(UserRole);

        public bool HasPedagogieAccess => UserRoles.HasPedagogieAccess(UserRole);

        // ═══════════════════════════════════════════════════════════════════
        // MÉTHODES DE COMPATIBILITÉ POUR DASHBOARDHUB
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Méthode pour obtenir l'ID de l'utilisateur (compatibilité DashboardHub)
        /// </summary>
        public int GetUserId() => UserId;

        /// <summary>
        /// Méthode pour obtenir le rôle de l'utilisateur (compatibilité DashboardHub)
        /// </summary>
        public string GetUserRole() => UserRole;

        /// <summary>
        /// Méthode pour obtenir l'ID de la société (compatibilité DashboardHub)
        /// </summary>
        public int GetSocieteId() => SocieteId;

        /// <summary>
        /// Méthode pour obtenir le nom de la société (compatibilité DashboardHub)
        /// </summary>
        public string? GetSocieteNom() => SocieteNom;

        /// <summary>
        /// Méthode pour obtenir le nom d'utilisateur (compatibilité DashboardHub)
        /// </summary>
        public string? GetUserName() => UserName;

        // ═══════════════════════════════════════════════════════════════════
        // MÉTHODES PRIVÉES UTILITAIRES
        // ═══════════════════════════════════════════════════════════════════

        private string? GetClaim(params string[] claimTypes)
        {
            if (claimTypes == null || claimTypes.Length == 0)
            {
                return null;
            }

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
            {
                return null;
            }

            foreach (var claimType in claimTypes)
            {
                var claim = user.Claims.FirstOrDefault(c =>
                    string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(claim?.Value))
                {
                    return claim!.Value;
                }
            }

            return null;
        }

        private int GetClaimAsInt(params string[] claimTypes)
        {
            var value = GetClaim(claimTypes);
            return int.TryParse(value, out int result) ? result : 0;
        }

        private int? GetClaimAsIntOrNull(params string[] claimTypes)
        {
            var value = GetClaim(claimTypes);
            return int.TryParse(value, out int result) && result > 0 ? result : null;
        }
    }
}

