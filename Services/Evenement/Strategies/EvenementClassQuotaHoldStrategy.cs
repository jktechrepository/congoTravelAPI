using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    /// <summary>Mode B (<c>ClassQuota</c>) : incrément atomique de <c>QuantiteHold</c> par classe.</summary>
    public class EvenementClassQuotaHoldStrategy : IEvenementInventoryHoldStrategy
    {
        private const string ReserveHoldSql = @"
UPDATE `EvenementSessionClassQuotas`
SET `QuantiteHold` = `QuantiteHold` + {0}
WHERE `IdEvenementSessionClassQuota` = {1}
  AND (`QuantiteHold` + `QuantiteVendue` + {0}) <= `CapaciteTotale`";

        private readonly CongoTravelDbContext _context;

        public EvenementClassQuotaHoldStrategy(CongoTravelDbContext context)
        {
            _context = context;
        }

        public EvenementInventoryMode SupportedMode => EvenementInventoryMode.ClassQuota;

        public async Task<EvenementHoldStrategyResult> ReserveHoldAsync(
            EvenementInventoryHoldRequest request,
            CancellationToken cancellationToken = default)
        {
            var session = request.Session;
            if (session.InventoryMode != EvenementInventoryMode.ClassQuota)
            {
                throw new InvalidOperationException(
                    $"La stratégie ClassQuota ne s'applique pas au mode {session.InventoryMode}.");
            }

            if (session.Status != EvenementSessionStatus.Published)
            {
                throw new InvalidOperationException(
                    "La session doit être publiée pour créer un hold.");
            }

            var aggregated = ValidateAndAggregateItems(request.Items);
            var quotas = await LoadSessionClassQuotasAsync(session, cancellationToken);

            var lines = new List<EvenementHoldLineResult>();
            decimal montantSousTotal = 0;

            foreach (var (classId, quantity) in aggregated)
            {
                var quota = quotas.FirstOrDefault(q => q.IdEvenementClasse == classId);
                if (quota == null)
                {
                    throw new InvalidOperationException(
                        $"Classe {classId} non configurée pour la session {session.IdEvenementSession}.");
                }

                var reserved = await TryIncrementHoldAsync(
                    quota.IdEvenementSessionClassQuota,
                    quantity,
                    cancellationToken);

                if (!reserved)
                {
                    throw new EvenementHoldConflictException(
                        $"Capacité insuffisante pour {quantity} place(s) sur la classe {classId} (session {session.IdEvenementSession}).");
                }

                lines.Add(new EvenementHoldLineResult
                {
                    LineType = EvenementReservationLineType.ClassQuota,
                    Quantite = quantity,
                    PrixUnitaire = quota.PrixUnitaire,
                    CodeDevise = quota.CodeDevise,
                    IdEvenementSessionClassQuota = quota.IdEvenementSessionClassQuota
                });

                montantSousTotal += quota.PrixUnitaire * quantity;
            }

            return new EvenementHoldStrategyResult
            {
                Lines = lines,
                MontantSousTotal = montantSousTotal
            };
        }

        public static Dictionary<int, int> ValidateAndAggregateItems(IReadOnlyList<EvenementHoldItemRequestDto> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Au moins un item est requis pour un hold ClassQuota.");
            }

            var aggregated = new Dictionary<int, int>();

            foreach (var item in items)
            {
                if (item.SeatId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Mode ClassQuota : les items ne doivent pas contenir seatId.");
                }

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

        private async Task<List<EvenementSessionClassQuota>> LoadSessionClassQuotasAsync(
            EvenementSession session,
            CancellationToken cancellationToken)
        {
            if (session.ClassQuotas.Count > 0)
                return session.ClassQuotas.ToList();

            return await _context.EvenementSessionClassQuotas
                .AsNoTracking()
                .Where(q => q.IdEvenementSession == session.IdEvenementSession)
                .ToListAsync(cancellationToken);
        }

        private async Task<bool> TryIncrementHoldAsync(
            int idEvenementSessionClassQuota,
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
                    new object[] { quantity, idEvenementSessionClassQuota },
                    cancellationToken);
                return rows > 0;
            }

            return await TryIncrementHoldViaEfAsync(idEvenementSessionClassQuota, quantity, cancellationToken);
        }

        private async Task<bool> TryIncrementHoldViaEfAsync(
            int idEvenementSessionClassQuota,
            int quantity,
            CancellationToken cancellationToken)
        {
            var quota = await _context.EvenementSessionClassQuotas
                .FirstOrDefaultAsync(
                    q => q.IdEvenementSessionClassQuota == idEvenementSessionClassQuota,
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
