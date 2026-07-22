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
        /// Renseigne <see cref="BilletResponseDto.PrixVoyage"/> pour chaque DTO à partir des entités billet (appariement par <see cref="Billet.IdBillet"/>).
        /// </summary>
        Task EnrichPrixVoyageAsync(IReadOnlyList<Billet> billets, IList<BilletResponseDto> dtos);
    }
}
