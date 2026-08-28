using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Enums;
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

        /// <summary>
        /// FlexPay poll <c>GET .../flexpay/verifier</c> : le Client voyageur achète sur la
        /// société <b>organisatrice</b> (catalogue Published cross-société) et doit pouvoir
        /// passer <c>?idSociete=</c> organisateur. Le staff reste strictement tenanté JWT.
        /// </summary>
        public static int ResolveEffectiveSocieteIdForFlexPayVerifier(
            ICurrentUserService currentUser,
            int? requestedIdSociete = null)
        {
            if (currentUser.IsSuperAdmin && requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
                return requestedIdSociete.Value;

            // Client / non-staff : autoriser idSociete organisateur (≠ JWT éventuel)
            var isClient = string.Equals(currentUser.UserRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.PrimaryRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase);
            if (isClient || !currentUser.IsStaff)
            {
                if (requestedIdSociete.HasValue && requestedIdSociete.Value > 0)
                    return requestedIdSociete.Value;
                if (currentUser.SocieteId > 0)
                    return currentUser.SocieteId;
                throw new UnauthorizedAccessException(
                    "Contexte société absent : fournir ?idSociete= (société organisatrice) ou un token avec société.");
            }

            return ResolveEffectiveSocieteId(currentUser, requestedIdSociete);
        }

        /// <summary>True si le JWT est un Client voyageur (pas staff).</summary>
        public static bool IsClientVoyageur(ICurrentUserService currentUser)
        {
            if (currentUser.IsStaff && !currentUser.IsSuperAdmin)
                return false;

            return string.Equals(currentUser.UserRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.PrimaryRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase)
                || !currentUser.IsStaff;
        }

        /// <summary>
        /// Borne la liste aux réservations du Client JWT (ignore les query idClient / idUtilisateur).
        /// </summary>
        public static void ApplyClientSelfScopeToListFilter(
            ICurrentUserService currentUser,
            EvenementReservationListFilter filter)
        {
            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            filter.IdClient = null;
            filter.IdUtilisateur = currentUser.UserId > 0 ? currentUser.UserId : null;
            if (filter.IdUtilisateur == null && currentUser.ClientId is > 0)
                filter.IdClient = currentUser.ClientId;
        }

        /// <summary>
        /// Le Client ne peut lire que ses réservations (IdUtilisateur JWT ou IdClient lié).
        /// </summary>
        public static void EnsureClientOwnsReservation(
            ICurrentUserService currentUser,
            int? reservationIdUtilisateur,
            int? reservationIdClient)
        {
            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            var ownsByUser = currentUser.UserId > 0
                && reservationIdUtilisateur.HasValue
                && reservationIdUtilisateur.Value == currentUser.UserId;
            var ownsByClient = currentUser.ClientId is > 0
                && reservationIdClient.HasValue
                && reservationIdClient.Value == currentUser.ClientId.Value;

            if (!ownsByUser && !ownsByClient)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : cette réservation ne vous appartient pas.");
            }
        }

        /// <summary>
        /// Liste cross-société par IdClient : le Client JWT ne peut interroger que son propre ClientId.
        /// Staff / Admin : n'importe quel idClient.
        /// </summary>
        public static void EnsureClientMayQueryByClientId(ICurrentUserService currentUser, int idClient)
        {
            if (idClient <= 0)
                throw new ArgumentException("idClient doit être strictement positif.", nameof(idClient));

            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            if (currentUser.ClientId is not > 0)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : profil client absent du token (ClientId).");
            }

            if (currentUser.ClientId.Value != idClient)
            {
                throw new UnauthorizedAccessException(
                    "Accès refusé : vous ne pouvez lister que vos propres réservations.");
            }
        }
    }
}
