using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers
{
    /// <summary>Garde-fous multi-tenant pour la lecture des devises et taux de change.</summary>
    public static class DeviseTenancyGuard
    {
        /// <summary>
        /// Super-Admin : toute société.
        /// Client : lecture cross-société (prévisualisation paiement FlexPay).
        /// Staff : uniquement la société JWT.
        /// </summary>
        public static bool CanReadDeviseDataForSociete(ICurrentUserService user, int idSociete)
        {
            if (user.IsSuperAdmin)
                return true;

            if (string.Equals(user.UserRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase))
                return idSociete > 0;

            return user.SocieteId == idSociete;
        }
    }
}
