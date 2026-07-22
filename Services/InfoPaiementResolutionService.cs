using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class InfoPaiementResolutionService : IInfoPaiementResolutionService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<InfoPaiementResolutionService> _logger;

        public InfoPaiementResolutionService(
            CongoTravelDbContext context,
            ILogger<InfoPaiementResolutionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<InfoPaiementSociete> ResolveActiveForSiteAsync(
            int idSite,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var direct = await FindActiveInfoPaiementOnSiteAsync(idSite, idSociete, cancellationToken);
            if (direct != null)
                return direct;

            var principalSite = await SitePrincipalHelper.GetActivePrincipalSiteAsync(
                _context, idSociete, cancellationToken);

            if (principalSite != null && principalSite.IdSite != idSite)
            {
                var principalConfig = await FindActiveInfoPaiementOnSiteAsync(
                    principalSite.IdSite, idSociete, cancellationToken);

                if (principalConfig != null)
                {
                    _logger.LogInformation(
                        "FlexPay InfoPaiement fallback — site demandeur {IdSiteDemandeur} → site principal {IdSitePrincipal} ({NomSite})",
                        idSite, principalSite.IdSite, principalSite.NomSite);
                    return principalConfig;
                }
            }

            var societeWide = await FindAnyActiveInfoPaiementInSocieteAsync(
                idSociete, excludeIdSite: idSite, cancellationToken);

            if (societeWide != null)
            {
                _logger.LogInformation(
                    "FlexPay InfoPaiement fallback société — site demandeur {IdSiteDemandeur} → site config {IdSiteConfig} (marchand {CodeMarchand})",
                    idSite, societeWide.IdSite, societeWide.CodeMarchand);
                return societeWide;
            }

            if (principalSite == null)
            {
                throw new InvalidOperationException(
                    $"Aucune configuration FlexPay active pour le site {idSite} et aucun site principal actif configuré pour la société {idSociete}.");
            }

            if (principalSite.IdSite == idSite)
            {
                throw new InvalidOperationException(
                    $"Le site {idSite} est le site principal mais n'a pas de configuration FlexPay active, et aucun autre site de la société {idSociete} n'a d'InfoPaiement active. Transférez le statut principal vers un site configuré ou créez une InfoPaiement sur ce site.");
            }

            throw new InvalidOperationException(
                $"Aucune configuration FlexPay active pour le site {idSite} ni pour le site principal {principalSite.IdSite} ({principalSite.NomSite}), et aucun autre site de la société {idSociete} n'a d'InfoPaiement active.");
        }

        private Task<InfoPaiementSociete?> FindActiveInfoPaiementOnSiteAsync(
            int idSite,
            int idSociete,
            CancellationToken cancellationToken) =>
            _context.InfoPaiementsSociete.AsNoTracking()
                .FirstOrDefaultAsync(
                    i => i.IdSite == idSite && i.IdSociete == idSociete && i.Statut,
                    cancellationToken);

        /// <summary>
        /// Repli élargi : toute InfoPaiement active sur un site actif de la société (hors site déjà testé).
        /// Priorité : site principal marqué, puis IdSite croissant.
        /// </summary>
        private async Task<InfoPaiementSociete?> FindAnyActiveInfoPaiementInSocieteAsync(
            int idSociete,
            int excludeIdSite,
            CancellationToken cancellationToken)
        {
            return await (
                from i in _context.InfoPaiementsSociete.AsNoTracking()
                join s in _context.Sites.AsNoTracking() on i.IdSite equals s.IdSite
                where i.IdSociete == idSociete
                      && i.Statut
                      && s.IdSociete == idSociete
                      && s.Statut
                      && i.IdSite != excludeIdSite
                orderby s.IsSitePrincipal descending, i.IdSite
                select i).FirstOrDefaultAsync(cancellationToken);
        }
    }
}
