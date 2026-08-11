using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Filtres <c>IdSociete</c> pour les entités site touristique.</summary>
    public static class SiteTouristiqueTenantQueryExtensions
    {
        public static IQueryable<SiteTouristiqueLieu> ForSociete(this IQueryable<SiteTouristiqueLieu> query, int idSociete) =>
            query.Where(l => l.IdSociete == idSociete);

        public static IQueryable<SiteTouristiqueJournee> ForSociete(this IQueryable<SiteTouristiqueJournee> query, int idSociete) =>
            query.Where(j => j.IdSociete == idSociete);

        public static IQueryable<SiteTouristiqueClasse> ForSociete(this IQueryable<SiteTouristiqueClasse> query, int idSociete) =>
            query.Where(c => c.IdSociete == idSociete);

        public static IQueryable<SiteTouristiqueReservation> ForSociete(this IQueryable<SiteTouristiqueReservation> query, int idSociete) =>
            query.Where(r => r.IdSociete == idSociete);
    }
}
