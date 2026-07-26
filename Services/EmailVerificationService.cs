using System.Security.Cryptography;
using System.Text;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        public static readonly TimeSpan TokenValidity = TimeSpan.FromHours(24);
        private const int MaxAttempts = 10;
        private const string SyntheticEmailSuffix = "@congotravel.local";

        private readonly CongoTravelDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailVerificationService> _logger;

        public EmailVerificationService(
            CongoTravelDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<EmailVerificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> IssueAndSendAsync(Utilisateur utilisateur, CancellationToken cancellationToken = default)
        {
            if (utilisateur.IdUtilisateur <= 0)
                throw new InvalidOperationException("Utilisateur non persisté.");

            var email = utilisateur.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || IsSyntheticEmail(email))
            {
                _logger.LogInformation(
                    "Vérification email ignorée (email absent ou synthétique) pour utilisateur {UserId}",
                    utilisateur.IdUtilisateur);
                return false;
            }

            if (utilisateur.EmailVerified == true)
            {
                _logger.LogInformation("Email déjà vérifié pour utilisateur {UserId}", utilisateur.IdUtilisateur);
                return false;
            }

            var rawToken = CreateRawToken();
            var hash = HashToken(rawToken);

            var anciens = await _context.EmailVerificationTokens
                .Where(t => t.IdUtilisateur == utilisateur.IdUtilisateur && t.DateUtilisation == null)
                .ToListAsync(cancellationToken);

            foreach (var t in anciens)
                t.DateUtilisation = DateTime.UtcNow;

            _context.EmailVerificationTokens.Add(new EmailVerificationToken
            {
                IdUtilisateur = utilisateur.IdUtilisateur,
                CodeHash = hash,
                DateCreation = DateTime.UtcNow,
                DateExpiration = DateTime.UtcNow.Add(TokenValidity),
                AttemptCount = 0
            });

            utilisateur.EmailVerified = false;
            await _context.SaveChangesAsync(cancellationToken);

            var verifyUrl = BuildVerifyUrl(rawToken);
            var nom = string.IsNullOrWhiteSpace(utilisateur.NomComplet) ? "Client" : utilisateur.NomComplet!;

            try
            {
                var sent = await _emailService.SendEmailVerificationLinkAsync(email, nom, verifyUrl);
                if (!sent)
                    _logger.LogWarning("Échec envoi lien vérification email à {Email} (User {UserId})", email, utilisateur.IdUtilisateur);
                else
                    _logger.LogInformation("Lien vérification email envoyé à {Email} (User {UserId})", email, utilisateur.IdUtilisateur);
                return sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi lien vérification à {Email}", email);
                return false;
            }
        }

        public async Task<(bool Success, int StatusCode, string Message)> VerifyAsync(
            string rawToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
                return (false, 400, "Token de vérification requis.");

            var hash = HashToken(rawToken.Trim());
            var token = await _context.EmailVerificationTokens
                .Include(t => t.Utilisateur)
                .FirstOrDefaultAsync(t => t.CodeHash == hash, cancellationToken);

            if (token == null)
                return (false, 400, "Lien de vérification invalide.");

            if (token.Utilise)
                return (false, 400, "Ce lien a déjà été utilisé.");

            if (token.AttemptCount >= MaxAttempts)
                return (false, 400, "Trop de tentatives pour ce lien. Demandez un nouvel email.");

            if (token.EstExpire)
            {
                token.AttemptCount++;
                await _context.SaveChangesAsync(cancellationToken);
                return (false, 400, "Ce lien a expiré. Demandez un nouvel email de vérification.");
            }

            var utilisateur = token.Utilisateur;
            if (utilisateur == null || utilisateur.Statut == false)
                return (false, 400, "Compte associé introuvable ou désactivé.");

            token.DateUtilisation = DateTime.UtcNow;
            utilisateur.EmailVerified = true;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Email vérifié pour utilisateur {UserId} ({Email})",
                utilisateur.IdUtilisateur, utilisateur.Email);

            return (true, 200, "Adresse email vérifiée avec succès.");
        }

        public async Task<(bool Success, int StatusCode, string Message)> ResendAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            const string messageStandard =
                "Si un compte existe pour cette adresse, un email de vérification a été envoyé.";

            if (string.IsNullOrWhiteSpace(email))
                return (false, 400, "Email requis.");

            var normalized = email.Trim().ToLowerInvariant();
            if (IsSyntheticEmail(normalized))
                return (true, 200, messageStandard);

            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(
                    u => u.Email != null && u.Email.ToLower() == normalized && u.Statut == true,
                    cancellationToken);

            if (utilisateur == null)
            {
                _logger.LogWarning("Renvoi vérification pour email inconnu: {Email}", normalized);
                return (true, 200, messageStandard);
            }

            if (utilisateur.EmailVerified == true)
                return (true, 200, "Cette adresse email est déjà vérifiée.");

            var sent = await IssueAndSendAsync(utilisateur, cancellationToken);
            if (!sent)
                _logger.LogWarning("Renvoi vérification non envoyé pour {Email}", normalized);

            return (true, 200, messageStandard);
        }

        public static bool IsSyntheticEmail(string email) =>
            email.EndsWith(SyntheticEmailSuffix, StringComparison.OrdinalIgnoreCase);

        public static string HashToken(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string CreateRawToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private string BuildVerifyUrl(string rawToken)
        {
            var baseUrl = (_configuration["FrontendSettings:BaseUrl"] ?? "https://congotravel.kansaconsulting.com")
                .TrimEnd('/');
            var path = (_configuration["FrontendSettings:VerifyEmailPath"] ?? "/verify-email").Trim();
            if (!path.StartsWith('/'))
                path = "/" + path;

            return $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";
        }
    }
}
