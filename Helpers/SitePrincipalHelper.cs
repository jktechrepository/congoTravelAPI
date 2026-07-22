using CongoTravel.Data;
using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public static class SitePrincipalHelper
    {
        /// <summary>
        /// Garantit un seul site principal par société (remet les autres à false).
        /// </summary>
        public static async Task EnsureSinglePrincipalAsync(
            CongoTravelDbContext context,
            int idSociete,
            int idSiteToKeep,
            CancellationToken cancellationToken = default)
        {
            var others = await context.Sites
                .Where(s => s.IdSociete == idSociete && s.IdSite != idSiteToKeep && s.IsSitePrincipal)
                .ToListAsync(cancellationToken);

            foreach (var site in others)
            {
                site.IsSitePrincipal = false;
                site.DateModification = DateTime.UtcNow;
            }
        }

        public static async Task<Site?> GetActivePrincipalSiteAsync(
            CongoTravelDbContext context,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            await context.Sites.AsNoTracking()
                .Where(s => s.IdSociete == idSociete && s.IsSitePrincipal && s.Statut)
                .OrderBy(s => s.IdSite)
                .FirstOrDefaultAsync(cancellationToken);

        public static void EnsurePrincipalStaysActive(bool isPrincipal, bool newStatut)
        {
            if (isPrincipal && !newStatut)
            {
                throw new InvalidOperationException(
                    "Impossible de désactiver le site principal. Transférez d'abord le statut de site principal à un autre site actif.");
            }
        }
    }
}
