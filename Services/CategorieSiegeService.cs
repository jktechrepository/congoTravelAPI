using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class CategorieSiegeService : ICategorieSiegeRepository
    {
        private readonly CongoTravelDbContext _context;

        public CategorieSiegeService(CongoTravelDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<CategorieSiege>> GetBySocieteAsync(int idSociete, bool actifsSeulement = false)
        {
            var query = _context.CategorieSieges.AsNoTracking().Where(c => c.IdSociete == idSociete);
            if (actifsSeulement)
                query = query.Where(c => c.Statut);

            return await query
                .OrderBy(c => c.CodeCategorieSiege)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<CategorieSiege?> GetByIdAsync(int idCategorieSiege)
        {
            return await _context.CategorieSieges
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategorieSiege == idCategorieSiege);
        }

        /// <inheritdoc />
        public async Task<CategorieSiege> CreateAsync(CategorieSiege categorie)
        {
            var code = categorie.CodeCategorieSiege.Trim();
            var libelle = categorie.Libelle.Trim();

            var codeExiste = await _context.CategorieSieges.AnyAsync(c =>
                c.IdSociete == categorie.IdSociete &&
                c.CodeCategorieSiege.ToUpper() == code.ToUpper());
            if (codeExiste)
                throw new InvalidOperationException("Une catégorie de siège avec ce code existe déjà pour cette société.");

            categorie.CodeCategorieSiege = code;
            categorie.Libelle = libelle;
            categorie.DateCreation = DateTime.UtcNow;
            categorie.DateModification = null;

            _context.CategorieSieges.Add(categorie);
            await _context.SaveChangesAsync();
            return categorie;
        }

        /// <inheritdoc />
        public async Task<CategorieSiege?> UpdateAsync(CategorieSiege categorie)
        {
            var existing = await _context.CategorieSieges
                .FirstOrDefaultAsync(c => c.IdCategorieSiege == categorie.IdCategorieSiege);
            if (existing == null)
                return null;

            var code = categorie.CodeCategorieSiege.Trim();
            var libelle = categorie.Libelle.Trim();

            var duplicateCode = await _context.CategorieSieges.AnyAsync(c =>
                c.IdCategorieSiege != categorie.IdCategorieSiege &&
                c.IdSociete == existing.IdSociete &&
                c.CodeCategorieSiege.ToUpper() == code.ToUpper());
            if (duplicateCode)
                throw new InvalidOperationException("Une autre catégorie de siège utilise déjà ce code dans cette société.");

            existing.CodeCategorieSiege = code;
            existing.Libelle = libelle;
            existing.Statut = categorie.Statut;
            existing.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existing;
        }

        /// <inheritdoc />
        public async Task<CategorieSiege?> ToggleStatutAsync(int idCategorieSiege)
        {
            var existing = await _context.CategorieSieges
                .FirstOrDefaultAsync(c => c.IdCategorieSiege == idCategorieSiege);
            if (existing == null)
                return null;

            existing.Statut = !existing.Statut;
            existing.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int idCategorieSiege)
        {
            var existing = await _context.CategorieSieges
                .FirstOrDefaultAsync(c => c.IdCategorieSiege == idCategorieSiege);
            if (existing == null)
                return false;

            _context.CategorieSieges.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
