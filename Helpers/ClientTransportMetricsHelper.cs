using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Helpers
{
    public static class ClientTransportMetricsHelper
    {
        public static async Task<string> GetCodeDevisePrincipaleForClientAsync(
            CongoTravelDbContext context,
            int clientId,
            int fallbackSocieteId = 0,
            CancellationToken cancellationToken = default)
        {
            var societeIdFromReservation = await context.Reservations.AsNoTracking()
                .Where(r => r.IdClient == clientId && r.IdSociete > 0)
                .OrderByDescending(r => r.DateReservation)
                .Select(r => r.IdSociete)
                .FirstOrDefaultAsync(cancellationToken);

            var societeId = societeIdFromReservation > 0
                ? societeIdFromReservation
                : fallbackSocieteId;

            if (societeId <= 0)
                return "CDF";

            return await CaissierTransportMetricsHelper.GetCodeDevisePrincipaleAsync(
                context, societeId, cancellationToken);
        }
    }
}
