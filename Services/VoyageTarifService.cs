using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class VoyageTarifService : IVoyageTarifService
    {
        private readonly CongoTravelDbContext _context;

        public VoyageTarifService(CongoTravelDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<int> ResolvePrixAsync(
            int idVoyage,
            int idCategorieSiege,
            int prixFallbackVoyage,
            CancellationToken cancellationToken = default)
        {
            var row = await _context.VoyageTarifsCategorieSiege.AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idCategorieSiege,
                    cancellationToken);

            return row?.Prix ?? prixFallbackVoyage;
        }

        /// <inheritdoc />
        public async Task<decimal> ComputeTotalForSiegesAsync(
            int idVoyage,
            IReadOnlyList<int> idSiegeList,
            int prixFallbackVoyage,
            CancellationToken cancellationToken = default)
        {
            if (idSiegeList.Count == 0)
                return 0m;

            var sieges = await _context.Sieges.AsNoTracking()
                .Where(s => idSiegeList.Contains(s.IdSiege))
                .Select(s => new { s.IdSiege, s.IdCategorieSiege })
                .ToListAsync(cancellationToken);

            if (sieges.Count != idSiegeList.Distinct().Count())
                throw new InvalidOperationException("Un ou plusieurs sièges d'allocation sont introuvables.");

            var total = 0m;
            foreach (var sid in idSiegeList)
            {
                var sg = sieges.First(s => s.IdSiege == sid);
                var prix = await ResolvePrixAsync(idVoyage, sg.IdCategorieSiege, prixFallbackVoyage, cancellationToken);
                total += prix;
            }

            return total;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VoyageTarifCategorieSiege>> GetTarifsByVoyageAsync(
            int idVoyage,
            CancellationToken cancellationToken = default)
        {
            return await _context.VoyageTarifsCategorieSiege.AsNoTracking()
                .Include(t => t.CategorieSiege)
                .Where(t => t.IdVoyage == idVoyage)
                .OrderBy(t => t.CategorieSiege!.CodeCategorieSiege)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ReplaceTarifsForVoyageAsync(
            int idVoyage,
            int idSociete,
            IReadOnlyList<(int IdCategorieSiege, int Prix)> lignes,
            CancellationToken cancellationToken = default)
        {
            var voyage = await _context.Voyages.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);
            if (voyage == null)
                throw new InvalidOperationException($"Voyage {idVoyage} introuvable.");
            if (voyage.IdSociete != idSociete)
                throw new InvalidOperationException("Le voyage n'appartient pas à cette société.");

            if (lignes.Count != lignes.Select(l => l.IdCategorieSiege).Distinct().Count())
                throw new ArgumentException("Chaque catégorie de siège ne peut apparaître qu'une seule fois.");

            var categorieIds = lignes.Select(l => l.IdCategorieSiege).Distinct().ToList();
            var ok = await _context.CategorieSieges.AsNoTracking()
                .Where(c => categorieIds.Contains(c.IdCategorieSiege) && c.IdSociete == idSociete)
                .Select(c => c.IdCategorieSiege)
                .CountAsync(cancellationToken);
            if (ok != categorieIds.Count)
                throw new InvalidOperationException("Une ou plusieurs catégories de siège sont invalides pour cette société.");

            foreach (var (_, prix) in lignes)
            {
                if (prix < 0)
                    throw new ArgumentException("Le prix ne peut pas être négatif.");
            }

            var existants = await _context.VoyageTarifsCategorieSiege
                .Where(t => t.IdVoyage == idVoyage)
                .ToListAsync(cancellationToken);
            _context.VoyageTarifsCategorieSiege.RemoveRange(existants);

            var utc = DateTime.UtcNow;
            foreach (var (idCat, prix) in lignes)
            {
                _context.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
                {
                    IdVoyage = idVoyage,
                    IdCategorieSiege = idCat,
                    Prix = prix,
                    IdSociete = idSociete,
                    DateCreation = utc
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<VoyageTarifCategorieSiege> UpsertTarifForVoyageAsync(
            int idVoyage,
            int idSociete,
            int idCategorieSiege,
            int prix,
            CancellationToken cancellationToken = default)
        {
            if (prix < 0)
                throw new ArgumentException("Le prix ne peut pas être négatif.");

            var voyage = await _context.Voyages.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == idVoyage, cancellationToken);
            if (voyage == null)
                throw new InvalidOperationException($"Voyage {idVoyage} introuvable.");
            if (voyage.IdSociete != idSociete)
                throw new InvalidOperationException("Le voyage n'appartient pas à cette société.");

            var categorieOk = await _context.CategorieSieges.AsNoTracking()
                .AnyAsync(
                    c => c.IdCategorieSiege == idCategorieSiege && c.IdSociete == idSociete,
                    cancellationToken);
            if (!categorieOk)
                throw new InvalidOperationException("La catégorie de siège est invalide pour cette société.");

            var row = await _context.VoyageTarifsCategorieSiege
                .FirstOrDefaultAsync(
                    t => t.IdVoyage == idVoyage && t.IdCategorieSiege == idCategorieSiege,
                    cancellationToken);

            var utc = DateTime.UtcNow;
            if (row == null)
            {
                row = new VoyageTarifCategorieSiege
                {
                    IdVoyage = idVoyage,
                    IdCategorieSiege = idCategorieSiege,
                    Prix = prix,
                    IdSociete = idSociete,
                    DateCreation = utc
                };
                _context.VoyageTarifsCategorieSiege.Add(row);
            }
            else
            {
                row.Prix = prix;
                row.DateModification = utc;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _context.Entry(row).Reference(r => r.CategorieSiege).LoadAsync(cancellationToken);
            return row;
        }

        /// <inheritdoc />
        public Task<bool> HasTarifsForVoyageAsync(int idVoyage, CancellationToken cancellationToken = default) =>
            _context.VoyageTarifsCategorieSiege.AsNoTracking()
                .AnyAsync(t => t.IdVoyage == idVoyage, cancellationToken);

        /// <inheritdoc />
        public async Task EnsureDefaultEcoTarifAsync(
            int idVoyage,
            int idSociete,
            int prixVoyage,
            CancellationToken cancellationToken = default)
        {
            if (await _context.VoyageTarifsCategorieSiege.AnyAsync(t => t.IdVoyage == idVoyage, cancellationToken))
                return;

            var ecoId = await _context.CategorieSieges.AsNoTracking()
                .Where(c => c.IdSociete == idSociete && c.CodeCategorieSiege == "ECO" && c.Statut)
                .Select(c => c.IdCategorieSiege)
                .FirstOrDefaultAsync(cancellationToken);

            if (ecoId == 0)
                return;

            _context.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
            {
                IdVoyage = idVoyage,
                IdCategorieSiege = ecoId,
                Prix = prixVoyage,
                IdSociete = idSociete,
                DateCreation = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task SyncEcoTarifPrixAsync(
            int idVoyage,
            int idSociete,
            int nouveauPrixVoyage,
            CancellationToken cancellationToken = default)
        {
            var ecoId = await _context.CategorieSieges.AsNoTracking()
                .Where(c => c.IdSociete == idSociete && c.CodeCategorieSiege == "ECO")
                .Select(c => c.IdCategorieSiege)
                .FirstOrDefaultAsync(cancellationToken);

            if (ecoId == 0)
            {
                await EnsureDefaultEcoTarifAsync(idVoyage, idSociete, nouveauPrixVoyage, cancellationToken);
                return;
            }

            var row = await _context.VoyageTarifsCategorieSiege
                .FirstOrDefaultAsync(t => t.IdVoyage == idVoyage && t.IdCategorieSiege == ecoId, cancellationToken);

            if (row == null)
            {
                await EnsureDefaultEcoTarifAsync(idVoyage, idSociete, nouveauPrixVoyage, cancellationToken);
                return;
            }

            row.Prix = nouveauPrixVoyage;
            row.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        [Obsolete("Utiliser UpsertTarifForVoyageAsync ou ReplaceTarifsForVoyageAsync par catégorie.")]
        public async Task SyncTarifsWhenVoyagePrixChangesAsync(
            int idVoyage,
            int idSociete,
            int ancienPrixVoyage,
            int nouveauPrixVoyage,
            CancellationToken cancellationToken = default)
        {
            if (nouveauPrixVoyage < 0)
                throw new ArgumentException("Le prix ne peut pas être négatif.");

            if (ancienPrixVoyage == nouveauPrixVoyage)
                return;

            var rows = await _context.VoyageTarifsCategorieSiege
                .Where(t => t.IdVoyage == idVoyage)
                .ToListAsync(cancellationToken);

            if (rows.Count == 0)
            {
                await EnsureDefaultEcoTarifAsync(idVoyage, idSociete, nouveauPrixVoyage, cancellationToken);
                return;
            }

            var utc = DateTime.UtcNow;
            if (ancienPrixVoyage <= 0)
            {
                foreach (var row in rows)
                {
                    row.Prix = nouveauPrixVoyage;
                    row.DateModification = utc;
                }
            }
            else
            {
                var ratio = (decimal)nouveauPrixVoyage / ancienPrixVoyage;
                foreach (var row in rows)
                {
                    row.Prix = Math.Max(0, (int)Math.Round(row.Prix * ratio, MidpointRounding.AwayFromZero));
                    row.DateModification = utc;
                }
            }

            var ecoId = await ResolveEcoCategorieIdAsync(idSociete, cancellationToken);
            if (ecoId > 0 && rows.All(r => r.IdCategorieSiege != ecoId))
            {
                _context.VoyageTarifsCategorieSiege.Add(new VoyageTarifCategorieSiege
                {
                    IdVoyage = idVoyage,
                    IdCategorieSiege = ecoId,
                    Prix = nouveauPrixVoyage,
                    IdSociete = idSociete,
                    DateCreation = utc
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> ResolveReferencePrixFromTarifsAsync(
            int idVoyage,
            int idSociete,
            int prixFallbackVoyage,
            CancellationToken cancellationToken = default)
        {
            var tarifs = await _context.VoyageTarifsCategorieSiege.AsNoTracking()
                .Where(t => t.IdVoyage == idVoyage)
                .Select(t => new { t.IdCategorieSiege, t.Prix })
                .ToListAsync(cancellationToken);

            if (tarifs.Count == 0)
                return prixFallbackVoyage;

            var ecoId = await ResolveEcoCategorieIdAsync(idSociete, cancellationToken);
            if (ecoId > 0)
            {
                var ecoTarif = tarifs.FirstOrDefault(t => t.IdCategorieSiege == ecoId);
                if (ecoTarif != null)
                    return ecoTarif.Prix;
            }

            return tarifs.Min(t => t.Prix);
        }

        private async Task<int> ResolveEcoCategorieIdAsync(int idSociete, CancellationToken cancellationToken)
        {
            return await _context.CategorieSieges.AsNoTracking()
                .Where(c => c.IdSociete == idSociete && c.CodeCategorieSiege == "ECO" && c.Statut)
                .Select(c => c.IdCategorieSiege)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
