using CongoTravel.Models;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Implémentation du repository des appareils utilisateurs
    /// </summary>
    public class UserDeviceRepository : IUserDeviceRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<UserDeviceRepository> _logger;

        public UserDeviceRepository(CongoTravelDbContext context, ILogger<UserDeviceRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDevice>> GetAllAsync()
        {
            try
            {
                return await _context.UserDevices
                    .Where(ud => ud.Statut == true)
                    .OrderByDescending(ud => ud.DateEnregistrement)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les appareils utilisateurs");
                throw;
            }
        }

        public async Task<UserDevice?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.UserDevices
                    .FirstOrDefaultAsync(ud => ud.IdUserDevice == id && ud.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'appareil utilisateur {Id}", id);
                throw;
            }
        }

        public async Task<UserDevice?> GetByFcmTokenAsync(string fcmToken)
        {
            try
            {
                return await _context.UserDevices
                    .FirstOrDefaultAsync(ud => ud.FcmToken == fcmToken && ud.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'appareil par FCM token");
                throw;
            }
        }

        public async Task<IEnumerable<UserDevice>> GetByUtilisateurIdAsync(int idUtilisateur)
        {
            try
            {
                return await _context.UserDevices
                    .Where(ud => ud.IdUtilisateur == idUtilisateur && ud.Statut == true)
                    .OrderByDescending(ud => ud.DateEnregistrement)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des appareils pour l'utilisateur {Id}", idUtilisateur);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActiveTokensByUtilisateurIdAsync(int idUtilisateur)
        {
            try
            {
                return await _context.UserDevices
                    .Where(ud => ud.IdUtilisateur == idUtilisateur && ud.Statut == true)
                    .Select(ud => ud.FcmToken)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tokens actifs pour l'utilisateur {Id}", idUtilisateur);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActiveTokensByRoleAsync(int idRole)
        {
            try
            {
                return await _context.UserDevices
                    .Include(ud => ud.Utilisateur)
                    .Where(ud => ud.Statut == true && 
                                   ud.Utilisateur != null && ud.Utilisateur.IdRole == idRole)
                    .Select(ud => ud.FcmToken)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tokens actifs pour le rôle {Id}", idRole);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActiveTokensBySocieteAsync(int idSociete)
        {
            try
            {
                return await _context.UserDevices
                    .Include(ud => ud.Utilisateur)
                    .Where(ud => ud.Statut == true && 
                                   ud.Utilisateur != null && ud.Utilisateur.IdSociete == idSociete)
                    .Select(ud => ud.FcmToken)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tokens actifs pour la société {Id}", idSociete);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetActiveTokensByClasseAsync(int idClasse)
        {
            try
            {
                return await _context.UserDevices
                    .Include(ud => ud.Utilisateur)
                    .Where(ud => ud.Statut == true && 
                                   ud.Utilisateur != null && ud.Utilisateur.IdSociete == idClasse)
                    .Select(ud => ud.FcmToken)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tokens actifs pour la classe {Id}", idClasse);
                throw;
            }
        }

        public async Task<UserDevice> CreateAsync(UserDevice userDevice)
        {
            try
            {
                userDevice.DateEnregistrement = DateTime.UtcNow;
                userDevice.Statut = true;

                _context.UserDevices.Add(userDevice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appareil utilisateur créé avec succès: {Id}", userDevice.IdUserDevice);
                return userDevice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'appareil utilisateur");
                throw;
            }
        }

        public async Task<UserDevice> CreateOrUpdateAsync(int idUtilisateur, string fcmToken, string? deviceType = null, string? deviceModel = null, string? osVersion = null)
        {
            try
            {
                var existingDevice = await GetByFcmTokenAsync(fcmToken);

                if (existingDevice != null)
                {
                    // Mettre à jour l'appareil existant
                    existingDevice.IdUtilisateur = idUtilisateur;
                    existingDevice.Statut = true;
                    existingDevice.DateDerniereUtilisation = DateTime.UtcNow;

                    if (!string.IsNullOrEmpty(deviceType))
                        existingDevice.DeviceType = deviceType;
                    if (!string.IsNullOrEmpty(deviceModel))
                        existingDevice.DeviceModel = deviceModel;
                    if (!string.IsNullOrEmpty(osVersion))
                        existingDevice.OsVersion = osVersion;

                    _context.UserDevices.Update(existingDevice);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Appareil utilisateur mis à jour: {Id}", existingDevice.IdUserDevice);
                    return existingDevice;
                }
                else
                {
                    // Créer un nouvel appareil
                    var newDevice = new UserDevice
                    {
                        IdUtilisateur = idUtilisateur,
                        FcmToken = fcmToken,
                        DeviceType = deviceType,
                        DeviceModel = deviceModel,
                        OsVersion = osVersion,
                        DateEnregistrement = DateTime.UtcNow,
                        DateDerniereUtilisation = DateTime.UtcNow,
                        Statut = true
                    };

                    return await CreateAsync(newDevice);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création/mise à jour de l'appareil utilisateur");
                throw;
            }
        }

        public async Task<UserDevice?> UpdateAsync(UserDevice userDevice)
        {
            try
            {
                var existingDevice = await GetByIdAsync(userDevice.IdUserDevice);
                if (existingDevice == null)
                    return null;

                existingDevice.FcmToken = userDevice.FcmToken;
                existingDevice.DeviceType = userDevice.DeviceType;
                existingDevice.DeviceModel = userDevice.DeviceModel;
                existingDevice.OsVersion = userDevice.OsVersion;
                existingDevice.DateDerniereUtilisation = DateTime.UtcNow;
                existingDevice.Statut = userDevice.Statut;

                _context.UserDevices.Update(existingDevice);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appareil utilisateur mis à jour: {Id}", existingDevice.IdUserDevice);
                return existingDevice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'appareil utilisateur {Id}", userDevice.IdUserDevice);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var device = await GetByIdAsync(id);
                if (device == null)
                    return false;

                device.Statut = false;
                _context.UserDevices.Update(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appareil utilisateur supprimé: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'appareil utilisateur {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteByFcmTokenAsync(string fcmToken)
        {
            try
            {
                var device = await GetByFcmTokenAsync(fcmToken);
                if (device == null)
                    return false;

                device.Statut = false;
                _context.UserDevices.Update(device);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Appareil utilisateur supprimé par FCM token: {Token}", fcmToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'appareil utilisateur par FCM token");
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.UserDevices
                    .AnyAsync(ud => ud.IdUserDevice == id && ud.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'existence de l'appareil utilisateur {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByFcmTokenAsync(string fcmToken)
        {
            try
            {
                return await _context.UserDevices
                    .AnyAsync(ud => ud.FcmToken == fcmToken && ud.Statut == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'existence de l'appareil par FCM token");
                throw;
            }
        }
    }
}
