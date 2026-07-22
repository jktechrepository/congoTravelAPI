namespace CongoTravel.Models.DTOs
{
    /// <summary>Granularité du filtre date sur les listes de voyages (query <c>periode</c>).</summary>
    public enum VoyageListePeriode
    {
        Jour,
        Hebdomadaire,
        Mensuel,
        /// <summary>Aucun filtre sur DateDepart (liste complète paginée).</summary>
        Tout
    }
}
