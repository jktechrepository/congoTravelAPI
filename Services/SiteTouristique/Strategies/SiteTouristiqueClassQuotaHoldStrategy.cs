using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode <c>ClassQuota</c> : incrément atomique de <c>QuantiteHold</c> par classe.</summary>
    public class SiteTouristiqueClassQuotaHoldStrategy : ISiteTouristiqueInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `SiteTouristiqueClassQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdSiteTouristiqueClassQuota` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueClassQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.ClassQuota;

        public async Task<SiteTouristiqueHoldStrategyResult> ReserveHoldAsync(
            SiteTouristiqueInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            if (journee.Status != SiteTouristiqueStatus.Published)
            {
                throw new InvalidOperationException(
                    "La journée doit être publiée pour créer un hold.");
            }

            var aggregated = ValidateAndAggregateItems(request.Items);
            var quotas = await LoadClassQuotasAsync(journee, cancellationToken);
            var codeDevise = string.IsNullOrWhiteSpace(journee.CodeDevise)
                ? "CDF"
                : journee.CodeDevise.Trim().ToUpperInvariant();

            var lines = new List<SiteTouristiqueHoldLineResult>();
            decimal montantSousTotal = 0;

            foreach (var (classId, quantity) in aggregated)
            {
                var quota = quotas.FirstOrDefault(q => q.IdSiteTouristiqueClasse == classId);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Classe {classId} non configurée pour la journée {journee.IdSiteTouristiqueJournee}.");
                }

                var reserved = await TryIncrementHoldAsync(
                    quota.IdSiteTouristiqueClassQuota,
                    quantity,
                    cancellationToken);

                if (!reserved)
                {
                    throw new SiteTouristiqueHoldConflictException(
                        $"Capacité insuffisante pour {quantity} place(s) sur la classe {classId} (journée {journee.IdSiteTouristiqueJournee}).");
                }

                lines.Add(new SiteTouristiqueHoldLineResult
                {
                    LineType = SiteTouristiqueReservationLineType.ClassQuota,
                    Quantite = quantity,
                    PrixUnitaire = quota.PrixUnitaire,
                    CodeDevise = codeDevise,
                    IdSiteTouristiqueClassQuota = quota.IdSiteTouristiqueClassQuota
                });

                montantSousTotal += quota.PrixUnitaire * quantity;
            }

            return new SiteTouristiqueHoldStrategyResult
            {
                Lines = lines,
                MontantSousTotal = montantSousTotal
            };
        }

        public static Dictionary<int, int> ValidateAndAggregateItems(IReadOnlyList<SiteTouristiqueHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Au moins un item est requis pour un hold ClassQuota.");
            }

            var aggregated = new Dictionary<int, int>();

            foreach (var item in items)
            {
                if (!item.ClassId.HasValue || item.ClassId.Value <= 0)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : classId est obligatoire sur chaque item.");
                }

                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");

                aggregated[item.ClassId.Value] = aggregated.GetValueOrDefault(item.ClassId.Value) + item.Quantity;
            }

            return aggregated;
        }

        private async Task<List<SiteTouristiqueClassQuota>> LoadClassQuotasAsync(
            SiteTouristiqueJournee journee,
            CancellationToken cancellationToken)
        {
            if (journee.ClassQuotas.Count > 0)
                return journee.ClassQuotas.ToList();

            return await _context.SiteTouristiqueClassQuotas
                .AsNoTracking()
                .Where(q => q.IdSiteTouristiqueJournee == journee.IdSiteTouristiqueJournee)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idSiteTouristiqueClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            if (_context.Database.IsRelational()
                && string.Equals(
                    _context.Database.ProviderName,
                    "Pomelo.EntityFrameworkCore.MySql",
                    StringComparison.Ordinal))
            {
                var rows = await _context.Database.ExecuteSqlRawAsync(
                    ReserveHoldSql,
                    new object[] { quantity, idSiteTouristiqueClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idSiteTouristiqueClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idSiteTouristiqueClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdSiteTouristiqueClassQuota == idSiteTouristiqueClassQuota,
                    cancellationToken);

            if (quota == null)
                return false;

            if (quota.QuantiteHold + quota.QuantiteVendue + quantity > quota.CapaciteTotale)
                return false;

            quota.QuantiteHold += quantity;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
