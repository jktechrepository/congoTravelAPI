using CongoTravel.Data;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    /// <summary>
    /// Service pour générer des noms d'utilisateur uniques par défaut
    /// Adapté d'AkademiaAPI pour KenergieAPI
    /// </summary>
    public interface IUsernameGeneratorService
    {
        /// <summary>
        /// Génère un DefaultUsername pour un élève basé sur son matricule
        /// Format: {Matricule}{3 chiffres aléatoires} (ex: "KEL001234")
        /// </summary>
        Task<string> GenerateForEleveAsync(string matricule);

        /// <summary>
        /// Génère un DefaultUsername pour un tuteur (parent)
        /// Format: T{4 caractères alphanumériques}{année} (ex: "TA7K92025")
        /// </summary>
        Task<string> GenerateForTuteurAsync();

        /// <summary>
        /// Génère un DefaultUsername pour un agent (caissier)
        /// Format: A{4 caractères alphanumériques}{année} (ex: "AZ3P42025")
        /// </summary>
        Task<string> GenerateForAgentAsync();

        /// <summary>
        /// Génère un DefaultUsername générique pour les autres rôles
        /// Format: {4 caractères alphanumériques}{année} (ex: "K7M92025")
        /// </summary>
        Task<string> GenerateDefaultAsync();

        /// <summary>
        /// Vérifie si un DefaultUsername existe déjà dans la base de données
        /// </summary>
        Task<bool> UsernameExistsAsync(string username);
    }

    public class UsernameGeneratorService : IUsernameGeneratorService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<UsernameGeneratorService> _logger;
        private static readonly Random _random = new Random();

        // Caractères alphanumériques pour la génération (sans ambiguïté)
        private const string AlphanumericChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public UsernameGeneratorService(CongoTravelDbContext context, ILogger<UsernameGeneratorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateForEleveAsync(string matricule)
        {
            if (string.IsNullOrWhiteSpace(matricule))
            {
                throw new ArgumentException("Le matricule ne peut pas être vide", nameof(matricule));
            }

            string username;
            int attempts = 0;
            const int maxAttempts = 100;

            do
            {
                // Générer 3 chiffres aléatoires
                var randomDigits = _random.Next(100, 1000); // De 100 à 999
                username = $"{matricule}{randomDigits}";

                attempts++;
                if (attempts >= maxAttempts)
                {
                    _logger.LogError("Impossible de générer un DefaultUsername unique pour le matricule {Matricule} après {Attempts} tentatives", 
                        matricule, maxAttempts);
                    throw new InvalidOperationException($"Impossible de générer un DefaultUsername unique pour le matricule {matricule}");
                }

            } while (await UsernameExistsAsync(username));

            _logger.LogInformation("DefaultUsername généré pour élève: {Username} (Matricule: {Matricule})", username, matricule);
            return username;
        }

        public async Task<string> GenerateForTuteurAsync()
        {
            string username;
            int attempts = 0;
            const int maxAttempts = 100;

            do
            {
                // Préfixe 'T' pour Tuteur + 4 caractères alphanumériques aléatoires
                var randomChars = new string(Enumerable.Range(0, 4)
                    .Select(_ => AlphanumericChars[_random.Next(AlphanumericChars.Length)])
                    .ToArray());

                // Ajouter l'année en cours
                var currentYear = DateTime.Now.Year;
                username = $"T{randomChars}{currentYear}";

                attempts++;
                if (attempts >= maxAttempts)
                {
                    _logger.LogError("Impossible de générer un DefaultUsername unique pour tuteur après {Attempts} tentatives", maxAttempts);
                    throw new InvalidOperationException("Impossible de générer un DefaultUsername unique pour tuteur");
                }

            } while (await UsernameExistsAsync(username));

            _logger.LogInformation("DefaultUsername généré pour tuteur: {Username}", username);
            return username;
        }

        public async Task<string> GenerateForAgentAsync()
        {
            string username;
            int attempts = 0;
            const int maxAttempts = 100;

            do
            {
                // Préfixe 'A' pour Agent + 4 caractères alphanumériques aléatoires
                var randomChars = new string(Enumerable.Range(0, 4)
                    .Select(_ => AlphanumericChars[_random.Next(AlphanumericChars.Length)])
                    .ToArray());

                // Ajouter l'année en cours
                var currentYear = DateTime.Now.Year;
                username = $"A{randomChars}{currentYear}";

                attempts++;
                if (attempts >= maxAttempts)
                {
                    _logger.LogError("Impossible de générer un DefaultUsername unique pour agent après {Attempts} tentatives", maxAttempts);
                    throw new InvalidOperationException("Impossible de générer un DefaultUsername unique pour agent");
                }

            } while (await UsernameExistsAsync(username));

            _logger.LogInformation("DefaultUsername généré pour agent: {Username}", username);
            return username;
        }

        public async Task<string> GenerateDefaultAsync()
        {
            string username;
            int attempts = 0;
            const int maxAttempts = 100;

            do
            {
                // Générer 4 caractères alphanumériques aléatoires
                var randomChars = new string(Enumerable.Range(0, 4)
                    .Select(_ => AlphanumericChars[_random.Next(AlphanumericChars.Length)])
                    .ToArray());

                // Ajouter l'année en cours
                var currentYear = DateTime.Now.Year;
                username = $"{randomChars}{currentYear}";

                attempts++;
                if (attempts >= maxAttempts)
                {
                    _logger.LogError("Impossible de générer un DefaultUsername unique après {Attempts} tentatives", maxAttempts);
                    throw new InvalidOperationException("Impossible de générer un DefaultUsername unique");
                }

            } while (await UsernameExistsAsync(username));

            _logger.LogInformation("DefaultUsername générique généré: {Username}", username);
            return username;
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            return await _context.Utilisateurs
                .AnyAsync(u => u.DefaultUsername == username);
        }
    }
}

