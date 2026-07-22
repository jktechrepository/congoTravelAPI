using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class SiegeDisponibiliteService : ISiegeDisponibiliteService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiegeService _siegeService;
        private readonly ILogger<SiegeDisponibiliteService> _logger;

        public SiegeDisponibiliteService(
            CongoTravelDbContext context,
            ISiegeService siegeService,
            ILogger<SiegeDisponibiliteService> logger)
        {
            _context = context;
            _siegeService = siegeService;
            _logger = logger;
        }

        public async Task<HashSet<int>> GetIndisponibleSiegeIdsAsync(int idVoyage, CancellationToken cancellationToken = default)
        {
            await PurgeExpiredHoldsAsync(cancellationToken);

            var utcNow = DateTime.UtcNow;

            var confirme = await _context.VoyageSeatAllocations
                .AsNoTracking()
                .Where(a => a.IdVoyage == idVoyage && a.Statut == "CONFIRME")
                .Select(a => a.IdSiege)
                .ToListAsync(cancellationToken);

            var holds = await _context.SiegeHoldsEnAttente
                .AsNoTracking()
                .Where(h => h.IdVoyage == idVoyage && h.ExpireAt > utcNow)
                .Select(h => h.IdSiege)
                .ToListAsync(cancellationToken);

            var set = new HashSet<int>(confirme);
            foreach (var id in holds)
                set.Add(id);

            return set;
        }

        public async Task<IReadOnlyDictionary<int, HashSet<int>>> GetIndisponibleSiegeIdsParVoyagesAsync(
            IReadOnlyList<int> idVoyages,
            CancellationToken cancellationToken = default)
        {
            if (idVoyages.Count == 0)
                return new Dictionary<int, HashSet<int>>();

            await PurgeExpiredHoldsAsync(cancellationToken);
            var idList = idVoyages.Distinct().ToList();
            var utcNow = DateTime.UtcNow;

            var result = idList.ToDictionary(id => id, _ => new HashSet<int>());

            var allocations = await _context.VoyageSeatAllocations
                .AsNoTracking()
                .Where(a => idList.Contains(a.IdVoyage) && a.Statut == "CONFIRME")
                .Select(a => new { a.IdVoyage, a.IdSiege })
                .ToListAsync(cancellationToken);

            foreach (var a in allocations)
                result[a.IdVoyage].Add(a.IdSiege);

            var holds = await _context.SiegeHoldsEnAttente
                .AsNoTracking()
                .Where(h => idList.Contains(h.IdVoyage) && h.ExpireAt > utcNow)
                .Select(h => new { h.IdVoyage, h.IdSiege })
                .ToListAsync(cancellationToken);

            foreach (var h in holds)
                result[h.IdVoyage].Add(h.IdSiege);

            return result;
        }

        public async Task<int> PurgeExpiredHoldsAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;
            var expired = await _context.SiegeHoldsEnAttente
                .Where(h => h.ExpireAt <= utcNow)
                .ToListAsync(cancellationToken);

            if (expired.Count == 0)
                return 0;

            _context.SiegeHoldsEnAttente.RemoveRange(expired);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Purged {Count} expired seat holds", expired.Count);
            return expired.Count;
        }

        public async Task<IReadOnlyList<int>> CreateHoldsForCategoriesAsync(
            int idVoyage,
            Guid idCommandeEnAttente,
            IReadOnlyList<int> idCategorieSiegeOrdered,
            int holdMinutes,
            CancellationToken cancellationToken = default)
        {
            if (idCategorieSiegeOrdered.Count == 0)
                return Array.Empty<int>();

            var voyage = await _context.Voyages
                .Include(v => v.Vehicule)
                .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);

            if (voyage?.Vehicule == null)
                throw new InvalidOperationException($"Voyage {idVoyage} ou véhicule associé introuvable.");

            await _siegeService.EnsureSeatsForVehiculeAsync(voyage.IdVehicule, cancellationToken);

            var indisponibles = await GetIndisponibleSiegeIdsAsync(idVoyage, cancellationToken);

            var orderedFree = await _context.Sieges
                .AsNoTracking()
                .Where(s => s.IdVehicule == voyage.IdVehicule
                            && s.EstActif
                            && s.NumeroOrdre <= voyage.Vehicule.NombreSiege)
                .OrderBy(s => s.NumeroOrdre)
                .Select(s => new { s.IdSiege, s.IdCategorieSiege })
                .ToListAsync(cancellationToken);

            var freeByCategory = orderedFree
                .Where(s => !indisponibles.Contains(s.IdSiege))
                .GroupBy(s => s.IdCategorieSiege)
                .ToDictionary(g => g.Key, g => new Queue<int>(g.Select(x => x.IdSiege)));

            var expireAt = DateTime.UtcNow.AddMinutes(holdMinutes);
            var heldSiegeIds = new List<int>();

            foreach (var idCategorie in idCategorieSiegeOrdered)
            {
                if (!freeByCategory.TryGetValue(idCategorie, out var queue) || queue.Count == 0)
                {
                    var catCode = await _context.CategorieSieges.AsNoTracking()
                        .Where(c => c.IdCategorieSiege == idCategorie)
                        .Select(c => c.CodeCategorieSiege)
                        .FirstOrDefaultAsync(cancellationToken);
                    throw new InvalidOperationException(
                        $"Aucun siège disponible (hold) dans la catégorie {(catCode ?? $"#{idCategorie}")} pour le voyage {idVoyage}.");
                }

                var idSiege = queue.Dequeue();
                _context.SiegeHoldsEnAttente.Add(new SiegeHoldEnAttente
                {
                    IdVoyage = idVoyage,
                    IdSiege = idSiege,
                    IdCommandeReservationEnAttente = idCommandeEnAttente,
                    ExpireAt = expireAt,
                    DateCreation = DateTime.UtcNow
                });
                heldSiegeIds.Add(idSiege);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return heldSiegeIds;
        }

        public async Task ReleaseHoldsForCommandeAsync(Guid idCommandeEnAttente, CancellationToken cancellationToken = default)
        {
            var holds = await _context.SiegeHoldsEnAttente
                .Where(h => h.IdCommandeReservationEnAttente == idCommandeEnAttente)
                .ToListAsync(cancellationToken);

            if (holds.Count == 0)
                return;

            _context.SiegeHoldsEnAttente.RemoveRange(holds);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<VoyageSeatAllocation>> ConfirmHoldsAsAllocationsAsync(
            Guid idCommandeEnAttente,
            int idVoyage,
            IReadOnlyList<int> idReservationPassengersOrdered,
            CancellationToken cancellationToken = default)
        {
            var holds = await _context.SiegeHoldsEnAttente
                .Where(h => h.IdCommandeReservationEnAttente == idCommandeEnAttente && h.IdVoyage == idVoyage)
                .OrderBy(h => h.IdSiegeHoldEnAttente)
                .ToListAsync(cancellationToken);

            if (holds.Count != idReservationPassengersOrdered.Count)
            {
                throw new InvalidOperationException(
                    $"Nombre de holds ({holds.Count}) différent du nombre de passagers ({idReservationPassengersOrdered.Count}).");
            }

            var utcNow = DateTime.UtcNow;
            if (holds.Any(h => h.ExpireAt <= utcNow))
                throw new InvalidOperationException("Les holds de sièges ont expiré avant confirmation du paiement.");

            var allocations = new List<VoyageSeatAllocation>();
            for (var i = 0; i < holds.Count; i++)
            {
                allocations.Add(new VoyageSeatAllocation
                {
                    IdVoyage = idVoyage,
                    IdSiege = holds[i].IdSiege,
                    IdReservationPassenger = idReservationPassengersOrdered[i],
                    Statut = "CONFIRME",
                    DateCreation = utcNow
                });
            }

            _context.VoyageSeatAllocations.AddRange(allocations);
            _context.SiegeHoldsEnAttente.RemoveRange(holds);
            await _context.SaveChangesAsync(cancellationToken);
            return allocations;
        }
    }
}
