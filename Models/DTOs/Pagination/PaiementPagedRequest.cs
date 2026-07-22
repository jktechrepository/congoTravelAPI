namespace CongoTravel.Models.DTOs.Pagination
{
    /// <summary>
    /// Pagination paiements avec filtre optionnel par regroupement d'origine (CLIENT | AGENT | INCONNU).
    /// </summary>
    public class PaiementPagedRequest : PagedRequest
    {
        /// <summary>CLIENT, AGENT ou INCONNU — filtre sur <c>Paiements.Origine</c> regroupée.</summary>
        public string? OrigineGroupe { get; set; }
    }
}
