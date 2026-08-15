using System.Security.Claims;
using System.Text;
using CongoTravel.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace CongoTravel.Services
{
    public interface ISimpleJwtService
    {
        string GenerateToken(Utilisateur utilisateur, int? idAgent = null);
        bool ValidateToken(string token);
    }

    public class SimpleJwtService : ISimpleJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public SimpleJwtService(IConfiguration configuration)
        {
            _configuration = configuration;
            _secretKey = _configuration["Jwt:SecretKey"] ?? "Kenergie_SecretKey_2024_SuperSecure_Key_For_Production_Use_Only";
            _issuer = _configuration["Jwt:Issuer"] ?? "Kenergie";
            _audience = _configuration["Jwt:Audience"] ?? "KenergieUsers";
            _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");
        }

        public string GenerateToken(Utilisateur utilisateur, int? idAgent = null)
        {
            try
            {
                // ✅ Utilisation de JwtSecurityTokenHandler pour générer un token standard
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // ✅ Utiliser les paramètres passés explicitement, sinon utiliser les valeurs de l'utilisateur
                var agentId = idAgent ?? utilisateur.IdAgent;

                // 🔍 Debug: Vérifier si IdAgent est présent
                Console.WriteLine($"🔍 [SIMPLEJWT] GenerateToken - Utilisateur {utilisateur.IdUtilisateur}: IdAgent (param) = {idAgent?.ToString() ?? "NULL"}, IdAgent (user) = {utilisateur.IdAgent?.ToString() ?? "NULL"}, IdAgent (final) = {agentId?.ToString() ?? "NULL"}");

                // Créer les claims (informations utilisateur dans le token)
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, utilisateur.IdUtilisateur.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, utilisateur.IdUtilisateur.ToString()), // Pour compatibilité avec User.FindFirst(ClaimTypes.NameIdentifier)
                    new Claim("IdUtilisateur", utilisateur.IdUtilisateur.ToString()), // Pour rétrocompatibilité
                    new Claim(JwtRegisteredClaimNames.Email, utilisateur.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Name, utilisateur.NomComplet ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Ajouter IdSociete uniquement s'il existe (ne pas créer de claim vide)
                if (utilisateur.IdSociete.HasValue && utilisateur.IdSociete.Value > 0)
                {
                    claims.Add(new Claim("idSociete", utilisateur.IdSociete.Value.ToString()));
                }

                // Ajouter IdClient quand l'utilisateur est lié à un client transport
                if (utilisateur.IdClient.HasValue && utilisateur.IdClient.Value > 0)
                {
                    claims.Add(new Claim("idClient", utilisateur.IdClient.Value.ToString()));
                }

                // Ajouter le nom de l'école uniquement s'il existe
                if (!string.IsNullOrWhiteSpace(utilisateur.Societe?.Nom))
                {
                    claims.Add(new Claim("societe", utilisateur.Societe.Nom));
                }

                // ✅ Ajouter IdAgent si disponible (utiliser la valeur finale)
                if (agentId.HasValue)
                {
                    claims.Add(new Claim("IdAgent", agentId.Value.ToString()));
                    Console.WriteLine($"✅ [SIMPLEJWT] IdAgent ajouté au token: {agentId.Value}");
                }
                else
                {
                    Console.WriteLine($"⚠️ [SIMPLEJWT] IdAgent est NULL pour utilisateur {utilisateur.IdUtilisateur}");
                }

                if (utilisateur.IdSite.HasValue && utilisateur.IdSite.Value > 0)
                {
                    claims.Add(new Claim("IdSite", utilisateur.IdSite.Value.ToString()));
                }


                // ✅ MULTI-RÔLES : Ajouter tous les rôles si disponibles via UserRoles
                if (utilisateur.UserRoles != null && utilisateur.UserRoles.Any(ur => ur.Statut == true))
                {
                    var activeRoles = utilisateur.UserRoles
                        .Where(ur => ur.Statut == true)
                        .Select(ur => ur.Role)
                        .Where(r => r != null)
                        .ToList();

                    // Ajouter chaque rôle comme ClaimTypes.Role (pour [Authorize(Roles = "...")])
                    foreach (var role in activeRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Nom));
                    }

                    // Rétrocompatibilité: idRole et primaryRole
                    var primaryRole = utilisateur.PrimaryRole ?? activeRoles.OrderBy(r => r.Niveau ?? 999).FirstOrDefault();
                    if (primaryRole != null)
                    {
                        claims.Add(new Claim("idRole", primaryRole.IdRole.ToString()));
                        claims.Add(new Claim("primaryRole", primaryRole.Nom));
                    }

                    // Ajouter une représentation JSON (utile pour le frontend)
                    var roleNames = activeRoles.Select(r => r.Nom).ToArray();
                    var roleIds = activeRoles.Select(r => r.IdRole.ToString()).ToArray();
                    claims.Add(new Claim("roles", System.Text.Json.JsonSerializer.Serialize(roleNames)));
                    claims.Add(new Claim("roleIds", System.Text.Json.JsonSerializer.Serialize(roleIds)));
                }
                else
                {
                    // Rétrocompatibilité: Si UserRoles n'est pas chargé, utiliser l'ancien système
                    claims.Add(new Claim("idRole", utilisateur.IdRole.ToString()));
                    if (utilisateur.Role != null)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, utilisateur.Role.Nom));
                        claims.Add(new Claim("primaryRole", utilisateur.Role.Nom));
                    }
                }

                // Créer le token JWT
                var token = new JwtSecurityToken(
                    issuer: _issuer,
                    audience: _audience,
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
                    signingCredentials: credentials
                );

                // Retourner le token encodé
                var tokenHandler = new JwtSecurityTokenHandler();
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors de la génération du JWT: {ex.Message}");
                throw;
            }
        }

        public bool ValidateToken(string token)
        {
            try
            {
                // ✅ Utilisation de JwtSecurityTokenHandler pour valider le token
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
