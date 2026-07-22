using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Garde-fous multi-tenant pour le module événementiel (JWT / Super-Admin).</summary>
    public static class EvenementTenancyGuard
    {
        public static int ResolveEffectiveSocieteId(ICurrentUserService currentUser, int? requestedIdSociete = null)
        {
            if (currentUser.IsSuperAdmin && requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
                return requestedIdSociete.Value;

            var jwtSocieteId = currentUser.SocieteId;
            if (jwtSocieteId <= 0)
                throw new UnauthorizedAccessException("Contexte société absent du token.");

            if (requestedIdSociete.HasValue && requestedIdSociete.Value > 0 && requestedIdSociete.Value != jwtSocieteId)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : la société demandée ne correspond pas à votre contexte.");
            }

            return jwtSocieteId;
        }

        public static void EnsureResourceBelongsToSociete(int resourceIdSociete, int effectiveIdSociete, bool isSuperAdmin)
        {
            TenantGuard.EnsureRouteSocieteMatchesJwt(resourceIdSociete, effectiveIdSociete, isSuperAdmin);
        }
    }
}
