using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    /// <summary>Mode <c>GlobalQuota</c> : incrément atomique de <c>QuantiteHold</c>.</summary>
    public class SiteTouristiqueGlobalQuotaHoldStrategy : ISiteTouristiqueInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `SiteTouristiqueGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdSiteTouristiqueJournee` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public SiteTouristiqueGlobalQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public SiteTouristiqueInventoryMode SupportedMode => SiteTouristiqueInventoryMode.GlobalQuota;

        public async Task<SiteTouristiqueHoldStrategyResult> ReserveHoldAsync(
            SiteTouristiqueInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var journee = request.Journee;
            if (journee.InventoryMode != SiteTouristiqueInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {journee.InventoryMode}.");
            }

            if (journee.Status != SiteTouristiqueStatus.Published)
            {
                throw new InvalidOperationException(
                    "La journée doit être publiée pour créer un hold.");
            }

            var totalQuantity = ValidateAndSumItems(request.Items);
            if (request.PrixUnitaire < 0)
                throw new InvalidOperationException("Le prix unitaire ne peut pas être négatif.");

            var codeDevise = string.IsNullOrWhiteSpace(request.CodeDevise)
                ? journee.CodeDevise
                : request.CodeDevise.Trim().ToUpperInvariant();

            var reserved = await TryIncrementHoldAsync(journee.IdSiteTouristiqueJournee, totalQuantity, cancellationToken);
            if (!reserved)
            {
                throw new SiteTouristiqueHoldConflictException(
                    $"Capacité insuffisante pour {totalQuantity} place(s) sur la journée {journee.IdSiteTouristiqueJournee}.");
            }

            var line = new SiteTouristiqueHoldLineResult
            {
                LineType = SiteTouristiqueReservationLineType.GlobalQuota,
                Quantite = totalQuantity,
                PrixUnitaire = request.PrixUnitaire,
                CodeDevise = codeDevise
            };

            return new SiteTouristiqueHoldStrategyResult
            {
                Lines = new[] { line },
                MontantSousTotal = request.PrixUnitaire * totalQuantity
            };
        }

        public static int ValidateAndSumItems(IReadOnlyList<SiteTouristiqueHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis pour un hold GlobalQuota.");

            var total = 0;
            foreach (var item in items)
            {
                if (item.ClassId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : les items ne doivent pas contenir classId.");
                }

                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");

                total += item.Quantity;
            }

            return total;
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idSiteTouristiqueJournee,
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
                    new object[] { quantity, idSiteTouristiqueJournee },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idSiteTouristiqueJournee, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idSiteTouristiqueJournee,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.SiteTouristiqueGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdSiteTouristiqueJournee == idSiteTouristiqueJournee, cancellationToken);

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
