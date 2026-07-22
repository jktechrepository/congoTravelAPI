using System.Data;
using System.Linq;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class VoyageSeatAllocationService : IVoyageSeatAllocationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISiegeService _siegeService;
        private readonly ISiegeDisponibiliteService _siegeDisponibilite;
        private readonly ILogger<VoyageSeatAllocationService> _logger;

        public VoyageSeatAllocationService(
            CongoTravelDbContext context,
            ISiegeService siegeService,
            ISiegeDisponibiliteService siegeDisponibilite,
            ILogger<VoyageSeatAllocationService> logger)
        {
            _context = context;
            _siegeService = siegeService;
            _siegeDisponibilite = siegeDisponibilite;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VoyageSeatAllocation>> AllocateSeatsForPassengersAsync(
            int idVoyage,
            int idReservation,
            IReadOnlyList<(int IdReservationPassenger, int IdCategorieSiege)> allocationRequestsOrdered,
            CancellationToken cancellationToken = default)
        {
            if (allocationRequestsOrdered.Count == 0)
                return Array.Empty<VoyageSeatAllocation>();

            if (allocationRequestsOrdered.Count != allocationRequestsOrdered.Select(r => r.IdReservationPassenger).Distinct().Count())
                throw new ArgumentException("La liste des passagers contient des doublons.");
            var requestedPassengerIds = allocationRequestsOrdered.Select(r => r.IdReservationPassenger).ToList();

            // InMemory ignore les transactions ; un BeginTransaction + Rollback peut annuler les écritures selon le flux.
            // Si une transaction parente existe (ex. cash ReservationWithPaiement), on la réutilise.
            IDbContextTransaction? tx = null;
            var ownsTransaction = false;
            if (_context.Database.IsRelational() && _context.Database.CurrentTransaction == null)
            {
                tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                ownsTransaction = true;
            }

            try
            {
                var voyage = await _context.Voyages
                    .Include(v => v.Vehicule)
                    .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);

                if (voyage?.Vehicule == null)
                    throw new InvalidOperationException($"Voyage {idVoyage} ou véhicule associé introuvable.");

                await _siegeService.EnsureSeatsForVehiculeAsync(voyage.IdVehicule, cancellationToken);

                foreach (var req in allocationRequestsOrdered)
                {
                    var belongs = await _context.ReservationPassengers.AnyAsync(
                        p => p.IdReservationPassenger == req.IdReservationPassenger && p.IdReservation == idReservation,
                        cancellationToken);
                    if (!belongs)
                        throw new InvalidOperationException(
                            $"Le passager {req.IdReservationPassenger} n’appartient pas à la réservation {idReservation}.");
                }

                var requestedCategoryIds = allocationRequestsOrdered
                    .Select(r => r.IdCategorieSiege)
                    .Distinct()
                    .ToList();
                var validCategoriesCount = await _context.CategorieSieges.AsNoTracking()
                    .CountAsync(
                        c => requestedCategoryIds.Contains(c.IdCategorieSiege) && c.IdSociete == voyage.IdSociete && c.Statut,
                        cancellationToken);
                if (validCategoriesCount != requestedCategoryIds.Count)
                    throw new InvalidOperationException("Une ou plusieurs catégories de siège sont invalides pour cette société.");

                var existingForPassengers = await _context.VoyageSeatAllocations.AnyAsync(
                    a => a.IdVoyage == idVoyage
                         && requestedPassengerIds.Contains(a.IdReservationPassenger),
                    cancellationToken);
                if (existingForPassengers)
                    throw new InvalidOperationException("Un ou plusieurs passagers ont déjà une attribution sur ce voyage.");

                var indisponibles = await _siegeDisponibilite.GetIndisponibleSiegeIdsAsync(idVoyage, cancellationToken);

                var orderedFreeSiegeIds = await _context.Sieges
                    .AsNoTracking()
                    .Where(s => s.IdVehicule == voyage.IdVehicule
                                && s.EstActif
                                && s.NumeroOrdre <= voyage.Vehicule.NombreSiege)
                    .OrderBy(s => s.NumeroOrdre)
                    .Select(s => new { s.IdSiege, s.IdCategorieSiege })
                    .ToListAsync(cancellationToken);

                var freeByCategory = orderedFreeSiegeIds
                    .Where(s => !indisponibles.Contains(s.IdSiege))
                    .GroupBy(s => s.IdCategorieSiege)
                    .ToDictionary(g => g.Key, g => new Queue<int>(g.Select(x => x.IdSiege)));

                var utcNow = DateTime.UtcNow;
                var result = new List<VoyageSeatAllocation>();

                for (var i = 0; i < allocationRequestsOrdered.Count; i++)
                {
                    var req = allocationRequestsOrdered[i];
                    if (!freeByCategory.TryGetValue(req.IdCategorieSiege, out var availableForCategory) || availableForCategory.Count == 0)
                    {
                        var catCode = await _context.CategorieSieges.AsNoTracking()
                            .Where(c => c.IdCategorieSiege == req.IdCategorieSiege)
                            .Select(c => c.CodeCategorieSiege)
                            .FirstOrDefaultAsync(cancellationToken);
                        throw new InvalidOperationException(
                            $"Aucun siège disponible dans la catégorie {(catCode ?? $"#{req.IdCategorieSiege}")} pour le voyage {idVoyage}.");
                    }

                    var alloc = new VoyageSeatAllocation
                    {
                        IdVoyage = idVoyage,
                        IdSiege = availableForCategory.Dequeue(),
                        IdReservationPassenger = req.IdReservationPassenger,
                        Statut = "CONFIRME",
                        DateCreation = utcNow
                    };
                    _context.VoyageSeatAllocations.Add(alloc);
                    result.Add(alloc);
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (ownsTransaction && tx != null)
                    await tx.CommitAsync(cancellationToken);

                foreach (var a in result)
                    await _context.Entry(a).Reference(x => x.Siege).LoadAsync(cancellationToken);

                var siegeIds = string.Join(',', result.Select(a => a.IdSiege));
                _logger.LogInformation(
                    "Allocation sièges voyage {VoyageId}, réservation {ReservationId}: {Count} sièges — IdSieges=[{AllocatedSiegeIds}]",
                    idVoyage,
                    idReservation,
                    result.Count,
                    siegeIds);

                return result;
            }
            catch
            {
                if (ownsTransaction && tx != null)
                    await tx.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (ownsTransaction && tx != null)
                    await tx.DisposeAsync();
            }
        }
    }
}
