using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode C (<c>GlobalQuota</c>) : incrément atomique de <c>QuantiteHold</c>.</summary>
    public class EvenementGlobalQuotaHoldStrategy : IEvenementInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `EvenementSessionGlobalQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdEvenementSession` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public EvenementGlobalQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.GlobalQuota;

        public async Task<EvenementHoldStrategyResult> ReserveHoldAsync(
            EvenementInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.GlobalQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie GlobalQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            if (session.Status != EvenementSessionStatus.Published)
            {
                throw new InvalidOperationException(
                    "La session doit être publiée pour créer un hold.");
            }

            var totalQuantity = ValidateAndSumItems(request.Items);
            if (request.PrixUnitaire < 0)
                throw new InvalidOperationException("Le prix unitaire ne peut pas être négatif.");

            var codeDevise = string.IsNullOrWhiteSpace(request.CodeDevise)
                ? "CDF"
                : request.CodeDevise.Trim().ToUpperInvariant();

            var reserved = await TryIncrementHoldAsync(session.IdEvenementSession, totalQuantity, cancellationToken);
            if (!reserved)
            {
                throw new EvenementHoldConflictException(
                    $"Capacité insuffisante pour {totalQuantity} place(s) sur la session {session.IdEvenementSession}.");
            }

            var line = new EvenementHoldLineResult
            {
                LineType = EvenementReservationLineType.GlobalQuota,
                Quantite = totalQuantity,
                PrixUnitaire = request.PrixUnitaire,
                CodeDevise = codeDevise
            };

            return new EvenementHoldStrategyResult
            {
                Lines = new[] { line },
                MontantSousTotal = request.PrixUnitaire * totalQuantity
            };
        }

        public static int ValidateAndSumItems(IReadOnlyList<EvenementHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis pour un hold GlobalQuota.");

            var total = 0;
            foreach (var item in items)
            {
                if (item.SeatId.HasValue || item.ClassId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Mode GlobalQuota : les items ne doivent pas contenir seatId ni classId.");
                }

                if (item.Quantity <= 0)
                    throw new InvalidOperationException("La quantité doit être strictement positive.");

                total += item.Quantity;
            }

            return total;
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idEvenementSession,
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
                    new object[] { quantity, idEvenementSession },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idEvenementSession, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idEvenementSession,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionGlobalQuotas
                .FirstOrDefaultAsync(g => g.IdEvenementSession == idEvenementSession, cancellationToken);

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
