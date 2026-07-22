using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface IInfoPaiementResolutionService
    {
        /// <summary>
        /// Résout la config FlexPay active pour un site demandeur :
        /// config directe, repli site principal, puis repli toute InfoPaiement active de la société.
        /// </summary>
        Task<InfoPaiementSociete> ResolveActiveForSiteAsync(
            int idSite,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
