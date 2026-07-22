using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Déduit l'origine d'une opération depuis la session JWT (snapshot serveur).
    /// </summary>
    public static class OrigineOperationResolver
    {
        public static string Resolve(ICurrentUserService currentUser)
        {
            if (!currentUser.IsAuthenticated)
                return OrigineOperation.INCONNU;

            return ResolveFromRole(currentUser.UserRole, currentUser.IsStaff);
        }

        public static string ResolveFromRole(string? role, bool isStaff)
        {
            if (string.IsNullOrWhiteSpace(role))
                return isStaff ? OrigineOperation.AUTRE_STAFF : OrigineOperation.INCONNU;

            return role switch
            {
                UserRoles.CLIENT => OrigineOperation.CLIENT,
                UserRoles.CAISSIER => OrigineOperation.CAISSIER,
                UserRoles.GERANT => OrigineOperation.GERANT,
                UserRoles.SOUS_DIRECTEUR => OrigineOperation.GERANT,
                UserRoles.ADMIN => OrigineOperation.ADMIN,
                UserRoles.FINANCIER => OrigineOperation.FINANCIER,
                UserRoles.SECRETAIRE => OrigineOperation.SECRETAIRE,
                UserRoles.SUPER_ADMIN => OrigineOperation.SUPER_ADMIN,
                _ when isStaff => OrigineOperation.AUTRE_STAFF,
                _ => OrigineOperation.INCONNU
            };
        }

        /// <summary>
        /// Origine pour un paiement lié à une réservation : hérite de la réservation si connue.
        /// </summary>
        public static string ResolveForPaiement(
            ICurrentUserService currentUser,
            string? reservationOrigine)
        {
            if (!string.IsNullOrWhiteSpace(reservationOrigine)
                && reservationOrigine != OrigineOperation.INCONNU)
            {
                return reservationOrigine;
            }

            return Resolve(currentUser);
        }
    }
}
