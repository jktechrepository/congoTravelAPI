using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Vérifie la cohérence multi-tenant : un site référencé doit appartenir à la société indiquée.
    /// </summary>
    public static class SiteSocieteValidation
    {
        /// <exception cref="InvalidOperationException">Si le site n'existe pas ou n'appartient pas à la société.</exception>
        public static async Task EnsureSiteBelongsToSocieteAsync(
            CongoTravelDbContext ctx,
            int? idSite,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (!idSite.HasValue || idSite.Value <= 0)
                return;

            var ok = await ctx.Sites.AsNoTracking()
                .AnyAsync(a => a.IdSite == idSite.Value && a.IdSociete == idSociete, cancellationToken);

            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Le site {idSite.Value} n'existe pas ou n'appartient pas à la société {idSociete}.");
            }
        }
    }
}
