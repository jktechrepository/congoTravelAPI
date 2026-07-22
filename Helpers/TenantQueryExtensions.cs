using CongoTravel.Models;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Extensions pour filtrer les entités multi-tenant par IdSociete.
    /// </summary>
    public static class TenantQueryExtensions
    {
        public static IQueryable<Reservation> ForSociete(this IQueryable<Reservation> query, int idSociete) =>
            query.Where(r => r.IdSociete == idSociete);

        public static IQueryable<Paiement> ForSociete(this IQueryable<Paiement> query, int idSociete) =>
            query.Where(p => p.IdSociete == idSociete);

        public static IQueryable<Billet> ForSociete(this IQueryable<Billet> query, int idSociete) =>
            query.Where(b => b.IdSociete == idSociete);

        public static IQueryable<Voyage> ForSociete(this IQueryable<Voyage> query, int idSociete) =>
            query.Where(v => v.IdSociete == idSociete);
    }
}
