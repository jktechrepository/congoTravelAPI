using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    public static class VoyageConfigEnrichmentHelper
    {
        public static async Task EnrichElectronicSupplementAsync(
            CongoTravelDbContext ctx,
            IReadOnlyList<VoyageResponseDto> dtos,
            CancellationToken cancellationToken = default)
        {
            if (dtos.Count == 0)
                return;

            var societeIds = dtos.Select(d => d.IdSociete).Distinct().ToList();
            var configs = await ctx.ConfigSocietes.AsNoTracking()
                .Where(c => societeIds.Contains(c.IdSociete))
                .ToDictionaryAsync(c => c.IdSociete, cancellationToken);

            foreach (var dto in dtos)
            {
                if (!configs.TryGetValue(dto.IdSociete, out var config))
                    continue;

                dto.MontAddPaieElectronique = config.MontAddPaieElectronique;
                dto.CodeDeviseMontAddPaieElectronique = config.CodeDeviseMontAddPaieElectronique;
            }
        }
    }
}
