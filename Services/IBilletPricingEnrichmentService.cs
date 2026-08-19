using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Services
{
    /// <summary>
    /// Calcule le prix unitaire affiché sur chaque billet (tarif catégorie du siège attribué).
    /// </summary>
    public interface IBilletPricingEnrichmentService
    {
        /// <summary>
        /// Renseigne les champs dérivés de config et de tarification billet
        /// à partir des entités billet (appariement par <see cref="Billet.IdBillet"/>).
        /// </summary>
        Task EnrichPrixVoyageAsync(IReadOnlyList<Billet> billets, IList<BilletResponseDto> dtos);
    }
}
