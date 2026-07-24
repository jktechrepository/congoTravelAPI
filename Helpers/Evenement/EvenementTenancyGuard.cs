using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Garde-fous multi-tenant pour le module événementiel (JWT / Super-Admin).</summary>
    public static class EvenementTenancyGuard
    {
        public static int ResolveEffectiveSocieteId(ICurrentUserService currentUser, int? requestedIdSociete = null)
        {
            if (TryResolveEffectiveSocieteId(currentUser, requestedIdSociete, out var effectiveId)
                && effectiveId.HasValue)
            {
                return effectiveId.Value;
            }

            throw new UnauthorizedAccessException("Contexte société absent du token.");
        }

        /// <summary>
        /// Résolution stricte (écritures / holds) : tout JWT avec société est tenanté.
        /// Lève si mismatch <paramref name="requestedIdSociete"/> hors Super-Admin.
        /// </summary>
        public static bool TryResolveEffectiveSocieteId(
            ICurrentUserService currentUser,
            int? requestedIdSociete,
            out int? effectiveIdSociete)
        {
            effectiveIdSociete = null;

            if (currentUser.IsSuperAdmin && requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
            {
                effectiveIdSociete = requestedIdSociete.Value;
                return true;
            }

            var jwtSocieteId = currentUser.SocieteId;
            if (jwtSocieteId <= 0)
                return false;

            if (requestedIdSociete.HasValue && requestedIdSociete.Value > 0 && requestedIdSociete.Value != jwtSocieteId)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : la société demandée ne correspond pas à votre contexte.");
            }

            effectiveIdSociete = jwtSocieteId;
            return true;
        }

        /// <summary>
        /// Résolution pour <c>GET /api/events/sessions</c> (catalogue).
        /// Retourne un tenant seulement pour le personnel société (<see cref="ICurrentUserService.IsStaff"/>).
        /// Clients / anonymes / non-staff → <c>false</c> (catalogue Published global, filtre idSociete libre côté controller).
        /// Staff + mismatch idSociete → throw 403.
        /// </summary>
        public static bool TryResolveStaffTenantForCatalogList(
            ICurrentUserService currentUser,
            int? requestedIdSociete,
            out int? effectiveIdSociete)
        {
            effectiveIdSociete = null;

            if (currentUser.IsSuperAdmin && requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
            {
                effectiveIdSociete = requestedIdSociete.Value;
                return true;
            }

            // Catalogue libre : Client, anonyme, non-staff (même avec IdSociete JWT)
            if (!currentUser.IsStaff || currentUser.IsSuperAdmin)
                return false;

            var jwtSocieteId = currentUser.SocieteId;
            if (jwtSocieteId <= 0)
                return false;

            if (requestedIdSociete.HasValue && requestedIdSociete.Value > 0 && requestedIdSociete.Value != jwtSocieteId)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : la société demandée ne correspond pas à votre contexte.");
            }

            effectiveIdSociete = jwtSocieteId;
            return true;
        }

        public static void EnsureResourceBelongsToSociete(int resourceIdSociete, int effectiveIdSociete, bool isSuperAdmin)
        {
            TenantGuard.EnsureRouteSocieteMatchesJwt(resourceIdSociete, effectiveIdSociete, isSuperAdmin);
        }
    }
}
