namespace CongoTravel.Helpers
{
    /// <summary>
    /// Garde-fous multi-tenant : vérifie la cohérence IdSociete route vs JWT.
    /// </summary>
    public static class TenantGuard
    {
        public static void EnsureRouteSocieteMatchesJwt(int routeIdSociete, int jwtIdSociete, bool isSuperAdmin)
        {
            if (isSuperAdmin)
                return;

            if (routeIdSociete <= 0)
                throw new UnauthorizedAccessException("Identifiant société invalide.");

            if (jwtIdSociete <= 0)
                throw new UnauthorizedAccessException("Contexte société absent du token.");

            if (routeIdSociete != jwtIdSociete)
                throw new UnauthorizedAccessException(
                    "Accès refusé : la société demandée ne correspond pas à votre contexte.");
        }

        public static int ResolveListSocieteId(int jwtIdSociete, bool isSuperAdmin, int? requestedIdSociete = null)
        {
            if (isSuperAdmin && requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
                return requestedIdSociete.Value;

            if (jwtIdSociete <= 0)
                throw new UnauthorizedAccessException("Contexte société absent du token.");

            return jwtIdSociete;
        }
    }
}
