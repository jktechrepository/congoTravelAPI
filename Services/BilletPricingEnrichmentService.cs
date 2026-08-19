using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class BilletPricingEnrichmentService : IBilletPricingEnrichmentService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IVoyageTarifService _voyageTarifService;

        public BilletPricingEnrichmentService(
            CongoTravelDbContext context,
            IVoyageTarifService voyageTarifService)
        {
            _context = context;
            _voyageTarifService = voyageTarifService;
        }

        /// <inheritdoc />
        public async Task EnrichPrixVoyageAsync(IReadOnlyList<Billet> billets, IList<BilletResponseDto> dtos)
        {
            if (billets.Count == 0 || dtos.Count == 0)
                return;

            var dtoByBilletId = dtos.ToDictionary(d => d.IdBillet);
            var societeIds = billets
                .Select(b => b.IdSociete)
                .Distinct()
                .ToList();
            var configBySocieteId = await _context.ConfigSocietes
                .AsNoTracking()
                .Where(c => societeIds.Contains(c.IdSociete))
                .ToDictionaryAsync(c => c.IdSociete);
            var siegeIdsNeedingLookup = billets
                .Where(b => b.IdSiege.HasValue && (b.Siege == null || b.Siege.IdCategorieSiege <= 0))
                .Select(b => b.IdSiege!.Value)
                .Distinct()
                .ToList();

            var categorieBySiegeId = siegeIdsNeedingLookup.Count == 0
                ? new Dictionary<int, int>()
                : await _context.Sieges
                    .AsNoTracking()
                    .Where(s => siegeIdsNeedingLookup.Contains(s.IdSiege))
                    .ToDictionaryAsync(s => s.IdSiege, s => s.IdCategorieSiege);

            foreach (var billet in billets)
            {
                if (!dtoByBilletId.TryGetValue(billet.IdBillet, out var dto))
                    continue;

                if (configBySocieteId.TryGetValue(billet.IdSociete, out var config))
                    dto.KiloBagageOffert = config.PoidsBagageParKiloOffert;

                var idVoyage = billet.Reservation?.Voyage?.Id;
                if (!idVoyage.HasValue || idVoyage.Value <= 0)
                {
                    dto.PrixVoyage = null;
                    continue;
                }

                var prixFallback = billet.Reservation!.Voyage!.Prix;
                var idCategorieSiege = ResolveIdCategorieSiege(billet, categorieBySiegeId);
                if (idCategorieSiege <= 0)
                {
                    dto.PrixVoyage = prixFallback;
                    continue;
                }

                dto.PrixVoyage = await _voyageTarifService.ResolvePrixAsync(
                    idVoyage.Value,
                    idCategorieSiege,
                    prixFallback);
            }
        }

        private static int ResolveIdCategorieSiege(
            Billet billet,
            IReadOnlyDictionary<int, int> categorieBySiegeId)
        {
            if (billet.Siege != null && billet.Siege.IdCategorieSiege > 0)
                return billet.Siege.IdCategorieSiege;

            if (billet.IdSiege.HasValue
                && categorieBySiegeId.TryGetValue(billet.IdSiege.Value, out var idCategorie))
                return idCategorie;

            return 0;
        }
    }
}
