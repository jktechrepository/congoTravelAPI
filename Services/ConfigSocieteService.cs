using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.ConfigSociete;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class ConfigSocieteService : IConfigSocieteRepository
    {
        private readonly CongoTravelDbContext _context;

        public ConfigSocieteService(CongoTravelDbContext context)
        {
            _context = context;
        }

        public async Task<ConfigSociete> GetOrCreateAsync(int idSociete, CancellationToken cancellationToken = default)
        {
            var existing = await _context.ConfigSocietes
                .FirstOrDefaultAsync(c => c.IdSociete == idSociete, cancellationToken);

            if (existing != null)
                return existing;

            var config = ConfigSocieteDefaults.CreateForSociete(idSociete);
            _context.ConfigSocietes.Add(config);
            await _context.SaveChangesAsync(cancellationToken);
            return config;
        }

        public async Task<ConfigSociete?> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default) =>
            await _context.ConfigSocietes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdSociete == idSociete, cancellationToken);

        public async Task<ConfigSociete> UpdateAsync(
            int idSociete,
            ConfigSocieteUpdateDto dto,
            CancellationToken cancellationToken = default)
        {
            var societeExists = await _context.Societes.AnyAsync(s => s.IdSociete == idSociete, cancellationToken);
            if (!societeExists)
                throw new InvalidOperationException($"Société {idSociete} introuvable.");

            var config = await GetOrCreateAsync(idSociete, cancellationToken);

            config.DureeValiditeBilletJours = dto.DureeValiditeBilletJours;
            config.PenaliteReaffectationPourcentage = dto.PenaliteReaffectationPourcentage;
            config.JoursAvanceMaxReservation = dto.JoursAvanceMaxReservation;
            config.HeuresLimiteReaffectation = dto.HeuresLimiteReaffectation;
            config.HeuresOuvertureEmbarquementAvantDepart = dto.HeuresOuvertureEmbarquementAvantDepart;
            config.HeuresFermetureEmbarquementApresJourDepart = dto.HeuresFermetureEmbarquementApresJourDepart;
            config.DureeHoldFlexPayMinutes = dto.DureeHoldFlexPayMinutes;
            config.ReaffectationActive = dto.ReaffectationActive;
            config.AutoReversementPaiementElectronique = dto.AutoReversementPaiementElectronique;
            config.PourcentageReversementSite = dto.PourcentageReversementSite;
            config.FraisPlateforme = dto.FraisPlateforme;
            config.CodeDeviseFraisPlateforme = dto.CodeDeviseFraisPlateforme;
            config.MontAddPaieElectronique = dto.MontAddPaieElectronique;
            config.CodeDeviseMontAddPaieElectronique = dto.CodeDeviseMontAddPaieElectronique;
            config.PoidsBagageParKiloOffert = dto.PoidsBagageParKiloOffert;
            ConfigSocieteDefaults.Normalize(config);
            config.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return config;
        }
    }
}
