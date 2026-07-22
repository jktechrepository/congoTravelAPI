using CongoTravel.Models.Evenement;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Filtres <c>IdSociete</c> pour les entités événementielles.</summary>
    public static class EvenementTenantQueryExtensions
    {
        public static IQueryable<EvenementSession> ForSociete(this IQueryable<EvenementSession> query, int idSociete) =>
            query.Where(s => s.IdSociete == idSociete);

        public static IQueryable<EvenementClasse> ForSociete(this IQueryable<EvenementClasse> query, int idSociete) =>
            query.Where(c => c.IdSociete == idSociete);

        public static IQueryable<EvenementReservation> ForSociete(this IQueryable<EvenementReservation> query, int idSociete) =>
            query.Where(r => r.IdSociete == idSociete);
    }
}
