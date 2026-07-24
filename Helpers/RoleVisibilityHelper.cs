using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Matrice « qui peut voir quels rôles » — alignée sur <see cref="Services.RoleService.GetAllAsync(string)"/>.
    /// Les rôles listés sont <em>cachés</em> pour l'appelant (non visibles en lecture / non assignables).
    /// </summary>
    public static class RoleVisibilityHelper
    {
        /// <summary>
        /// Rôles d'agents (ou rôles assignables) invisibles pour <paramref name="callerRole"/>.
        /// Super-Admin → ensemble vide.
        /// </summary>
        public static IReadOnlySet<string> GetHiddenRoleNamesForCaller(string? callerRole)
        {
            if (string.IsNullOrWhiteSpace(callerRole))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    UserRoles.SUPER_ADMIN,
                    UserRoles.ADMIN,
                    UserRoles.GERANT
                };
            }

            if (string.Equals(callerRole, UserRoles.SUPER_ADMIN, StringComparison.OrdinalIgnoreCase))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.Equals(callerRole, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    UserRoles.SUPER_ADMIN
                };
            }

            if (string.Equals(callerRole, UserRoles.GERANT, StringComparison.OrdinalIgnoreCase))
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    UserRoles.SUPER_ADMIN,
                    UserRoles.ADMIN
                };
            }

            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                UserRoles.SUPER_ADMIN,
                UserRoles.ADMIN,
                UserRoles.GERANT
            };
        }

        /// <summary>
        /// True si un agent (ou un rôle) avec <paramref name="targetRole"/> est visible pour l'appelant.
        /// Rôle cible vide → considéré visible (données legacy).
        /// </summary>
        public static bool IsRoleVisibleToCaller(string? targetRole, string? callerRole)
        {
            if (string.IsNullOrWhiteSpace(targetRole))
                return true;

            var hidden = GetHiddenRoleNamesForCaller(callerRole);
            return !hidden.Contains(targetRole.Trim());
        }
    }
}
