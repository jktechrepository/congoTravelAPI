using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class SiegeService : ISiegeService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiegeService> _logger;

        public SiegeService(CongoTravelDbContext context, ILogger<SiegeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task EnsureSeatsForVehiculeAsync(int idVehicule, CancellationToken cancellationToken = default)
        {
            await EnsureSeatsForVehiculeWithCategorieDistributionAsync(idVehicule, null, cancellationToken);
        }

        /// <inheritdoc />
        public async Task EnsureSeatsForVehiculeWithCategorieDistributionAsync(
            int idVehicule,
            IReadOnlyList<(int IdCategorieSiege, int NombreSiegeParCategorie)>? distribution,
            CancellationToken cancellationToken = default)
        {
            var vehicule = await _context.Vehicules
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVehicule == idVehicule, cancellationToken);

            if (vehicule == null)
                throw new InvalidOperationException($"Véhicule {idVehicule} introuvable.");

            if (vehicule.NombreSiege <= 0)
                return;

            var categoriesSociete = await _context.CategorieSieges
                .AsNoTracking()
                .Where(c => c.IdSociete == vehicule.IdSociete)
                .ToListAsync(cancellationToken);

            if (!categoriesSociete.Any())
                throw new InvalidOperationException($"Aucune catégorie de siège configurée pour la société {vehicule.IdSociete}.");

            var distributionEffective = await ResolveDistributionAsync(
                vehicule,
                categoriesSociete,
                distribution,
                cancellationToken);

            var targetAssignments = BuildTargetAssignments(vehicule.NombreSiege, distributionEffective);
            var categoryCodeById = categoriesSociete.ToDictionary(c => c.IdCategorieSiege, c => c.CodeCategorieSiege);

            var utcNow = DateTime.UtcNow;

            var existingSeats = await _context.Sieges
                .Where(s => s.IdVehicule == idVehicule)
                .ToListAsync(cancellationToken);

            var updates = new List<(Siege Seat, int NewCategorieId, string FinalCode)>();
            foreach (var target in targetAssignments)
            {
                if (!categoryCodeById.TryGetValue(target.IdCategorieSiege, out var codeCategorie))
                    throw new InvalidOperationException($"Catégorie siège {target.IdCategorieSiege} introuvable pour la société {vehicule.IdSociete}.");

                var seat = existingSeats.FirstOrDefault(s => s.NumeroOrdre == target.NumeroOrdre);
                var code = $"{codeCategorie}/{target.IndexDansCategorie}";

                if (seat == null)
                {
                    _context.Sieges.Add(new Siege
                    {
                        IdVehicule = idVehicule,
                        NumeroOrdre = target.NumeroOrdre,
                        CodeSiege = code,
                        EstActif = true,
                        IdSociete = vehicule.IdSociete,
                        IdCategorieSiege = target.IdCategorieSiege,
                        DateCreation = utcNow
                    });
                }
                else
                {
                    if (seat.IdCategorieSiege != target.IdCategorieSiege || seat.CodeSiege != code)
                    {
                        updates.Add((seat, target.IdCategorieSiege, code));
                    }
                }
            }

            if (updates.Count > 0)
            {
                // Phase 1: codes temporaires uniques pour casser les permutations sous index unique (IdVehicule, CodeSiege).
                foreach (var (seat, _, _) in updates)
                {
                    seat.CodeSiege = $"TMP-{seat.IdSiege}-{Guid.NewGuid():N}";
                    seat.DateModification = utcNow;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // Phase 2: codes/catégories finaux.
                foreach (var (seat, newCategorieId, finalCode) in updates)
                {
                    seat.IdCategorieSiege = newCategorieId;
                    seat.CodeSiege = finalCode;
                    seat.DateModification = utcNow;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Sièges synchronisés pour véhicule {VehiculeId} ({Count} places)", idVehicule, vehicule.NombreSiege);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyDictionary<int, List<VehiculeCategorieSiegeRepartitionDto>>> GetActiveRepartitionByVehiculeIdsAsync(
            IReadOnlyCollection<int> idVehicules,
            CancellationToken cancellationToken = default)
        {
            if (idVehicules == null || idVehicules.Count == 0)
                return new Dictionary<int, List<VehiculeCategorieSiegeRepartitionDto>>();

            var ids = idVehicules.Distinct().ToList();

            var counts = await _context.Sieges
                .AsNoTracking()
                .Where(s => ids.Contains(s.IdVehicule) && s.EstActif)
                .GroupBy(s => new { s.IdVehicule, s.IdCategorieSiege })
                .Select(g => new
                {
                    g.Key.IdVehicule,
                    g.Key.IdCategorieSiege,
                    NombreSiegeParCategorie = g.Count()
                })
                .ToListAsync(cancellationToken);

            var categorieIds = counts.Select(c => c.IdCategorieSiege).Distinct().ToList();
            var categories = categorieIds.Count == 0
                ? new Dictionary<int, CategorieSiege>()
                : await _context.CategorieSieges
                    .AsNoTracking()
                    .Where(c => categorieIds.Contains(c.IdCategorieSiege))
                    .ToDictionaryAsync(c => c.IdCategorieSiege, cancellationToken);

            var result = ids.ToDictionary(
                id => id,
                _ => new List<VehiculeCategorieSiegeRepartitionDto>());

            foreach (var row in counts)
            {
                if (!categories.TryGetValue(row.IdCategorieSiege, out var categorie))
                    continue;

                result[row.IdVehicule].Add(new VehiculeCategorieSiegeRepartitionDto
                {
                    IdCategorieSiege = row.IdCategorieSiege,
                    Libelle = categorie.Libelle,
                    CodeCategorieSiege = categorie.CodeCategorieSiege,
                    NombreSiegeParCategorie = row.NombreSiegeParCategorie
                });
            }

            foreach (var repartition in result.Values)
            {
                repartition.Sort((a, b) =>
                    string.Compare(a.CodeCategorieSiege, b.CodeCategorieSiege, StringComparison.OrdinalIgnoreCase));
            }

            return result;
        }

        private async Task<List<(int IdCategorieSiege, int NombreSiegeParCategorie)>> ResolveDistributionAsync(
            Vehicule vehicule,
            List<CategorieSiege> categoriesSociete,
            IReadOnlyList<(int IdCategorieSiege, int NombreSiegeParCategorie)>? distribution,
            CancellationToken cancellationToken)
        {
            if (distribution != null && distribution.Count > 0)
            {
                var grouped = distribution
                    .GroupBy(x => x.IdCategorieSiege)
                    .Select(g => (IdCategorieSiege: g.Key, NombreSiegeParCategorie: g.Sum(x => x.NombreSiegeParCategorie)))
                    .ToList();

                var total = grouped.Sum(x => x.NombreSiegeParCategorie);
                if (total != vehicule.NombreSiege)
                    throw new InvalidOperationException($"La somme des sièges par catégorie ({total}) doit être égale à NombreSiege ({vehicule.NombreSiege}).");

                foreach (var item in grouped)
                {
                    if (item.NombreSiegeParCategorie <= 0)
                        throw new InvalidOperationException("Chaque catégorie doit avoir un nombre de sièges strictement positif.");

                    var cat = categoriesSociete.FirstOrDefault(c => c.IdCategorieSiege == item.IdCategorieSiege);
                    if (cat == null)
                        throw new InvalidOperationException($"La catégorie {item.IdCategorieSiege} n'existe pas dans la société {vehicule.IdSociete}.");
                    if (!cat.Statut)
                        throw new InvalidOperationException($"La catégorie {cat.CodeCategorieSiege} est inactive.");
                }

                return grouped;
            }

            // Si aucune répartition fournie, essayer de conserver l'existant.
            var existing = await _context.Sieges
                .AsNoTracking()
                .Where(s => s.IdVehicule == vehicule.IdVehicule)
                .GroupBy(s => s.IdCategorieSiege)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            if (existing.Any() && existing.Sum(x => x.Count) == vehicule.NombreSiege)
            {
                return existing
                    .Select(x => (IdCategorieSiege: x.Key, NombreSiegeParCategorie: x.Count))
                    .ToList();
            }

            // Fallback legacy: tout en ECO.
            var idEco = categoriesSociete
                .Where(c => c.Statut && c.CodeCategorieSiege == "ECO")
                .Select(c => c.IdCategorieSiege)
                .FirstOrDefault();
            if (idEco == 0)
                throw new InvalidOperationException(
                    $"Aucune catégorie de siège « ECO » active pour la société {vehicule.IdSociete}. Fournissez une répartition explicite.");

            return new List<(int IdCategorieSiege, int NombreSiegeParCategorie)>
            {
                (idEco, vehicule.NombreSiege)
            };
        }

        private static List<(int NumeroOrdre, int IdCategorieSiege, int IndexDansCategorie)> BuildTargetAssignments(
            int nombreSiege,
            IReadOnlyList<(int IdCategorieSiege, int NombreSiegeParCategorie)> distribution)
        {
            var result = new List<(int NumeroOrdre, int IdCategorieSiege, int IndexDansCategorie)>(nombreSiege);
            var ordre = 1;
            foreach (var item in distribution)
            {
                for (var i = 1; i <= item.NombreSiegeParCategorie; i++)
                {
                    result.Add((ordre, item.IdCategorieSiege, i));
                    ordre++;
                }
            }

            return result;
        }
    }
}
