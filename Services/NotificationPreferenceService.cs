using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class NotificationPreferenceService : INotificationPreferenceRepository
    {
        private readonly CongoTravelDbContext _context;

        public NotificationPreferenceService(CongoTravelDbContext context)
        {
            _context = context;
        }

        public async Task<NotificationPreference?> GetByUtilisateurAsync(int idUtilisateur)
        {
            return await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.IdUtilisateur == idUtilisateur);
        }

        public async Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference)
        {
            var existing = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.IdUtilisateur == preference.IdUtilisateur);

            if (existing != null)
            {
                // Mise à jour
                existing.AllowPush = preference.AllowPush;
                existing.AllowInApp = preference.AllowInApp;
                existing.AllowSms = preference.AllowSms;
                existing.AllowEmail = preference.AllowEmail;
                existing.OptOutGlobal = preference.OptOutGlobal;
                existing.OptOutFactures = preference.OptOutFactures;
                existing.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();
                return existing;
            }
            else
            {
                // Création
                preference.DateCreation = DateTime.Now;
                preference.DateModification = DateTime.Now;
                _context.NotificationPreferences.Add(preference);
                await _context.SaveChangesAsync();
                return preference;
            }
        }

        public async Task<bool> DeleteAsync(int idUtilisateur)
        {
            var preference = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.IdUtilisateur == idUtilisateur);

            if (preference == null)
                return false;

            _context.NotificationPreferences.Remove(preference);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

