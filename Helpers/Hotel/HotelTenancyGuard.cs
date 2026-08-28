using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Helpers.Hotel
{
    public static class HotelTenancyGuard
    {
        public static int ResolveEffectiveSocieteId(ICurrentUserService currentUser, int? requestedIdSociete = null)
        {
            if (TryResolveEffectiveSocieteId(currentUser, requestedIdSociete, out var id) && id.HasValue)
                return id.Value;
            throw new UnauthorizedAccessException("Contexte société absent du token.");
        }

        public static bool TryResolveEffectiveSocieteId(
            ICurrentUserService currentUser, int? requestedIdSociete, out int? effectiveIdSociete)
        {
            effectiveIdSociete = null;
            if (currentUser.IsSuperAdmin && requestedIdSociete is > 0)
            {
                effectiveIdSociete = requestedIdSociete;
                return true;
            }
            if (currentUser.SocieteId <= 0)
                return false;
            if (requestedIdSociete is > 0 && requestedIdSociete != currentUser.SocieteId)
                throw new UnauthorizedAccessException("Accès refusé : la société demandée ne correspond pas à votre contexte.");
            effectiveIdSociete = currentUser.SocieteId;
            return true;
        }

        public static bool TryResolveStaffTenantForCatalogList(
            ICurrentUserService currentUser, int? requestedIdSociete, out int? effectiveIdSociete)
        {
            effectiveIdSociete = null;
            if (currentUser.IsSuperAdmin && requestedIdSociete is > 0)
            {
                effectiveIdSociete = requestedIdSociete;
                return true;
            }
            if (!currentUser.IsStaff || currentUser.IsSuperAdmin || currentUser.SocieteId <= 0)
                return false;
            if (requestedIdSociete is > 0 && requestedIdSociete != currentUser.SocieteId)
                throw new UnauthorizedAccessException("Accès refusé : la société demandée ne correspond pas à votre contexte.");
            effectiveIdSociete = currentUser.SocieteId;
            return true;
        }

        public static void EnsureResourceBelongsToSociete(int resourceIdSociete, int effectiveIdSociete, bool isSuperAdmin) =>
            TenantGuard.EnsureRouteSocieteMatchesJwt(resourceIdSociete, effectiveIdSociete, isSuperAdmin);

        public static int ResolveEffectiveSocieteIdForFlexPayVerifier(
            ICurrentUserService currentUser,
            int? requestedIdSociete = null)
        {
            if (currentUser.IsSuperAdmin && requestedIdSociete is > 0)
                return requestedIdSociete.Value;

            if (IsClientVoyageur(currentUser) || !currentUser.IsStaff)
            {
                if (requestedIdSociete is > 0)
                    return requestedIdSociete.Value;
                if (currentUser.SocieteId > 0)
                    return currentUser.SocieteId;
                throw new UnauthorizedAccessException(
                    "Contexte société absent : fournir ?idSociete= (société de l'hôtel) ou un token avec société.");
            }

            return ResolveEffectiveSocieteId(currentUser, requestedIdSociete);
        }

        public static bool IsClientVoyageur(ICurrentUserService currentUser)
        {
            if (currentUser.IsStaff && !currentUser.IsSuperAdmin)
                return false;

            return string.Equals(currentUser.UserRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentUser.PrimaryRole, UserRoles.CLIENT, StringComparison.OrdinalIgnoreCase)
                || !currentUser.IsStaff;
        }

        public static void ApplyClientSelfScopeToListFilter(
            ICurrentUserService currentUser,
            HotelReservationListFilter filter)
        {
            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            filter.IdClient = null;
            filter.IdUtilisateur = currentUser.UserId > 0 ? currentUser.UserId : null;
            if (filter.IdUtilisateur == null && currentUser.ClientId is > 0)
                filter.IdClient = currentUser.ClientId;
        }

        public static void EnsureClientOwnsReservation(
            ICurrentUserService currentUser,
            int? reservationIdUtilisateur,
            int? reservationIdClient)
        {
            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            var ownsByUser = currentUser.UserId > 0
                && reservationIdUtilisateur == currentUser.UserId;
            var ownsByClient = currentUser.ClientId is > 0
                && reservationIdClient == currentUser.ClientId;

            if (!ownsByUser && !ownsByClient)
                throw new UnauthorizedAccessException("Accès refusé : cette réservation ne vous appartient pas.");
        }

        public static void EnsureClientMayQueryByClientId(ICurrentUserService currentUser, int idClient)
        {
            if (idClient <= 0)
                throw new ArgumentException("idClient doit être strictement positif.", nameof(idClient));

            if (!IsClientVoyageur(currentUser) || currentUser.IsStaff)
                return;

            if (currentUser.ClientId is not > 0)
                throw new UnauthorizedAccessException(
                    "Accès refusé : profil client absent du token (ClientId).");

            if (currentUser.ClientId.Value != idClient)
                throw new UnauthorizedAccessException(
                    "Accès refusé : vous ne pouvez lister que vos propres réservations.");
        }
    }
}
