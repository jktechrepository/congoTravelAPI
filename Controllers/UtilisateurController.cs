using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Services.Repositories;
using CongoTravel.Services.Repositories;
using CongoTravel.Services;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using MySqlConnector;
using System;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Protection globale - tous les endpoints nécessitent un token JWT
    public class UtilisateurController : ControllerBase
    {
        private readonly IUtilisateurRepository _utilisateurRepository;
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly IAuditService _auditService;
        private readonly ISimpleJwtService _jwtService;
        private readonly IPermissionService _permissionService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IAppleAuthService _appleAuthService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UtilisateurController> _logger;
        private readonly CongoTravelDbContext _context;
        private readonly IEmailService _emailService;
        private readonly TimeSpan _passwordResetTokenValidity = TimeSpan.FromMinutes(5);

        public UtilisateurController(
            IUtilisateurRepository utilisateurRepository, 
            IUserDeviceRepository userDeviceRepository,
            ISimpleJwtService jwtService,
            IPermissionService permissionService,
            IRefreshTokenService refreshTokenService,
            IGoogleAuthService googleAuthService,
            IAppleAuthService appleAuthService,
            IConfiguration configuration,
            ILogger<UtilisateurController> logger,
            CongoTravelDbContext context,
            IAuditService auditService,
            IEmailService emailService)
        {
            _utilisateurRepository = utilisateurRepository;
            _auditService = auditService;
            _userDeviceRepository = userDeviceRepository;
            _jwtService = jwtService;
            _permissionService = permissionService;
            _refreshTokenService = refreshTokenService;
            _googleAuthService = googleAuthService;
            _appleAuthService = appleAuthService;
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        // GET: api/Utilisateur
        /// <summary>
        /// Récupérer tous les utilisateurs avec pagination et filtres (Admin uniquement)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin de lister les utilisateurs de son école avec pagination.
        /// Un Super-Admin peut voir les utilisateurs de toutes les écoles.
        /// 
        /// Restrictions :
        /// - Réservé aux Admins et Super-Admins
        /// - Un Admin ne voit que les utilisateurs de son école
        /// - Pagination obligatoire (max 100 par page)
        /// 
        /// Paramètres de requête :
        /// - page : Numéro de page (défaut = 1)
        /// - pageSize : Nombre par page (défaut = 50, max = 100)
        /// - statut : Filtrer par statut (actif/inactif)
        /// - idRole : Filtrer par rôle
        /// - searchTerm : Rechercher dans nom, prénom, email
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> GetUtilisateurs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? statut = null,
            [FromQuery] int? idRole = null,
            [FromQuery] string? searchTerm = null)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION DES PARAMÈTRES
            // ═══════════════════════════════════════════════════════════
            
            if (page < 1)
            {
                return BadRequest(new { message = "Le numéro de page doit être >= 1" });
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "La taille de page doit être entre 1 et 100" });
            }
            
            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'ADMIN CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 3. RÉCUPÉRER LES UTILISATEURS
            // ═══════════════════════════════════════════════════════════
            
            IEnumerable<Utilisateur> allUtilisateurs;
            
            // Si pas Super-Admin, filtrer automatiquement par société
            if (!isSuperAdmin)
            {
                allUtilisateurs = await _utilisateurRepository.GetBySocieteAsync(currentUser?.IdSociete ?? 0);
                _logger.LogInformation($"🔍 Admin {userId} liste les utilisateurs de sa société ({currentUser?.IdSociete})");
            }
            else
            {
                allUtilisateurs = await _utilisateurRepository.GetAllAsync();
                _logger.LogInformation($"🔍 Super-Admin {userId} liste TOUS les utilisateurs");
            }
            
            // ═══════════════════════════════════════════════════════════
            // 4. APPLIQUER LES FILTRES
            // ═══════════════════════════════════════════════════════════
            
            // Filtrer par statut
            if (statut.HasValue)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.Statut == statut.Value);
            }
            
            // Filtrer par rôle
            if (idRole.HasValue)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.IdRole == idRole.Value);
            }
            
            // Recherche par terme
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                allUtilisateurs = allUtilisateurs.Where(u =>
                    (u.NomComplet?.ToLower().Contains(search) ?? false) ||
                    (u.Email?.ToLower().Contains(search) ?? false) ||
                    (u.Telephone?.Contains(search) ?? false)
                );
            }
            
            var totalCount = allUtilisateurs.Count();
            
            // ═══════════════════════════════════════════════════════════
            // 5. PAGINATION
            // ═══════════════════════════════════════════════════════════
            
            var utilisateurs = allUtilisateurs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Note: Les utilisateurs retournés ne contiennent pas MotDePasseHash (filtré)
            
            return Ok(new {
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                totalCount,
                filtres = new {
                    statut,
                    idRole,
                    searchTerm
                },
                data = utilisateurs
            });
        }

        // GET: api/Utilisateur/5
        /// <summary>
        /// Récupérer un utilisateur par ID
        /// </summary>
        /// <remarks>
        /// Un utilisateur peut voir ses propres informations.
        /// Un Admin peut voir les informations des utilisateurs de son école.
        /// Un Super-Admin peut voir toutes les informations.
        /// 
        /// Le hash du mot de passe n'est JAMAIS retourné.
        /// </remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Utilisateur), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Utilisateur>> GetUtilisateur(int id)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. RÉCUPÉRER L'UTILISATEUR CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            
            // ═══════════════════════════════════════════════════════════
            // 2. CONTRÔLE D'ACCÈS
            // ═══════════════════════════════════════════════════════════
            
            bool isAdmin = currentUser?.Role?.Nom == "Admin" || currentUser?.Role?.Nom == "Super-Admin";
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            bool isOwnProfile = userId == id;
            
            // Vérifier que l'utilisateur accède à ses propres infos OU est admin
            if (!isOwnProfile && !isAdmin)
            {
                _logger.LogWarning($"❌ Tentative d'accès non autorisé : User {userId} → User {id}");
                return Forbid(); // 403 Forbidden
            }
            
            // ═══════════════════════════════════════════════════════════
            // 3. RÉCUPÉRER L'UTILISATEUR
            // ═══════════════════════════════════════════════════════════
            
            var utilisateur = await _utilisateurRepository.GetByIdAsync(id);
            
            if (utilisateur == null)
            {
                _logger.LogWarning($"❌ Utilisateur {id} non trouvé");
                return NotFound();
            }
            
            // Si admin (mais pas Super-Admin), vérifier même école
            if (isAdmin && !isSuperAdmin && !isOwnProfile)
            {
                if (utilisateur.IdSociete != currentUser?.IdSociete)
                {
                    _logger.LogWarning($"❌ Admin {userId} (École {currentUser?.IdSociete}) tente d'accéder à User {id} (École {utilisateur.IdSociete})");
                    return Forbid();
                }
            }
            
            // ═══════════════════════════════════════════════════════════
            // 4. RETOURNER L'UTILISATEUR (sans mot de passe)
            // ═══════════════════════════════════════════════════════════
            
            // 🔒 SÉCURITÉ : Ne JAMAIS retourner le hash du mot de passe
            utilisateur.MotDePasseHash = null;
            
            return Ok(utilisateur);
        }

        // GET: api/Utilisateur/email?email=user@example.com
        /// <summary>
        /// Récupérer un utilisateur par email (Admin uniquement)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin de rechercher un utilisateur par son email.
        /// 
        /// Restrictions :
        /// - Réservé aux Admins et Super-Admins
        /// - Un Admin ne peut rechercher que dans son école
        /// - Protection contre enumeration attack
        /// 
        /// Le hash du mot de passe n'est JAMAIS retourné.
        /// 
        /// ⚠️ Utilise un paramètre de requête pour éviter les problèmes d'encodage URL en production.
        /// </remarks>
        [HttpGet("email")]
        [ProducesResponseType(typeof(Utilisateur), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Utilisateur>> GetUtilisateurByEmail([FromQuery] string email)
        {
            // ═══════════════════════════════════════════════════════════
            // 0. VALIDATION DU PARAMÈTRE EMAIL
            // ═══════════════════════════════════════════════════════════
            
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Le paramètre 'email' est obligatoire" });
            }
            
            // ═══════════════════════════════════════════════════════════
            // 1. RÉCUPÉRER L'ADMIN CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 2. RECHERCHER L'UTILISATEUR
            // ═══════════════════════════════════════════════════════════
            
            var utilisateur = await _utilisateurRepository.GetByEmailAsync(email);
            
            if (utilisateur == null)
            {
                _logger.LogInformation($"📧 Utilisateur avec email {email} non trouvé (recherche par Admin {userId})");
                return NotFound();
            }
            
            // ═══════════════════════════════════════════════════════════
            // 3. CONTRÔLE D'ACCÈS
            // ═══════════════════════════════════════════════════════════
            
            // Si pas Super-Admin, vérifier même école
            if (!isSuperAdmin && utilisateur.IdSociete != currentUser?.IdSociete)
            {
                _logger.LogWarning($"❌ Admin {userId} (École {currentUser?.IdSociete}) tente d'accéder à User {utilisateur.IdUtilisateur} (École {utilisateur.IdSociete})");
                return Forbid();
            }
            
            // ═══════════════════════════════════════════════════════════
            // 4. RETOURNER L'UTILISATEUR (sans mot de passe)
            // ═══════════════════════════════════════════════════════════
            
            // 🔒 SÉCURITÉ : Ne JAMAIS retourner le hash du mot de passe
            utilisateur.MotDePasseHash = null;
            
            _logger.LogInformation($"✅ Utilisateur {utilisateur.IdUtilisateur} récupéré par email par Admin {userId}");
            
            return Ok(utilisateur);
        }


        // GET: api/Utilisateur/role/1
        /// <summary>
        /// Récupérer les utilisateurs par rôle avec pagination (Admin uniquement)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin de lister les utilisateurs d'un rôle spécifique.
        /// 
        /// Restrictions :
        /// - Réservé aux Admins et Super-Admins
        /// - Un Admin ne voit que les utilisateurs de son école
        /// - Pagination obligatoire
        /// </remarks>
        [HttpGet("role/{roleId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> GetUtilisateursByRole(
            int roleId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION
            // ═══════════════════════════════════════════════════════════
            
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Paramètres de pagination invalides" });
            }
            
            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'ADMIN CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 3. RÉCUPÉRER ET FILTRER
            // ═══════════════════════════════════════════════════════════
            
            var allUtilisateurs = await _utilisateurRepository.GetByRoleAsync(roleId);
            
            // Si pas Super-Admin, filtrer par école
            if (!isSuperAdmin)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.IdSociete == currentUser?.IdSociete);
            }
            
            var totalCount = allUtilisateurs.Count();
            
            // Pagination
            var utilisateurs = allUtilisateurs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Note: V_Utilisateur ne contient pas MotDePasseHash (déjà filtré dans la vue SQL)
            
            return Ok(new {
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                totalCount,
                roleId,
                data = utilisateurs
            });
        }

        // GET: api/Utilisateur/statut/true
        /// <summary>
        /// Récupérer les utilisateurs par statut avec pagination (Admin uniquement)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin de lister les utilisateurs actifs ou inactifs.
        /// 
        /// Restrictions :
        /// - Réservé aux Admins et Super-Admins
        /// - Un Admin ne voit que les utilisateurs de son école
        /// - Pagination obligatoire
        /// </remarks>
        [HttpGet("statut/{statut}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> GetUtilisateursByStatut(
            bool statut,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION
            // ═══════════════════════════════════════════════════════════
            
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Paramètres de pagination invalides" });
            }
            
            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'ADMIN CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 3. RÉCUPÉRER ET FILTRER
            // ═══════════════════════════════════════════════════════════
            
            var allUtilisateurs = await _utilisateurRepository.GetByStatutAsync(statut);
            
            // Si pas Super-Admin, filtrer par école
            if (!isSuperAdmin)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.IdSociete == currentUser?.IdSociete);
            }
            
            var totalCount = allUtilisateurs.Count();
            
            // Pagination
            var utilisateurs = allUtilisateurs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Note: V_Utilisateur ne contient pas MotDePasseHash (déjà filtré dans la vue SQL)
            
            return Ok(new {
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                totalCount,
                statut,
                data = utilisateurs
            });
        }

        // GET: api/Utilisateur/exists/5
        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> UtilisateurExists(int id)
        {
            var exists = await _utilisateurRepository.ExistsAsync(id);
            return Ok(exists);
        }

        // GET: api/Utilisateur/exists/email/user@example.com
        [HttpGet("exists/email/{email}")]
        public async Task<ActionResult<bool>> UtilisateurExistsByEmail(string email)
        {
            var exists = await _utilisateurRepository.ExistsByEmailAsync(email);
            return Ok(exists);
        }


        // GET: api/Utilisateur/societe/1
        /// <summary>
        /// Récupérer les utilisateurs d'une école (Admin uniquement)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin de lister les utilisateurs de son école avec pagination.
        /// 
        /// Restrictions :
        /// - Réservé aux Admins et Super-Admins
        /// - Un Admin ne peut voir que les utilisateurs de son école
        /// - Pagination obligatoire (max 100 par page)
        /// 
        /// Paramètres de requête :
        /// - page : Numéro de page (défaut = 1)
        /// - pageSize : Nombre par page (défaut = 50, max = 100)
        /// - statut : Filtrer par statut (actif/inactif)
        /// - idRole : Filtrer par rôle
        /// </remarks>
        [HttpGet("societe/{idSociete}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<object>> GetUtilisateursBySociete(
            int idSociete,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] bool? statut = null,
            [FromQuery] int? idRole = null)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION DES PARAMÈTRES
            // ═══════════════════════════════════════════════════════════
            
            if (page < 1)
            {
                return BadRequest(new { message = "Le numéro de page doit être >= 1" });
            }
            
            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "La taille de page doit être entre 1 et 100" });
            }
            
            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'ADMIN CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 3. CONTRÔLE D'ACCÈS
            // ═══════════════════════════════════════════════════════════
            
            // Vérifier que l'utilisateur accède à SA propre école (sauf Super-Admin)
            if (!isSuperAdmin && currentUser?.IdSociete != idSociete)
            {
                _logger.LogWarning($"❌ Tentative d'accès inter-écoles : Admin {userId} (École {currentUser?.IdSociete}) → École {idSociete}");
                return Forbid(); // 403 Forbidden
            }
            
            // ═══════════════════════════════════════════════════════════
            // 4. RÉCUPÉRER LES UTILISATEURS AVEC PAGINATION
            // ═══════════════════════════════════════════════════════════
            
            var allUtilisateurs = await _utilisateurRepository.GetBySocieteAsync(idSociete);
            
            // Filtrer par statut si spécifié
            if (statut.HasValue)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.Statut == statut.Value);
            }
            
            // Filtrer par rôle si spécifié
            if (idRole.HasValue)
            {
                allUtilisateurs = allUtilisateurs.Where(u => u.IdRole == idRole.Value);
            }
            
            var totalCount = allUtilisateurs.Count();
            
            // Pagination
            var utilisateurs = allUtilisateurs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            // Note: V_Utilisateur ne contient pas MotDePasseHash (déjà filtré dans la vue SQL)
            
            _logger.LogInformation($"✅ {utilisateurs.Count} utilisateurs récupérés pour École {idSociete} (Page {page}/{(int)Math.Ceiling(totalCount / (double)pageSize)}) par Admin {userId}");
            
            return Ok(new {
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                totalCount,
                data = utilisateurs
            });
        }

        // POST: api/Utilisateur
        /// <summary>
        /// Créer un nouvel utilisateur (Admin, Gerant et Super-Admin)
        /// </summary>
        /// <remarks>
        /// Permet à un Admin ou Gerant de créer un nouvel utilisateur dans son école.
        /// 
        /// Restrictions :
        /// - Un Admin/Gerant ne peut créer que dans son école (sauf Super-Admin)
        /// - Un Admin ne peut pas créer un Super-Admin
        /// - Un Gerant ne peut pas créer un Admin ni un Super-Admin
        /// - Email doit être unique
        /// - Mot de passe doit respecter la complexité
        /// 
        /// Champs auto-générés :
        /// - ReferenceUtilisateur (Guid)
        /// - DefaultUsername (prenom.nom)
        /// - DateCreation (DateTime.UtcNow)
        /// </remarks>
        [HttpPost]
        [Permission("Utilisateur.Create")]
        [ProducesResponseType(typeof(Utilisateur), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<Utilisateur>> CreateUtilisateur([FromBody] CreateUtilisateurDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ═══════════════════════════════════════════════════════════
            // 1. RÉCUPÉRER L'UTILISATEUR CONNECTÉ (Admin/Gerant/Super-Admin)
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            // ✅ MULTI-RÔLES : Charger les UserRoles pour vérifier tous les rôles
            var currentUser = await _context.Utilisateurs
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Role) // Rétrocompatibilité
                .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);
            
            // Vérifier les rôles de l'utilisateur connecté
            var userRoles = currentUser?.UserRoles?
                .Where(ur => ur.Statut == true)
                .Select(ur => ur.Role?.Nom)
                .Where(nom => !string.IsNullOrEmpty(nom))
                .ToList() ?? new List<string?>();
            
            // Rétrocompatibilité : ajouter le rôle principal si disponible
            if (currentUser?.PrimaryRole != null && !userRoles.Contains(currentUser.PrimaryRole.Nom))
            {
                userRoles.Add(currentUser.PrimaryRole.Nom);
            }
            
            bool isSuperAdmin = userRoles.Contains("Super-Admin");
            bool isAdmin = userRoles.Contains("Admin");
            bool isGerant = userRoles.Contains("Gerant");
            
            // ═══════════════════════════════════════════════════════════
            // 2. CONTRÔLES DE SÉCURITÉ
            // ═══════════════════════════════════════════════════════════
            
            // Vérifier que l'admin/directeur crée dans SA propre école (sauf Super-Admin)
            if (!isSuperAdmin && dto.IdSociete != currentUser?.IdSociete)
            {
                _logger.LogWarning($"❌ Utilisateur {userId} (École {currentUser?.IdSociete}) tente de créer un utilisateur dans École {dto.IdSociete}");
                return Forbid();
            }
            
            // Récupérer le rôle cible
            var targetRole = await _context.Roles.FindAsync(dto.IdRole);
            if (targetRole == null)
            {
                return BadRequest(new { message = "Rôle spécifié introuvable" });
            }
            
            // Empêcher un Admin de créer un Super-Admin
            if (!isSuperAdmin && targetRole.Nom == "Super-Admin")
            {
                _logger.LogWarning($"❌ Utilisateur {userId} (Admin) tente de créer un Super-Admin");
                return BadRequest(new { message = "Vous ne pouvez pas créer un Super-Admin" });
            }
            
            // ✅ NOUVEAU : Empêcher un Gerant de créer un Admin ou Super-Admin
            if (isGerant && !isAdmin && !isSuperAdmin)
            {
                if (targetRole.Nom == "Admin" || targetRole.Nom == "Super-Admin")
                {
                    _logger.LogWarning($"❌ Gerant {userId} tente de créer un utilisateur avec le rôle {targetRole.Nom}");
                    return BadRequest(new { message = $"Un Gerant ne peut pas créer un utilisateur avec le rôle {targetRole.Nom}" });
                }
            }
            
            // ═══════════════════════════════════════════════════════════
            // 3. VÉRIFIER UNICITÉ EMAIL
            // ═══════════════════════════════════════════════════════════
            
            var emailExists = await _utilisateurRepository.ExistsByEmailAsync(dto.Email);
            if (emailExists)
            {
                _logger.LogWarning($"❌ Email déjà utilisé : {dto.Email}");
                return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur" });
            }

            if (dto.IdSite.HasValue)
            {
                try
                {
                    await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, dto.IdSite, dto.IdSociete);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            
            // ═══════════════════════════════════════════════════════════
            // 4. CRÉER L'ENTITÉ UTILISATEUR
            // ═══════════════════════════════════════════════════════════
            
            var utilisateur = new Utilisateur
            {
                // Informations personnelles
                NomComplet = dto.NomComplet,
                Email = dto.Email,
                Telephone = dto.Telephone,
                PhotoUrl = dto.PhotoUrl,
                LieuNaissance = dto.LieuNaissance,
                DateNaissance = dto.DateNaissance,
                Genre = dto.Genre,
                
                // Informations administratives
                IdRole = dto.IdRole,
                IdSociete = dto.IdSociete,
                IdSite = dto.IdSite,
                Statut = dto.Statut,
                
                // Champs auto-générés
                ReferenceUtilisateur = Guid.NewGuid(),
                DefaultUsername = dto.NomComplet?.ToLower().Replace(" ", ".") ?? "user",
                DateCreation = DateTime.UtcNow,
                IsConnecte = false,
                
                // Hash du mot de passe
                MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.MotDePasse)
            };
            
            _logger.LogInformation($"🆕 Création d'un nouvel utilisateur : {dto.Email} (Role: {dto.IdRole}, École: {dto.IdSociete}) par Admin {userId}");

            // ═══════════════════════════════════════════════════════════
            // 5. SAUVEGARDER EN BASE DE DONNÉES
            // ═══════════════════════════════════════════════════════════
            
            Utilisateur createdUtilisateur;
            try
            {
                createdUtilisateur = await _utilisateurRepository.CreateAsync(utilisateur);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "❌ Échec création utilisateur : email déjà utilisé ({Email})", dto.Email);
                return Conflict(new { message = "Cet email est déjà utilisé. Merci d'en choisir un autre." });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailException(ex))
            {
                _logger.LogWarning(ex, "❌ Échec création utilisateur (conflit unique) : {Email}", dto.Email);
                return Conflict(new { message = "Cet email est déjà utilisé. Merci d'en choisir un autre." });
            }
            
            // Récupérer avec relations (Eager Loading)
            var utilisateurAvecRelations = await _utilisateurRepository.GetByIdAsync(createdUtilisateur.IdUtilisateur);
            
            // Ne JAMAIS retourner MotDePasseHash
            if (utilisateurAvecRelations != null)
            {
                utilisateurAvecRelations.MotDePasseHash = null;
            }
            
            _logger.LogInformation($"✅ Utilisateur {createdUtilisateur.IdUtilisateur} créé avec succès par Admin {userId}");
            
            return CreatedAtAction(nameof(GetUtilisateur), 
                new { id = createdUtilisateur.IdUtilisateur }, 
                utilisateurAvecRelations);
        }

        // PUT: api/Utilisateur/5
        /// <summary>
        /// Modifier les informations personnelles d'un utilisateur
        /// </summary>
        /// <remarks>
        /// Un utilisateur peut modifier ses propres informations personnelles.
        /// Un admin peut modifier les informations des utilisateurs de son école.
        /// 
        /// Champs modifiables :
        /// - Nom, Prénom, Post-nom
        /// - Email (vérifié unique)
        /// - Téléphone
        /// - Photo
        /// - Date de naissance, Lieu de naissance, Genre
        /// 
        /// Champs protégés (non modifiables via cet endpoint) :
        /// - Mot de passe (utiliser POST /api/Utilisateur/changer_mot_de_passe)
        /// - Rôle, École, Statut (utiliser PUT /api/Utilisateur/{id}/admin)
        /// </remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Utilisateur), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Utilisateur>> UpdateUtilisateur(
            int id, 
            [FromBody] UpdateUtilisateurDto dto)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION DE BASE
            // ═══════════════════════════════════════════════════════════
            
            if (id != dto.IdUtilisateur)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'UTILISATEUR CONNECTÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide ou manquant" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            
            if (currentUser == null)
            {
                _logger.LogWarning($"❌ Utilisateur connecté {userId} non trouvé en base de données");
                return Unauthorized(new { message = "Utilisateur non authentifié" });
            }

            // ═══════════════════════════════════════════════════════════
            // 3. RÉCUPÉRER L'UTILISATEUR CIBLE
            // ═══════════════════════════════════════════════════════════
            
            var targetUser = await _utilisateurRepository.GetByIdAsync(id);
            
            if (targetUser == null)
            {
                _logger.LogWarning($"❌ Utilisateur cible {id} non trouvé");
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            // ═══════════════════════════════════════════════════════════
            // 4. CONTRÔLE D'ACCÈS
            // ═══════════════════════════════════════════════════════════
            
            bool isAdmin = currentUser.Role?.Nom == "Admin" || currentUser.Role?.Nom == "Super-Admin";
            bool isSuperAdmin = currentUser.Role?.Nom == "Super-Admin";
            bool isOwnProfile = userId == id;
            
            // Règle : L'utilisateur modifie ses propres infos OU est admin de la même école
            if (!isOwnProfile && !isAdmin)
            {
                _logger.LogWarning($"❌ Tentative de modification non autorisée : User {userId} ({currentUser.Email}) → User {id} ({targetUser.Email})");
                return Forbid(); // 403 Forbidden
            }
            
            // Si admin (mais pas Super-Admin), vérifier la même école
            if (isAdmin && !isSuperAdmin && !isOwnProfile)
            {
                if (targetUser.IdSociete != currentUser.IdSociete)
                {
                    _logger.LogWarning($"❌ Tentative de modification inter-écoles : Admin {userId} (École {currentUser.IdSociete}) → User {id} (École {targetUser.IdSociete})");
                    return Forbid();
                }
            }

            _logger.LogInformation($"✅ Autorisation accordée : User {userId} modifie User {id} (isOwnProfile: {isOwnProfile}, isAdmin: {isAdmin})");

            // ═══════════════════════════════════════════════════════════
            // 5. VÉRIFIER UNICITÉ EMAIL (si changé)
            // ═══════════════════════════════════════════════════════════
            
            if (dto.Email != targetUser.Email)
            {
                var emailExists = await _utilisateurRepository.ExistsByEmailAsync(dto.Email!);
                if (emailExists)
                {
                    _logger.LogWarning($"❌ Email déjà utilisé : {dto.Email}");
                    return BadRequest(new { message = "Cet email est déjà utilisé par un autre utilisateur" });
                }
                
                _logger.LogInformation($"📧 Changement d'email : {targetUser.Email} → {dto.Email}");
            }

            // ═══════════════════════════════════════════════════════════
            // 6. CAPTURER L'ÉTAT AVANT MODIFICATION (AUDIT)
            // ═══════════════════════════════════════════════════════════
            
            var oldUtilisateur = new Utilisateur
            {
                IdUtilisateur = targetUser.IdUtilisateur,
                NomComplet = targetUser.NomComplet,
                Email = targetUser.Email,
                Telephone = targetUser.Telephone,
                PhotoUrl = targetUser.PhotoUrl,
                LieuNaissance = targetUser.LieuNaissance,
                DateNaissance = targetUser.DateNaissance,
                Genre = targetUser.Genre
            };

            // ═══════════════════════════════════════════════════════════
            // 7. METTRE À JOUR SEULEMENT LES CHAMPS AUTORISÉS
            // ═══════════════════════════════════════════════════════════
            
            // Informations personnelles (modifiables par tous)
            targetUser.NomComplet = dto.NomComplet;
            targetUser.Email = dto.Email;
            targetUser.Telephone = dto.Telephone;
            targetUser.PhotoUrl = dto.PhotoUrl;
            targetUser.LieuNaissance = dto.LieuNaissance;
            targetUser.DateNaissance = dto.DateNaissance;
            targetUser.Genre = dto.Genre;
            
            // Champs protégés (JAMAIS modifiés via cet endpoint)
            // ❌ targetUser.MotDePasseHash      → Utiliser POST /api/Utilisateur/changer_mot_de_passe
            // ❌ targetUser.IdRole              → Utiliser PUT /api/Utilisateur/{id}/admin
            // ❌ targetUser.IdSociete             → Utiliser PUT /api/Utilisateur/{id}/admin (Super-Admin)
            // ❌ targetUser.Statut              → Utiliser PUT /api/Utilisateur/toggle-statut/{id}
            // ❌ targetUser.ReferenceUtilisateur → Immuable
            // ❌ targetUser.DateCreation        → Immuable
            // ❌ targetUser.IsConnecte          → Géré automatiquement
            
            _logger.LogInformation($"🔄 Mise à jour des infos de l'utilisateur {id} par {userId}");

            // ═══════════════════════════════════════════════════════════
            // 8. SAUVEGARDER ET RETOURNER
            // ═══════════════════════════════════════════════════════════
            
            var updatedUtilisateur = await _utilisateurRepository.UpdateAsync(targetUser);
            
            if (updatedUtilisateur == null)
            {
                _logger.LogError($"❌ Erreur lors de la sauvegarde de l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // ═══════════════════════════════════════════════════════════
            // 9. ENREGISTRER L'AUDIT
            // ═══════════════════════════════════════════════════════════
            
            var auditContext = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                oldUtilisateur,
                updatedUtilisateur,
                auditContext.UserId,
                auditContext.UserName,
                auditContext.UserRole,
                auditContext.IdSociete,
                auditContext.IpAddress,
                auditContext.UserAgent,
                "Modification du profil utilisateur"
            );

            // Récupérer avec relations (Eager Loading)
            var utilisateurAvecRelations = await _utilisateurRepository.GetByIdAsync(updatedUtilisateur.IdUtilisateur);
            
            // 🔒 SÉCURITÉ : Ne JAMAIS retourner le hash du mot de passe
            if (utilisateurAvecRelations != null)
            {
                utilisateurAvecRelations.MotDePasseHash = null;
            }
            
            _logger.LogInformation($"✅ Utilisateur {id} mis à jour avec succès");
            
            return Ok(utilisateurAvecRelations);
        }
        
        // PUT: api/Utilisateur/5/admin
        /// <summary>
        /// Modifier les informations d'un utilisateur (Admin uniquement)
        /// Permet de modifier des champs supplémentaires (Rôle, Statut)
        /// </summary>
        /// <remarks>
        /// Réservé aux Admins et Super-Admins.
        /// 
        /// Champs supplémentaires modifiables :
        /// - Rôle (IdRole)
        /// - Statut (actif/inactif)
        /// 
        /// Restrictions :
        /// - Un Admin ne peut pas créer un Super-Admin
        /// - Un Admin ne peut modifier que les utilisateurs de son école
        /// - Un Admin ne peut pas modifier un Super-Admin
        /// </remarks>
        [HttpPut("{id}/admin")]
        [Permission("Utilisateur.Update")]
        [ProducesResponseType(typeof(Utilisateur), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Utilisateur>> UpdateUtilisateurAdmin(
            int id, 
            [FromBody] UpdateUtilisateurAdminDto dto)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VALIDATION DE BASE
            // ═══════════════════════════════════════════════════════════
            
            if (id != dto.IdUtilisateur)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // ═══════════════════════════════════════════════════════════
            // 2. RÉCUPÉRER L'ADMIN CONNECTÉ ET L'UTILISATEUR CIBLE
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
            var targetUser = await _utilisateurRepository.GetByIdAsync(id);
            
            if (targetUser == null)
            {
                _logger.LogWarning($"❌ Utilisateur cible {id} non trouvé");
                return NotFound(new { message = "Utilisateur non trouvé" });
            }
            
            bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";
            
            // ═══════════════════════════════════════════════════════════
            // 3. CONTRÔLES D'ACCÈS ADMIN
            // ═══════════════════════════════════════════════════════════
            
            // Vérifier la même école (sauf Super-Admin)
            if (!isSuperAdmin && targetUser.IdSociete != currentUser?.IdSociete)
            {
                _logger.LogWarning($"❌ Admin {userId} (École {currentUser?.IdSociete}) tente de modifier User {id} (École {targetUser.IdSociete})");
                return Forbid();
            }
            
            // Empêcher un Admin de modifier un Super-Admin
            if (!isSuperAdmin && targetUser.Role?.Nom == "Super-Admin")
            {
                _logger.LogWarning($"❌ Admin {userId} tente de modifier un Super-Admin ({id})");
                return Forbid();
            }

            // ═══════════════════════════════════════════════════════════
            // 4. VÉRIFIER UNICITÉ EMAIL (si changé)
            // ═══════════════════════════════════════════════════════════
            
            if (dto.Email != targetUser.Email)
            {
                var emailExists = await _utilisateurRepository.ExistsByEmailAsync(dto.Email!);
                if (emailExists)
                {
                    return BadRequest(new { message = "Cet email est déjà utilisé" });
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 5. METTRE À JOUR LES CHAMPS PERSONNELS
            // ═══════════════════════════════════════════════════════════
            
            targetUser.NomComplet = dto.NomComplet;
            targetUser.Email = dto.Email;
            targetUser.Telephone = dto.Telephone;
            targetUser.PhotoUrl = dto.PhotoUrl;
            targetUser.LieuNaissance = dto.LieuNaissance;
            targetUser.DateNaissance = dto.DateNaissance;
            targetUser.Genre = dto.Genre;
            
            // ═══════════════════════════════════════════════════════════
            // 6. METTRE À JOUR LES CHAMPS ADMINISTRATIFS (si fournis)
            // ═══════════════════════════════════════════════════════════
            
            if (dto.IdRole.HasValue)
            {
                // Empêcher un Admin (non Super-Admin) de créer un Super-Admin
                var targetRole = await _context.Roles.FindAsync(dto.IdRole.Value);
                
                if (!isSuperAdmin && targetRole?.Nom == "Super-Admin")
                {
                    _logger.LogWarning($"❌ Admin {userId} tente d'assigner le rôle Super-Admin à User {id}");
                    return BadRequest(new { message = "Vous ne pouvez pas assigner le rôle Super-Admin" });
                }
                
                _logger.LogInformation($"🔄 Changement de rôle pour User {id} : {targetUser.IdRole} → {dto.IdRole}");
                targetUser.IdRole = dto.IdRole.Value;
            }
            
            if (dto.Statut.HasValue)
            {
                _logger.LogInformation($"🔄 Changement de statut pour User {id} : {targetUser.Statut} → {dto.Statut}");
                targetUser.Statut = dto.Statut.Value;
            }

            if (dto.DesassocierSite)
            {
                targetUser.IdSite = null;
            }
            else if (dto.IdSite.HasValue)
            {
                var idSoc = targetUser.IdSociete;
                if (!idSoc.HasValue || idSoc.Value <= 0)
                    return BadRequest(new { message = "Impossible d'assigner un site : utilisateur sans société." });
                try
                {
                    await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(_context, dto.IdSite, idSoc.Value);
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                targetUser.IdSite = dto.IdSite;
            }
            
            _logger.LogInformation($"🔄 Modification admin des infos de l'utilisateur {id} par Admin {userId}");

            // ═══════════════════════════════════════════════════════════
            // 7. SAUVEGARDER ET RETOURNER
            // ═══════════════════════════════════════════════════════════
            
            var updatedUtilisateur = await _utilisateurRepository.UpdateAsync(targetUser);
            
            if (updatedUtilisateur == null)
            {
                _logger.LogError($"❌ Erreur lors de la sauvegarde de l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            var utilisateurAvecRelations = await _utilisateurRepository.GetByIdAsync(updatedUtilisateur.IdUtilisateur);
            
            // 🔒 SÉCURITÉ : Ne JAMAIS retourner le hash du mot de passe
            if (utilisateurAvecRelations != null)
            {
                utilisateurAvecRelations.MotDePasseHash = null;
            }
            
            _logger.LogInformation($"✅ Utilisateur {id} mis à jour avec succès par Admin {userId}");
            
            return Ok(utilisateurAvecRelations);
        }

        // DELETE: api/Utilisateur/5
        /// <summary>
        /// Désactiver un utilisateur (Super-Admin uniquement) - Soft Delete
        /// </summary>
        /// <remarks>
        /// Désactivation d'un utilisateur (soft delete - opération réversible).
        /// Réservé aux Super-Admins uniquement.
        /// 
        /// Restrictions :
        /// - Un utilisateur ne peut pas se désactiver lui-même
        /// - Toutes les désactivations sont tracées dans les logs
        /// 
        /// Note : L'utilisateur est désactivé (Statut = false) mais les données sont conservées.
        /// </remarks>
        [HttpDelete("{id}")]
        [Permission("Utilisateur.Delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> DeleteUtilisateur(int id)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. VÉRIFICATIONS DE SÉCURITÉ
            // ═══════════════════════════════════════════════════════════
            
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Token JWT invalide" });
            }
            
            // EMPÊCHER DE SE SUPPRIMER SOI-MÊME
            if (userId == id)
            {
                _logger.LogWarning($"❌ Super-Admin {userId} tente de se supprimer lui-même");
                return BadRequest(new { message = "Vous ne pouvez pas vous supprimer vous-même" });
            }
            
            var targetUser = await _utilisateurRepository.GetByIdAsync(id);
            if (targetUser == null)
            {
                _logger.LogWarning($"❌ Utilisateur {id} non trouvé pour suppression");
                return NotFound();
            }
            
            // ═══════════════════════════════════════════════════════════
            // 2. DÉSACTIVER L'UTILISATEUR (SOFT DELETE)
            // ═══════════════════════════════════════════════════════════
            
            _logger.LogWarning($"⚠️ DÉSACTIVATION (SOFT DELETE) : User {id} ({targetUser.Email}) par Super-Admin {userId}");
            
            var success = await _utilisateurRepository.DeleteAsync(id);
            if (!success)
            {
                _logger.LogError($"❌ Erreur lors de la désactivation de User {id}");
                return NotFound();
            }

            _logger.LogInformation($"✅ Utilisateur {id} désactivé (soft delete) par Super-Admin {userId}");
            return Ok(new 
            { 
                message = "Utilisateur désactivé avec succès (soft delete)",
                idUtilisateur = id,
                note = "L'utilisateur a été désactivé. Les données sont conservées pour l'historique."
            });
        }

        // POST: api/Utilisateur/authentifier
        // 🔓 Endpoint PUBLIC (pas de token requis pour se connecter)
        [AllowAnonymous]
        [HttpPost("authentifier")]
        public async Task<ActionResult<AuthentificationResponse>> Authentifier(AuthentificationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Vérifier si l'input est un email, un defaultUsername ou un téléphone
                Utilisateur? utilisateur = null;
                string searchMethod = "UNKNOWN";

                // Essayer d'abord de rechercher par email
                if (request.EmailOuTelephone?.Contains("@") == true)
                {
                    _logger.LogInformation($"🔍 Recherche par EMAIL: {request.EmailOuTelephone}");
                    utilisateur = await _utilisateurRepository.GetByEmailAsync(request.EmailOuTelephone);
                    if (utilisateur != null)
                    {
                        searchMethod = "EMAIL";
                    }
                }
                
                // Si pas trouvé, essayer par DefaultUsername
                if (utilisateur == null)
                {
                    _logger.LogInformation($"🔍 Recherche par DEFAULTUSERNAME: {request.EmailOuTelephone}");
                    utilisateur = await _utilisateurRepository.GetByDefaultUsernameAsync(request.EmailOuTelephone);
                    if (utilisateur != null)
                    {
                        searchMethod = "DEFAULTUSERNAME";
                    }
                }
                
                // Si toujours pas trouvé, essayer par téléphone
                if (utilisateur == null)
                {
                    _logger.LogInformation($"🔍 Recherche par TELEPHONE: {request.EmailOuTelephone}");
                    var utilisateurs = await _utilisateurRepository.GetByTelephoneAsync(request.EmailOuTelephone);
                    utilisateur = utilisateurs?.FirstOrDefault();
                    if (utilisateur != null)
                    {
                        searchMethod = "TELEPHONE";
                    }
                }

                if (utilisateur == null)
                {
                    _logger.LogWarning($"❌ Aucun utilisateur trouvé avec {searchMethod}: {request.EmailOuTelephone}");
                    return Unauthorized(new { message = "Email/Username/Telephone ou mot de passe incorrect" });
                }

                _logger.LogInformation($"✅ Utilisateur trouvé via {searchMethod}: {utilisateur.IdUtilisateur} - {utilisateur.Email}");
                _logger.LogInformation($"🔍 Utilisateur initial - IdAgent: {utilisateur.IdAgent?.ToString() ?? "NULL"}");

                // Vérifier le mot de passe
                if (string.IsNullOrEmpty(utilisateur.MotDePasseHash))
                {
                    _logger.LogWarning($"❌ Compte non configuré pour l'utilisateur {utilisateur.IdUtilisateur} - {utilisateur.Email}");
                    return Unauthorized(new { message = "Compte non configuré correctement" });
                }

                bool motDePasseValide = BCrypt.Net.BCrypt.Verify(request.MotDePasse, utilisateur.MotDePasseHash);

                if (!motDePasseValide)
                {
                    _logger.LogWarning($"❌ Mot de passe incorrect pour l'utilisateur {utilisateur.IdUtilisateur} - {utilisateur.Email}");
                    return Unauthorized(new { message = "Email/Telephone ou mot de passe incorrect" });
                }

                // Vérifier si l'utilisateur est actif
                if (utilisateur.Statut != true)
                {
                    _logger.LogWarning($"❌ Compte désactivé pour l'utilisateur {utilisateur.IdUtilisateur} - {utilisateur.Email}");
                    return Unauthorized(new { message = "Compte désactivé" });
                }

                // ✨ Vérifier si l'école de l'utilisateur est active
                if (utilisateur.IdSociete.HasValue)
                {
                    var societe = utilisateur.Societe;
                    if (societe == null || societe.Statut != true)
                    {
                        _logger.LogWarning($"❌ École désactivée pour l'utilisateur {utilisateur.IdUtilisateur} - {utilisateur.Email} (École ID: {utilisateur.IdSociete})");
                        return Unauthorized(new { message = "Accès refusé : Votre école a été désactivée. Veuillez contacter l'administrateur." });
                    }
                }

                _logger.LogInformation($"✅ Authentification réussie pour l'utilisateur {utilisateur.IdUtilisateur} - {utilisateur.Email}");

                // Marquer l'utilisateur comme connecté
                await _utilisateurRepository.MarquerCommeConnecteAsync(utilisateur.IdUtilisateur);
                _logger.LogInformation($"✅ Utilisateur {utilisateur.IdUtilisateur} marqué comme connecté");

                // ✨ Enregistrer le token FCM et les informations du device (pour les notifications push)
                if (!string.IsNullOrEmpty(request.FcmToken) && 
                    request.FcmToken != "string" && 
                    request.FcmToken != "null" &&
                    !string.IsNullOrEmpty(request.DeviceType) && 
                    request.DeviceType != "string" && 
                    request.DeviceType != "null")
                {
                    try
                    {
                        await _userDeviceRepository.CreateOrUpdateAsync(
                            utilisateur.IdUtilisateur,
                            request.FcmToken,
                            request.DeviceType,
                            request.DeviceModel,
                            request.OsVersion
                        );
                        
                        _logger.LogInformation($"✅ Token FCM enregistré pour l'utilisateur {utilisateur.IdUtilisateur} - Device: {request.DeviceType} {request.DeviceModel}");
                    }
                    catch (ArgumentException argEx)
                    {
                        // Erreur de validation des données (valeurs "string" par défaut)
                        _logger.LogWarning($"⚠️ Données device invalides pour l'utilisateur {utilisateur.IdUtilisateur}: {argEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        // Autres erreurs lors de l'enregistrement du token
                        _logger.LogError(ex, $"❌ Erreur lors de l'enregistrement du token FCM pour l'utilisateur {utilisateur.IdUtilisateur}");
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ Token FCM ou DeviceType manquant/invalide pour l'utilisateur {utilisateur.IdUtilisateur} - Token: '{request.FcmToken}', Device: '{request.DeviceType}'");
                }

                // Récupérer les informations complètes avec UserRoles pour multi-rôles
                var _utilisateur = await _context.Utilisateurs
                    .Include(u => u.Role)  // Rétrocompatibilité
                    .Include(u => u.Societe)
                    .Include(u => u.UserRoles)  // ✅ MULTI-RÔLES : Charger les UserRoles
                        .ThenInclude(ur => ur.Role)  // Charger les rôles associés
                    .Include(u => u.Agent)  // ✨ NOUVEAU : Charger l'agent associé
                    .Include(u => u.Client)  // ✨ NOUVEAU : Charger le client associé
          
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == utilisateur.IdUtilisateur);

                if (_utilisateur == null)
                {
                    _logger.LogError($"❌ Impossible de récupérer les informations complètes pour l'utilisateur {utilisateur.IdUtilisateur}");
                    return NotFound(new { message = "Informations utilisateur non trouvées" });
                }

                // ✅ IMPORTANT: S'assurer que IdAgent est préservé depuis l'utilisateur initial
                // (car il peut être perdu lors du chargement avec Include)
                _logger.LogInformation($"🔍 AVANT restauration - utilisateur initial: IdAgent={utilisateur.IdAgent?.ToString() ?? "NULL"}");
                _logger.LogInformation($"🔍 AVANT restauration - _utilisateur chargé: IdAgent={_utilisateur.IdAgent?.ToString() ?? "NULL"}");
                
                if (utilisateur.IdAgent.HasValue)
                {
                    _utilisateur.IdAgent = utilisateur.IdAgent;
                    _logger.LogInformation($"🔧 IdAgent restauré/assigné depuis utilisateur initial: {utilisateur.IdAgent.Value}");
                }
                
                _logger.LogInformation($"🔍 APRÈS restauration - _utilisateur: IdAgent={_utilisateur.IdAgent?.ToString() ?? "NULL"}");

                _logger.LogInformation($"✅ Informations complètes récupérées pour l'utilisateur {_utilisateur.IdUtilisateur}");

                // ✅ MULTI-RÔLES : Log des rôles chargés
                var userRolesCount = _utilisateur.UserRoles?.Count(ur => ur.Statut == true) ?? 0;
                var primaryRoleFromUser = _utilisateur.PrimaryRole;
                _logger.LogInformation($"🔐 Utilisateur {_utilisateur.IdUtilisateur} a {userRolesCount} rôle(s) actif(s). Rôle principal: {primaryRoleFromUser?.Nom ?? "Aucun"}");

                // ✅ IMPORTANT: S'assurer que _utilisateur a bien IdAgent et IdClient
                // Si l'utilisateur initial les a, les copier dans _utilisateur
                if (utilisateur.IdAgent.HasValue)
                {
                    _utilisateur.IdAgent = utilisateur.IdAgent;
                    _logger.LogInformation($"🔧 IdAgent copié dans _utilisateur: {utilisateur.IdAgent.Value}");
                }
                
                if (utilisateur.IdClient.HasValue)
                {
                    _utilisateur.IdClient = utilisateur.IdClient;
                    _logger.LogInformation($"🔧 IdClient copié dans _utilisateur: {utilisateur.IdClient.Value}");
                }
                
                // 🔍 Debug: Vérifier IdAgent et IdClient avant génération du token
                _logger.LogInformation($"🔍 AVANT GenerateToken - _utilisateur {_utilisateur.IdUtilisateur}: IdAgent = {_utilisateur.IdAgent?.ToString() ?? "NULL"}, IdClient = {_utilisateur.IdClient?.ToString() ?? "NULL"}");

                // 🔐 JWT: Générer le token JWT (avec tous les rôles)
                // ✅ Passer explicitement IdAgent depuis l'utilisateur initial
                _logger.LogInformation($"🔐 Génération du token JWT pour l'utilisateur {_utilisateur.IdUtilisateur}");
                _logger.LogInformation($"🔍 Valeurs passées à GenerateToken - utilisateur.IdAgent: {utilisateur.IdAgent?.ToString() ?? "NULL"}");
                var accessToken = _jwtService.GenerateToken(_utilisateur, utilisateur.IdAgent);
                var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

                // ✅ REFRESH TOKEN : Générer le refresh token
                var deviceInfo = $"{request.DeviceType} {request.DeviceModel}".Trim();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                    _utilisateur.IdUtilisateur,
                    string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                    ipAddress
                );
                _logger.LogInformation($"✅ Refresh token généré pour l'utilisateur {_utilisateur.IdUtilisateur}");

                // Construire le nom complet
                var nomComplet = _utilisateur.NomComplet ?? "Utilisateur";

                // ✨ Vérifier si l'utilisateur doit changer son mot de passe (propriété non disponible pour l'instant)
                // if (utilisateur.DoitChangerMotDePasse)
                // {
                //     return Ok(new AuthentificationResponse
                //     {
                //         Success = true,
                //         Message = "Vous devez changer votre mot de passe avant de continuer",
                //         AccessToken = accessToken,
                //         TokenType = "Bearer",
                //         ExpiresIn = expirationMinutes * 60,
                //         ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                //         DoitChangerMotDePasse = true,
                //         Utilisateur = _utilisateur
                //     });
                // }

                // 🔐 Retourner le JWT avec les informations utilisateur
                _logger.LogInformation($"✅ Token JWT généré avec succès pour l'utilisateur {_utilisateur.IdUtilisateur} - Expiration: {DateTime.UtcNow.AddMinutes(expirationMinutes):yyyy-MM-dd HH:mm:ss} UTC");
                
                // ✨ NOUVEAU : Récupérer les permissions de l'utilisateur (union de tous les rôles)
                var permissions = await _permissionService.GetUserPermissionsAsync(_utilisateur.IdUtilisateur);
                var permissionsList = permissions.ToList();
                _logger.LogInformation($"🔑 {permissionsList.Count} permission(s) chargée(s) pour l'utilisateur {_utilisateur.IdUtilisateur}");

                // ✅ MULTI-RÔLES : Récupérer tous les rôles actifs
                var userRoles = await _permissionService.GetUserRolesAsync(_utilisateur.IdUtilisateur);
                var userRolesList = userRoles.ToList();
                var primaryRole = await _permissionService.GetUserPrimaryRoleAsync(_utilisateur.IdUtilisateur);
                
                // ✨ NOUVEAU : Mapper les informations du Client si disponible
                ClientInfoDto? clientInfo = null;
                if (_utilisateur.Client != null)
                {
                    

                    clientInfo = new ClientInfoDto
                    {
                        IdClient = _utilisateur.Client.IdClient,
                        NomClient = _utilisateur.Client.NomClient,
                        Telephone = _utilisateur.Client.Telephone,
                        EmailClient = _utilisateur.Client.EmailClient,
                        GenreClient = _utilisateur.Client.GenreClient,
                        AdresseClient = _utilisateur.Client.AdresseClient,
                        Statut = _utilisateur.Client.Statut
                    };
                 //   _logger.LogInformation($"✅ Informations Client chargées pour l'utilisateur {_utilisateur.IdUtilisateur} - Client: {clientInfo.NomClient} ({clientInfo.CodeCons}) - {usages.Count} usage(s)");
                }
                else if (_utilisateur.IdClient.HasValue)
                {
                    // Si Client n'est pas chargé mais IdClient existe, charger le client avec ses usages
                    var client = await _context.Clients
                        .FirstOrDefaultAsync(c => c.IdClient == _utilisateur.IdClient.Value);
                    
                    if (client != null)
                    {
                        // ✨ NOUVEAU : Mapper les usages du client

                        clientInfo = new ClientInfoDto
                        {
                            IdClient = client.IdClient,
                            NomClient = client.NomClient,
                            Telephone = client.Telephone,
                            EmailClient = client.EmailClient,
                            GenreClient = client.GenreClient,
                            AdresseClient = client.AdresseClient,
                            Statut = client.Statut,
                        };
                      //  _logger.LogInformation($"✅ Informations Client chargées depuis la base pour l'utilisateur {_utilisateur.IdUtilisateur} - Client: {clientInfo.NomClient} ({clientInfo.CodeCons}) - {usages.Count} usage(s)");
                    }
                }

                // ✨ NOUVEAU : Mapper les informations de l'Agent si disponible
                AgentInfoDto? agentInfo = null;
                if (_utilisateur.Agent != null)
                {
                    agentInfo = new AgentInfoDto
                    {
                        IdAgent = _utilisateur.Agent.IdAgent,
                        Matricule = _utilisateur.Agent.Matricule,
                        NomComplet = _utilisateur.Agent.NomComplet,
                        Genre = _utilisateur.Agent.Genre,
                        DateNaissance = _utilisateur.Agent.DateNaissance,
                        TelephoneAgent = _utilisateur.Agent.TelephoneAgent,
                        EmailAgent = _utilisateur.Agent.EmailAgent,
                        Statut = _utilisateur.Agent.Statut,
                        Fonction = _utilisateur.Agent.Fonction,
                        RoleAgent = _utilisateur.Agent.RoleAgent,
                        PhotoUrl = _utilisateur.Agent.PhotoUrl,
                        IdSociete = _utilisateur.Agent.IdSociete,
                        IdSite = _utilisateur.Agent.IdSite,
                        AdresseResidence = _utilisateur.Agent.AdresseResidence,
                        Zone = _utilisateur.Agent.Zone
                    };
                    _logger.LogInformation($"✅ Informations Agent chargées pour l'utilisateur {_utilisateur.IdUtilisateur} - Agent: {agentInfo.NomComplet} ({agentInfo.Matricule})");
                }
                else if (_utilisateur.IdAgent.HasValue)
                {
                    // Si Agent n'est pas chargé mais IdAgent existe, charger l'agent
                    var agent = await _context.Agents.FindAsync(_utilisateur.IdAgent.Value);
                    if (agent != null)
                    {
                        agentInfo = new AgentInfoDto
                        {
                            IdAgent = agent.IdAgent,
                            Matricule = agent.Matricule,
                            NomComplet = agent.NomComplet,
                            Genre = agent.Genre,
                            DateNaissance = agent.DateNaissance,
                            TelephoneAgent = agent.TelephoneAgent,
                            EmailAgent = agent.EmailAgent,
                            Statut = agent.Statut,
                            Fonction = agent.Fonction,
                            RoleAgent = agent.RoleAgent,
                            PhotoUrl = agent.PhotoUrl,
                            IdSociete = agent.IdSociete,
                            IdSite = agent.IdSite,
                            AdresseResidence = agent.AdresseResidence,
                            Zone = agent.Zone
                        };
                        _logger.LogInformation($"✅ Informations Agent chargées depuis la base pour l'utilisateur {_utilisateur.IdUtilisateur} - Agent: {agentInfo.NomComplet} ({agentInfo.Matricule})");
                    }
                }
                
                return Ok(new AuthentificationResponse
                {
                    Success = true,
                    Message = "Authentification réussie",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken, // ✅ REFRESH TOKEN : Ajouter le refresh token
                    TokenType = "Bearer",
                    ExpiresIn = expirationMinutes * 60, // En secondes
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    DoitChangerMotDePasse = _utilisateur.DoitChangerMotDePasse == true,  // ✅ FIX: Utiliser la vraie valeur de la DB
                    Utilisateur = new Utilisateur
                    {
                        IdUtilisateur = _utilisateur.IdUtilisateur,
                        ReferenceUtilisateur = _utilisateur.ReferenceUtilisateur,
                        NomComplet = _utilisateur.NomComplet,
                        Email = _utilisateur.Email,
                        DefaultUsername = _utilisateur.DefaultUsername,
                        Telephone = _utilisateur.Telephone,
                        PhotoUrl = _utilisateur.PhotoUrl,
                        LieuNaissance = _utilisateur.LieuNaissance,
                        DateNaissance = _utilisateur.DateNaissance,
                        Genre = _utilisateur.Genre,
                        Statut = _utilisateur.Statut,
                        IdAgent = _utilisateur.IdAgent,  // ✅ Déjà présent
                        IdClient = _utilisateur.IdClient,  // ✨ NOUVEAU : Ajouter IdClient
                        IdSite = _utilisateur.IdSite, // ✨ NOUVEAU : Ajouter IdSite
                        DateCreation = _utilisateur.DateCreation,
                        IsConnecte = _utilisateur.IsConnecte,
                        IdSociete = _utilisateur.IdSociete,
                        Societe = _utilisateur.Societe,
                        IdRole = _utilisateur.IdRole  // Rétrocompatibilité
                    },
                    NomRole = primaryRole?.Nom ?? _utilisateur.Role?.Nom ?? "",  // ✅ MULTI-RÔLES : Utiliser le rôle principal
                    NomSociete = _utilisateur.Societe?.Nom ?? "",
                    AcceptNotification = true, // Valeur par défaut (champ supprimé du modèle Societe)
                    Permissions = permissionsList,
                    // ✅ MULTI-RÔLES : Ajouter les rôles dans la réponse
                    Roles = userRolesList,  // Tous les rôles actifs
                    PrimaryRole = primaryRole,  // Rôle principal
                    // ✨ NOUVEAU : Ajouter les informations Client et Agent
                    Client = clientInfo,
                    Agent = agentInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'authentification pour {EmailOuTelephone}", request.EmailOuTelephone);
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Connexion / inscription via Google ID token.
        /// Réponse : même contrat que <see cref="Authentifier"/>.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("auth/google")]
        [ProducesResponseType(typeof(AuthentificationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<AuthentificationResponse>> AuthentifierGoogle(
            [FromBody] GoogleAuthRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new { message = "idToken est requis." });

            try
            {
                var deviceInfo = Request.Headers.UserAgent.ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var response = await _googleAuthService.SignInWithGoogleAsync(
                    request.IdToken.Trim(),
                    string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                    ipAddress,
                    cancellationToken);
                return Ok(response);
            }
            catch (ExternalAuthException ex)
            {
                _logger.LogWarning(ex, "Échec auth Google ({StatusCode})", ex.StatusCode);
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue auth Google");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Connexion / inscription via Apple identity token.
        /// Réponse : même contrat que <see cref="Authentifier"/>.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("auth/apple")]
        [ProducesResponseType(typeof(AuthentificationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<AuthentificationResponse>> AuthentifierApple(
            [FromBody] GoogleAuthRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.IdToken))
                return BadRequest(new { message = "idToken est requis." });

            try
            {
                var deviceInfo = Request.Headers.UserAgent.ToString();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var response = await _appleAuthService.SignInWithAppleAsync(
                    request.IdToken.Trim(),
                    string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                    ipAddress,
                    cancellationToken);
                return Ok(response);
            }
            catch (ExternalAuthException ex)
            {
                _logger.LogWarning(ex, "Échec auth Apple ({StatusCode})", ex.StatusCode);
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue auth Apple");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Déconnecter l'utilisateur courant et invalider ses devices si nécessaire
        /// </summary>
        [HttpPost("deconnecter")]
        [ProducesResponseType(typeof(DeconnexionResponse), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DeconnexionResponse>> Deconnecter([FromBody] DeconnexionRequest? request = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }

                var deconnexionOk = await _utilisateurRepository.MarquerCommeDeconnecteAsync(currentUserId.Value);
                if (!deconnexionOk)
                {
                    _logger.LogWarning($"❌ Impossible de marquer l'utilisateur {currentUserId} comme déconnecté");
                    return NotFound(new { message = "Utilisateur introuvable" });
                }

                int devicesDesactives = 0;

                if (request != null)
                {
                    if (request.SupprimerTousLesDevices)
                    {
                        var devices = await _userDeviceRepository.GetByUtilisateurIdAsync(currentUserId.Value);
                        foreach (var device in devices)
                        {
                            try
                            {
                                device.Statut = false;
                                device.DateDerniereUtilisation = DateTime.Now;
                                var updated = await _userDeviceRepository.UpdateAsync(device);
                                if (updated != null)
                                {
                                    devicesDesactives++;
                                }
                            }
                            catch (Exception deviceEx)
                            {
                                _logger.LogWarning(deviceEx, $"⚠️ Erreur lors de la désactivation du device {device.IdUserDevice} pour l'utilisateur {currentUserId}");
                            }
                        }
                    }
                    else if (request.IdUserDevice.HasValue)
                    {
                        var device = await _userDeviceRepository.GetByIdAsync(request.IdUserDevice.Value);
                        if (device != null && device.IdUtilisateur == currentUserId.Value)
                        {
                            device.Statut = false;
                            device.DateDerniereUtilisation = DateTime.Now;
                            var updated = await _userDeviceRepository.UpdateAsync(device);
                            if (updated != null)
                            {
                                devicesDesactives = 1;
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Device {request.IdUserDevice} introuvable ou ne correspond pas à l'utilisateur {currentUserId}");
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(request.FcmToken))
                    {
                        var deleted = await _userDeviceRepository.DeleteByFcmTokenAsync(request.FcmToken);
                        if (deleted)
                        {
                            devicesDesactives = 1;
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Aucun device supprimé pour le token FCM fourni (User {currentUserId})");
                        }
                    }
                }

                _logger.LogInformation($"✅ Utilisateur {currentUserId} déconnecté. Devices désactivés: {devicesDesactives}");

                return Ok(new DeconnexionResponse
                {
                    Success = true,
                    Message = "Déconnexion effectuée avec succès",
                    DevicesDesactives = devicesDesactives
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la déconnexion de l'utilisateur");
                return StatusCode(500, new { message = "Erreur lors de la déconnexion", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("mot-de-passe-oublie")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> DemanderReinitialisationMotDePasse([FromBody] MotDePasseOublieRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            const string messageStandard = "Si un compte existe pour cette adresse email, un lien de réinitialisation a été envoyé.";

            try
            {
                var utilisateur = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Statut == true);

                if (utilisateur == null)
                {
                    _logger.LogWarning("❔ Demande mot de passe oublié pour email inexistant: {Email}", request.Email);
                    return Ok(new { message = messageStandard });
                }

                var anciensTokens = await _context.PasswordResetTokens
                    .Where(t => t.IdUtilisateur == utilisateur.IdUtilisateur && t.DateUtilisation == null)
                    .ToListAsync();

                foreach (var token in anciensTokens)
                {
                    token.DateUtilisation = DateTime.UtcNow;
                }

                var resetToken = new PasswordResetToken
                {
                    IdUtilisateur = utilisateur.IdUtilisateur,
                    Token = Guid.NewGuid().ToString("N"),
                    DateCreation = DateTime.UtcNow,
                    DateExpiration = DateTime.UtcNow.Add(_passwordResetTokenValidity)
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                var nomComplet = utilisateur.NomComplet ?? "Utilisateur";

                try
                {
                    var emailEnvoye = await _emailService.SendPasswordResetEmailAsync(
                        utilisateur.Email ?? string.Empty,
                        string.IsNullOrWhiteSpace(nomComplet) ? "Utilisateur" : nomComplet,
                        resetToken.Token);

                    if (emailEnvoye)
                    {
                        _logger.LogInformation("📧 Email de réinitialisation envoyé à {Email}", utilisateur.Email);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Échec de l'envoi de l'email de réinitialisation pour {Email}", utilisateur.Email);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "❌ Erreur lors de l'envoi de l'email de réinitialisation pour {Email}", utilisateur.Email);
                }

                return Ok(new { message = messageStandard });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la demande de mot de passe oublié");
                return StatusCode(500, new { message = "Erreur lors de la demande de réinitialisation", error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("mot-de-passe-oublie/confirmer")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> ConfirmerReinitialisationMotDePasse([FromBody] ConfirmerMotDePasseOublieRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var tokenPreview = string.IsNullOrWhiteSpace(request.Token)
                ? "null"
                : request.Token.Length <= 8
                    ? request.Token
                    : $"{request.Token[..4]}...{request.Token[^4..]}";

            _logger.LogInformation("🔐 Tentative de confirmation de réinitialisation. Token={TokenPreview}", tokenPreview);

            try
            {
                var token = await _context.PasswordResetTokens
                    .Include(t => t.Utilisateur)
                    .FirstOrDefaultAsync(t => t.Token == request.Token);

                if (token == null)
                {
                    _logger.LogWarning("❌ Token de réinitialisation introuvable. Token={TokenPreview}", tokenPreview);
                    return BadRequest(new { message = "Token invalide ou expiré" });
                }

                if (token.Utilise)
                {
                    _logger.LogWarning("❌ Token déjà utilisé. TokenId={TokenId}, Utilisateur={UserId}", token.IdPasswordResetToken, token.IdUtilisateur);
                    return BadRequest(new { message = "Ce lien de réinitialisation a déjà été utilisé" });
                }

                if (DateTime.UtcNow > token.DateExpiration)
                {
                    _logger.LogWarning("⌛ Token expiré. TokenId={TokenId}, Expiration={Expiration:O}, Now={Now:O}", token.IdPasswordResetToken, token.DateExpiration, DateTime.UtcNow);
                    return BadRequest(new { message = "Ce lien de réinitialisation a expiré" });
                }

                var utilisateur = token.Utilisateur;
                if (utilisateur == null || utilisateur.Statut != true)
                {
                    _logger.LogWarning("⚠️ Utilisateur associé introuvable ou inactif. TokenId={TokenId}, Utilisateur={UserId}, Statut={Statut}", token.IdPasswordResetToken, token.IdUtilisateur, utilisateur?.Statut);
                    return BadRequest(new { message = "Utilisateur introuvable ou inactif" });
                }

                utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(request.NouveauMotDePasse);
                utilisateur.DoitChangerMotDePasse = false;
                utilisateur.IsConnecte = false;

                token.DateUtilisation = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Mot de passe réinitialisé. Utilisateur={UserId}, TokenId={TokenId}", utilisateur.IdUtilisateur, token.IdPasswordResetToken);

                try
                {
                    var nomComplet = utilisateur.NomComplet ?? "Utilisateur";
                    await _emailService.SendPasswordChangedConfirmationEmailAsync(
                        utilisateur.Email ?? string.Empty,
                        string.IsNullOrWhiteSpace(nomComplet) ? "Utilisateur" : nomComplet,
                        DateTime.UtcNow);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "⚠️ Erreur lors de l'envoi de l'email de confirmation de réinitialisation");
                }

                return Ok(new { message = "Mot de passe réinitialisé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la confirmation de réinitialisation de mot de passe (token={TokenPreview})", tokenPreview);
                return StatusCode(500, new { message = "Erreur lors de la réinitialisation du mot de passe", error = ex.Message });
            }
        }

        // POST: api/Utilisateur/changer_mot_de_passe
        /// <summary>
        /// Changer le mot de passe d'un utilisateur
        /// </summary>
        /// <remarks>
        /// Un utilisateur peut changer UNIQUEMENT son propre mot de passe.
        /// Exception : Un Admin peut réinitialiser le mot de passe (tracé dans les logs).
        /// 
        /// Sécurité :
        /// - Vérification de l'ancien mot de passe obligatoire
        /// - Validation de la complexité du nouveau mot de passe
        /// - Logging de toutes les tentatives
        /// </remarks>
        [HttpPost("changer_mot_de_passe")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangerMotDePasse(ChangerMotDePasseRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // ═══════════════════════════════════════════════════════════
                // 1. VÉRIFIER QUE L'UTILISATEUR CHANGE SON PROPRE MOT DE PASSE
                // ═══════════════════════════════════════════════════════════
                
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }
                
                // L'utilisateur authentifié change son propre mot de passe
                var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
                if (currentUser == null)
                {
                    return Unauthorized(new { message = "Utilisateur non trouvé" });
                }
                
                // L'utilisateur authentifié change son propre mot de passe
                var utilisateur = await _utilisateurRepository.GetByIdAsync(userId);
                if (utilisateur == null)
                {
                    _logger.LogWarning($"❌ Utilisateur {userId} non trouvé pour changement de mot de passe");
                    return NotFound(new { message = "Utilisateur non trouvé" });
                }

                // ═══════════════════════════════════════════════════════════
                // 2. CHANGER LE MOT DE PASSE
                // ═══════════════════════════════════════════════════════════
                
                bool success = await _utilisateurRepository.ChangerMotDePasseAsync(
                    userId, 
                    request.AncienMotDePasse, 
                    request.NouveauMotDePasse
                );

                if (!success)
                {
                    _logger.LogWarning($"❌ Ancien mot de passe incorrect pour User {userId}");
                    return BadRequest(new { message = "Ancien mot de passe incorrect" });
                }

                _logger.LogInformation($"✅ Mot de passe changé avec succès pour User {userId}");
                return Ok(new { message = "Mot de passe changé avec succès" });
            }
            catch (Exception ex)
            {
                var currentUserId = GetCurrentUserId();
                _logger.LogError(ex, $"❌ Erreur lors du changement de mot de passe pour User {currentUserId}");
                return StatusCode(500, new { message = "Erreur lors du changement de mot de passe", error = ex.Message });
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            _logger.LogWarning("❌ Impossible de déterminer l'utilisateur courant à partir du token JWT");
            return null;
        }

        // PUT: api/Utilisateur/toggle-statut/{id}
        /// <summary>
        /// Activer/Désactiver un utilisateur ou désactiver son propre compte
        /// </summary>
        /// <remarks>
        /// **Auto-désactivation** (`id` = utilisateur connecté) :
        /// - Permission `Utilisateur.DeactivateSelf` (tous les rôles)
        /// - Passe le compte à `Statut = false` uniquement (pas de réactivation self)
        ///
        /// **Gestion admin** (`id` ≠ utilisateur connecté) :
        /// - Permission `Utilisateur.Update`
        /// - Toggle actif/inactif ; même société (sauf Super-Admin) ; pas de désactivation d'un Super-Admin par un non Super-Admin
        /// </remarks>
        [HttpPut("toggle-statut/{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }

                var targetUser = await _utilisateurRepository.GetByIdAsync(id);
                if (targetUser == null)
                {
                    _logger.LogWarning("Utilisateur {Id} non trouvé pour toggle statut", id);
                    return NotFound(new { message = "Utilisateur non trouvé" });
                }

                if (userId == id)
                {
                    var canDeactivateSelf = await _permissionService.UserHasPermissionAsync(userId, "Utilisateur.DeactivateSelf");
                    if (!canDeactivateSelf)
                    {
                        _logger.LogWarning("User {UserId} sans permission Utilisateur.DeactivateSelf", userId);
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Permission insuffisante pour désactiver votre compte" });
                    }

                    if (targetUser.Statut != true)
                    {
                        return BadRequest(new { message = "Compte déjà désactivé" });
                    }

                    var deactivated = await _utilisateurRepository.DeactivateAsync(id);
                    if (!deactivated)
                    {
                        return BadRequest(new { message = "Compte déjà désactivé" });
                    }

                    await _utilisateurRepository.MarquerCommeDeconnecteAsync(id);
                    await _refreshTokenService.RevokeAllRefreshTokensAsync(id);

                    var selfUser = await _utilisateurRepository.GetByIdAsync(id);
                    if (selfUser != null)
                        selfUser.MotDePasseHash = null;

                    _logger.LogInformation("Compte désactivé par l'utilisateur {UserId} (auto-désactivation)", userId);
                    return Ok(new
                    {
                        message = "Compte désactivé avec succès",
                        utilisateur = selfUser,
                        nouveauStatut = false
                    });
                }

                var canUpdateOthers = await _permissionService.UserHasPermissionAsync(userId, "Utilisateur.Update");
                if (!canUpdateOthers)
                {
                    _logger.LogWarning("User {UserId} sans permission Utilisateur.Update pour toggle User {TargetId}", userId, id);
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "Permission insuffisante. Pour désactiver votre compte, appelez cette route avec votre propre identifiant utilisateur.",
                        code = "TOGGLE_STATUT_NOT_SELF"
                    });
                }

                var currentUser = await _utilisateurRepository.GetByIdAsync(userId);
                bool isSuperAdmin = currentUser?.Role?.Nom == "Super-Admin";

                if (!isSuperAdmin && targetUser.IdSociete != currentUser?.IdSociete)
                {
                    _logger.LogWarning("Admin {UserId} (Société {Societe}) tente de modifier statut User {TargetId} (Société {TargetSociete})",
                        userId, currentUser?.IdSociete, id, targetUser.IdSociete);
                    return Forbid();
                }

                if (!isSuperAdmin && targetUser.Role?.Nom == "Super-Admin")
                {
                    _logger.LogWarning("Admin {UserId} tente de désactiver un Super-Admin ({TargetId})", userId, id);
                    return Forbid();
                }

                var ancienStatut = targetUser.Statut;
                var success = await _utilisateurRepository.ToggleStatutAsync(id);

                if (!success)
                {
                    _logger.LogError("Erreur lors du changement de statut pour User {Id}", id);
                    return NotFound(new { message = "Erreur lors du changement de statut" });
                }

                var utilisateurAvecRelations = await _utilisateurRepository.GetByIdAsync(id);
                if (utilisateurAvecRelations != null)
                    utilisateurAvecRelations.MotDePasseHash = null;

                _logger.LogInformation("Statut modifié pour User {TargetId} : {Ancien} → {Nouveau} par Admin {UserId}",
                    id, ancienStatut, utilisateurAvecRelations?.Statut, userId);

                return Ok(new
                {
                    message = "Statut modifié avec succès",
                    utilisateur = utilisateurAvecRelations,
                    nouveauStatut = utilisateurAvecRelations?.Statut
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de statut pour User {Id}", id);
                return StatusCode(500, new { message = "Erreur lors du changement de statut", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔐 RÉINITIALISATION MOT DE PASSE EN MASSE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Réinitialiser le mot de passe en masse pour tous les utilisateurs d'une école avec un rôle spécifique
        /// </summary>
        /// <remarks>
        /// **Restrictions :**
        /// - Réservé aux Admin et Super-Admin uniquement
        /// - Admin : Peut réinitialiser uniquement dans son école
        /// - Super-Admin : Peut réinitialiser dans n'importe quelle école
        /// - Admin ne peut PAS réinitialiser les Super-Admin ni les autres Admin
        /// </remarks>
        [HttpPost("reinitialiser-masse")]
        [Permission("Utilisateur.ChangePassword")]
        [ProducesResponseType(typeof(ReinitialiserMotDePasseMasseResponse), 200)]
        public async Task<ActionResult<ReinitialiserMotDePasseMasseResponse>> ReinitialiserMotDePasseMasse(
            [FromBody] ReinitialiserMotDePasseMasseDto dto)
        {
            try
            {
                // 1️⃣ Récupérer l'utilisateur connecté
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var userId = currentUserId.Value;
                var currentUser = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .Include(u => u.Societe)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (currentUser == null)
                    return Unauthorized(new { message = "Utilisateur non authentifié" });

                var roleUtilisateurConnecte = currentUser.Role?.Nom;

                _logger.LogInformation($"🔐 Tentative de réinitialisation en masse par {roleUtilisateurConnecte} (User {userId}) pour École {dto.IdSociete}, Rôle {dto.IdRole}");

                // 2️⃣ Vérifier les permissions (Admin ou Super-Admin uniquement)
                if (roleUtilisateurConnecte != "Admin" && roleUtilisateurConnecte != "Super-Admin")
                {
                    _logger.LogWarning($"⛔ Accès refusé: {roleUtilisateurConnecte} (User {userId}) a essayé de réinitialiser des mots de passe");
                    return Forbid();
                }

                // 3️⃣ Admin : Vérifier qu'il réinitialise uniquement dans SON école
                if (roleUtilisateurConnecte == "Admin" && currentUser.IdSociete != dto.IdSociete)
                {
                    _logger.LogWarning($"⛔ Accès refusé: Admin (User {userId}) a essayé de réinitialiser des mots de passe dans une autre école ({dto.IdSociete})");
                    return StatusCode(403, new { message = "Vous ne pouvez réinitialiser que les utilisateurs de votre école" });
                }

                // 4️⃣ Récupérer le rôle cible pour vérification
                var roleCible = await _context.Roles.FindAsync(dto.IdRole);
                if (roleCible == null)
                {
                    return NotFound(new { message = $"Rôle {dto.IdRole} introuvable" });
                }

                // 5️⃣ Admin ne peut PAS réinitialiser les Super-Admin ni les autres Admin
                if (roleUtilisateurConnecte == "Admin" && (roleCible.Nom == "Super-Admin" || roleCible.Nom == "Admin"))
                {
                    _logger.LogWarning($"⛔ Accès refusé: Admin (User {userId}) a essayé de réinitialiser des {roleCible.Nom}");
                    return StatusCode(403, new { message = $"Vous ne pouvez pas réinitialiser le mot de passe des {roleCible.Nom}" });
                }

                // 6️⃣ Récupérer l'école pour le message
                var societe = await _context.Societes.FindAsync(dto.IdSociete);
                if (societe == null)
                {
                    return NotFound(new { message = $"École {dto.IdSociete} introuvable" });
                }

                // 7️⃣ Réinitialiser les mots de passe
                var nombreUtilisateurs = await _utilisateurRepository.ReinitialiserMotDePasseMasseAsync(
                    dto.IdSociete, dto.IdRole, dto.NouveauMotDePasse);

                if (nombreUtilisateurs == 0)
                {
                    return NotFound(new { message = $"Aucun utilisateur actif trouvé avec le rôle '{roleCible.Nom}' dans l'école '{societe.Nom}'" });
                }

                _logger.LogInformation($"✅ {nombreUtilisateurs} utilisateur(s) réinitialisé(s) avec succès par {roleUtilisateurConnecte} (User {userId})");

                return Ok(new ReinitialiserMotDePasseMasseResponse
                {
                    Success = true,
                    Message = $"{nombreUtilisateurs} utilisateur(s) réinitialisé(s) avec succès",
                    NombreUtilisateurs = nombreUtilisateurs,
                    Details = new DetailsReinitialisation
                    {
                        Societe = societe.Nom,
                        Role = roleCible.Nom,
                        MotDePasseChange = true,
                        DoitChangerMotDePasse = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réinitialisation en masse");
                return StatusCode(500, new { message = "Erreur lors de la réinitialisation des mots de passe", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔐 RÉINITIALISATION MOT DE PASSE INDIVIDUELLE
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Réinitialiser le mot de passe d'un utilisateur spécifique
        /// </summary>
        /// <remarks>
        /// **Restrictions :**
        /// - Réservé aux Admin et Super-Admin uniquement
        /// - Admin : Peut réinitialiser uniquement les utilisateurs de son école
        /// - Super-Admin : Peut réinitialiser n'importe quel utilisateur
        /// - Admin ne peut PAS réinitialiser les Super-Admin ni les autres Admin
        /// </remarks>
        [HttpPost("reinitialiser-un")]
        [Permission("Utilisateur.ChangePassword")]
        [ProducesResponseType(typeof(ReinitialiserMotDePasseIndividuelResponse), 200)]
        public async Task<ActionResult<ReinitialiserMotDePasseIndividuelResponse>> ReinitialiserMotDePasseIndividuel(
            [FromBody] ReinitialiserMotDePasseIndividuelDto dto)
        {
            try
            {
                // 1️⃣ Récupérer l'utilisateur connecté
                var currentUserId = GetCurrentUserId();
                if (currentUserId == null)
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var userId = currentUserId.Value;
                var currentUser = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .Include(u => u.Societe)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId);

                if (currentUser == null)
                    return Unauthorized(new { message = "Utilisateur non authentifié" });

                var roleUtilisateurConnecte = currentUser.Role?.Nom;

                _logger.LogInformation($"🔐 Tentative de réinitialisation individuelle par {roleUtilisateurConnecte} (User {userId}) pour User {dto.IdUtilisateur}");

                // 2️⃣ Vérifier les permissions (Admin ou Super-Admin uniquement)
                if (roleUtilisateurConnecte != "Admin" && roleUtilisateurConnecte != "Super-Admin")
                {
                    _logger.LogWarning($"⛔ Accès refusé: {roleUtilisateurConnecte} (User {userId}) a essayé de réinitialiser un mot de passe");
                    return Forbid();
                }

                // 3️⃣ Récupérer l'utilisateur cible
                var targetUser = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .Include(u => u.Societe)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == dto.IdUtilisateur);

                if (targetUser == null)
                {
                    return NotFound(new { message = $"Utilisateur {dto.IdUtilisateur} introuvable" });
                }

                // 4️⃣ Admin : Vérifier qu'il réinitialise uniquement dans SON école
                if (roleUtilisateurConnecte == "Admin" && currentUser.IdSociete != targetUser.IdSociete)
                {
                    _logger.LogWarning($"⛔ Accès refusé: Admin (User {userId}) a essayé de réinitialiser un utilisateur d'une autre école ({targetUser.IdSociete})");
                    return StatusCode(403, new { message = "Vous ne pouvez réinitialiser que les utilisateurs de votre école" });
                }

                // 5️⃣ Admin ne peut PAS réinitialiser les Super-Admin ni les autres Admin
                if (roleUtilisateurConnecte == "Admin" && 
                    (targetUser.Role?.Nom == "Super-Admin" || targetUser.Role?.Nom == "Admin"))
                {
                    _logger.LogWarning($"⛔ Accès refusé: Admin (User {userId}) a essayé de réinitialiser un {targetUser.Role?.Nom} (User {dto.IdUtilisateur})");
                    return StatusCode(403, new { message = $"Vous ne pouvez pas réinitialiser le mot de passe des {targetUser.Role?.Nom}" });
                }

                // 6️⃣ Réinitialiser le mot de passe
                var success = await _utilisateurRepository.ReinitialiserMotDePasseIndividuelAsync(
                    dto.IdUtilisateur, dto.NouveauMotDePasse);

                if (!success)
                {
                    return StatusCode(500, new { message = "Erreur lors de la réinitialisation du mot de passe" });
                }

                _logger.LogInformation($"✅ Mot de passe réinitialisé pour User {dto.IdUtilisateur} par {roleUtilisateurConnecte} (User {userId})");

                // 7️⃣ Retourner les informations (sans le hash du mot de passe)
                return Ok(new ReinitialiserMotDePasseIndividuelResponse
                {
                    Success = true,
                    Message = "Mot de passe réinitialisé avec succès",
                    Utilisateur = new UtilisateurReinitialise
                    {
                        IdUtilisateur = targetUser.IdUtilisateur,
                        NomComplet = targetUser.NomComplet ?? "Utilisateur",
                        Email = targetUser.Email,
                        Telephone = targetUser.Telephone,
                        DoitChangerMotDePasse = true
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réinitialisation individuelle");
                return StatusCode(500, new { message = "Erreur lors de la réinitialisation du mot de passe", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Gestion des rôles utilisateur
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupérer tous les rôles d'un utilisateur
        /// </summary>
        /// <param name="id">ID de l'utilisateur</param>
        /// <returns>Liste des rôles actifs de l'utilisateur</returns>
        [HttpGet("{id}/roles")]
        [Permission("Role.Read")]
        [ProducesResponseType(typeof(IEnumerable<Role>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<Role>>> GetUserRoles(int id)
        {
            try
            {
                // Vérifier que l'utilisateur existe
                var user = await _utilisateurRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"Utilisateur {id} introuvable" });
                }

                // Récupérer les rôles
                var roles = await _permissionService.GetUserRolesAsync(id);

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des rôles pour l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors de la récupération des rôles", error = ex.Message });
            }
        }

        /// <summary>
        /// Ajouter un rôle à un utilisateur
        /// </summary>
        /// <param name="id">ID de l'utilisateur</param>
        /// <param name="roleId">ID du rôle à ajouter</param>
        /// <param name="isPrimary">Indique si ce rôle doit être le rôle principal (défaut: false)</param>
        /// <returns>Message de succès</returns>
        [HttpPost("{id}/roles/{roleId}")]
        [Permission("Role.Update")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> AddRoleToUser(
            int id, 
            int roleId, 
            [FromQuery] bool isPrimary = false)
        {
            try
            {
                // Récupérer l'utilisateur connecté
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }

                // Vérifier que l'utilisateur cible existe
                var user = await _utilisateurRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"Utilisateur {id} introuvable" });
                }

                // Vérifier que le rôle existe
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { message = $"Rôle {roleId} introuvable" });
                }

                // Vérifier les permissions (Admin ne peut modifier que les utilisateurs de son école)
                var currentUser = await _utilisateurRepository.GetByIdAsync(currentUserId);
                if (currentUser?.Role?.Nom != "Super-Admin" && currentUser?.IdSociete != user.IdSociete)
                {
                    return StatusCode(403, new { message = "Vous ne pouvez modifier que les utilisateurs de votre école" });
                }

                // Ajouter le rôle
                var success = await _utilisateurRepository.AddRoleToUserAsync(id, roleId, currentUserId, isPrimary);

                if (!success)
                {
                    return BadRequest(new { message = "Impossible d'ajouter le rôle. Il est peut-être déjà assigné." });
                }

                _logger.LogInformation($"✅ Rôle {roleId} ajouté à l'utilisateur {id} par {currentUserId}");

                // Récupérer les rôles mis à jour
                var updatedRoles = await _permissionService.GetUserRolesAsync(id);

                return Ok(new
                {
                    message = "Rôle ajouté avec succès",
                    utilisateurId = id,
                    roleId = roleId,
                    roleNom = role.Nom,
                    isPrimary = isPrimary,
                    roles = updatedRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de l'ajout du rôle {roleId} à l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors de l'ajout du rôle", error = ex.Message });
            }
        }

        /// <summary>
        /// Retirer un rôle d'un utilisateur
        /// </summary>
        /// <param name="id">ID de l'utilisateur</param>
        /// <param name="roleId">ID du rôle à retirer</param>
        /// <returns>Message de succès</returns>
        [HttpDelete("{id}/roles/{roleId}")]
        [Permission("Role.Update")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> RemoveRoleFromUser(int id, int roleId)
        {
            try
            {
                // Vérifier que l'utilisateur cible existe
                var user = await _utilisateurRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"Utilisateur {id} introuvable" });
                }

                // Vérifier que le rôle existe
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { message = $"Rôle {roleId} introuvable" });
                }

                // Vérifier que l'utilisateur a ce rôle
                var userRoles = await _permissionService.GetUserRolesAsync(id);
                if (!userRoles.Any(r => r.IdRole == roleId))
                {
                    return BadRequest(new { message = "L'utilisateur n'a pas ce rôle" });
                }

                // Vérifier qu'il reste au moins un rôle actif
                if (userRoles.Count() <= 1)
                {
                    return BadRequest(new { message = "Impossible de retirer le dernier rôle actif. Un utilisateur doit avoir au moins un rôle." });
                }

                // Récupérer l'utilisateur connecté pour vérifier les permissions
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }

                var currentUser = await _utilisateurRepository.GetByIdAsync(currentUserId);
                if (currentUser?.Role?.Nom != "Super-Admin" && currentUser?.IdSociete != user.IdSociete)
                {
                    return StatusCode(403, new { message = "Vous ne pouvez modifier que les utilisateurs de votre école" });
                }

                // Retirer le rôle
                var success = await _utilisateurRepository.RemoveRoleFromUserAsync(id, roleId);

                if (!success)
                {
                    return BadRequest(new { message = "Impossible de retirer le rôle" });
                }

                _logger.LogInformation($"✅ Rôle {roleId} retiré de l'utilisateur {id} par {currentUserId}");

                // Récupérer les rôles mis à jour
                var updatedRoles = await _permissionService.GetUserRolesAsync(id);

                return Ok(new
                {
                    message = "Rôle retiré avec succès",
                    utilisateurId = id,
                    roleId = roleId,
                    roleNom = role.Nom,
                    roles = updatedRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors du retrait du rôle {roleId} de l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors du retrait du rôle", error = ex.Message });
            }
        }

        /// <summary>
        /// Définir le rôle principal d'un utilisateur
        /// </summary>
        /// <param name="id">ID de l'utilisateur</param>
        /// <param name="roleId">ID du rôle à définir comme principal</param>
        /// <returns>Message de succès</returns>
        [HttpPut("{id}/roles/{roleId}/primary")]
        [Permission("Role.Update")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> SetPrimaryRole(int id, int roleId)
        {
            try
            {
                // Récupérer l'utilisateur connecté
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
                {
                    return Unauthorized(new { message = "Token JWT invalide" });
                }

                // Vérifier que l'utilisateur cible existe
                var user = await _utilisateurRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = $"Utilisateur {id} introuvable" });
                }

                // Vérifier que le rôle existe
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { message = $"Rôle {roleId} introuvable" });
                }

                // Vérifier que l'utilisateur a ce rôle
                var userRoles = await _permissionService.GetUserRolesAsync(id);
                if (!userRoles.Any(r => r.IdRole == roleId))
                {
                    return BadRequest(new { message = "L'utilisateur n'a pas ce rôle. Ajoutez-le d'abord." });
                }

                // Vérifier les permissions
                var currentUser = await _utilisateurRepository.GetByIdAsync(currentUserId);
                if (currentUser?.Role?.Nom != "Super-Admin" && currentUser?.IdSociete != user.IdSociete)
                {
                    return StatusCode(403, new { message = "Vous ne pouvez modifier que les utilisateurs de votre école" });
                }

                // Définir le rôle principal (cela désactivera automatiquement les autres rôles principaux)
                var success = await _utilisateurRepository.AddRoleToUserAsync(id, roleId, currentUserId, isPrimary: true);

                if (!success)
                {
                    return BadRequest(new { message = "Impossible de définir le rôle principal" });
                }

                _logger.LogInformation($"✅ Rôle principal {roleId} défini pour l'utilisateur {id} par {currentUserId}");

                // Récupérer les rôles mis à jour
                var updatedRoles = await _permissionService.GetUserRolesAsync(id);
                var primaryRole = await _permissionService.GetUserPrimaryRoleAsync(id);

                return Ok(new
                {
                    message = "Rôle principal défini avec succès",
                    utilisateurId = id,
                    roleId = roleId,
                    roleNom = role.Nom,
                    primaryRole = primaryRole,
                    roles = updatedRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la définition du rôle principal {roleId} pour l'utilisateur {id}");
                return StatusCode(500, new { message = "Erreur lors de la définition du rôle principal", error = ex.Message });
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ REFRESH TOKEN : Endpoints pour gérer les refresh tokens
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Rafraîchit l'access token en utilisant un refresh token valide
        /// </summary>
        /// <param name="request">Contient le refresh token</param>
        /// <returns>Nouvel access token et nouveau refresh token</returns>
        [AllowAnonymous]
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthentificationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AuthentificationResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token requis" });
            }

            try
            {
                // Valider le refresh token
                var userId = await _refreshTokenService.ValidateRefreshTokenAsync(request.RefreshToken);
                if (userId == null)
                {
                    _logger.LogWarning($"❌ Refresh token invalide ou expiré");
                    return Unauthorized(new { message = "Refresh token invalide ou expiré" });
                }

                // Récupérer l'utilisateur avec tous ses rôles
                var utilisateur = await _context.Utilisateurs
                    .Include(u => u.Role)
                    .Include(u => u.Societe)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdUtilisateur == userId && u.Statut == true);

                if (utilisateur == null)
                {
                    _logger.LogWarning($"❌ Utilisateur {userId} non trouvé ou inactif");
                    return Unauthorized(new { message = "Utilisateur non trouvé ou inactif" });
                }

                // Vérifier si l'école est active
                if (utilisateur.IdSociete.HasValue)
                {
                    var societe = utilisateur.Societe;
                    if (societe == null || societe.Statut != true)
                    {
                        _logger.LogWarning($"❌ École désactivée pour l'utilisateur {userId}");
                        return Unauthorized(new { message = "Accès refusé : Votre école a été désactivée" });
                    }
                }

                // Révoquer l'ancien refresh token (rotation)
                await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);

                // Générer un nouvel access token
                var accessToken = _jwtService.GenerateToken(utilisateur);
                var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");

                // Générer un nouveau refresh token
                var deviceInfo = request.DeviceInfo;
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                    utilisateur.IdUtilisateur,
                    deviceInfo,
                    ipAddress
                );

                _logger.LogInformation($"✅ Token rafraîchi avec succès pour l'utilisateur {userId}");

                // Récupérer les permissions et rôles
                var permissions = await _permissionService.GetUserPermissionsAsync(utilisateur.IdUtilisateur);
                var permissionsList = permissions.ToList();
                var userRoles = await _permissionService.GetUserRolesAsync(utilisateur.IdUtilisateur);
                var userRolesList = userRoles.ToList();
                var primaryRole = await _permissionService.GetUserPrimaryRoleAsync(utilisateur.IdUtilisateur);

                return Ok(new AuthentificationResponse
                {
                    Success = true,
                    Message = "Token rafraîchi avec succès",
                    AccessToken = accessToken,
                    RefreshToken = newRefreshToken,
                    TokenType = "Bearer",
                    ExpiresIn = expirationMinutes * 60,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    DoitChangerMotDePasse = utilisateur.DoitChangerMotDePasse == true,
                    Utilisateur = new Utilisateur
                    {
                        IdUtilisateur = utilisateur.IdUtilisateur,
                        ReferenceUtilisateur = utilisateur.ReferenceUtilisateur,
                        NomComplet = utilisateur.NomComplet,
                        Email = utilisateur.Email,
                        DefaultUsername = utilisateur.DefaultUsername,
                        Telephone = utilisateur.Telephone,
                        PhotoUrl = utilisateur.PhotoUrl,
                        LieuNaissance = utilisateur.LieuNaissance,
                        DateNaissance = utilisateur.DateNaissance,
                        Genre = utilisateur.Genre,
                        Statut = utilisateur.Statut,
                        IdAgent = utilisateur.IdAgent,
                        IdClient = utilisateur.IdClient,
                        IdSite = utilisateur.IdSite,
                        DateCreation = utilisateur.DateCreation,
                        IsConnecte = utilisateur.IsConnecte,
                        IdSociete = utilisateur.IdSociete,
                        Societe = utilisateur.Societe,
                        IdRole = utilisateur.IdRole
                    },
                    NomRole = primaryRole?.Nom ?? utilisateur.Role?.Nom ?? "",
                    NomSociete = utilisateur.Societe?.Nom ?? "",
                    AcceptNotification = true, // Valeur par défaut (champ supprimé du modèle Societe)
                    Permissions = permissionsList,
                    Roles = userRolesList,
                    PrimaryRole = primaryRole
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors du rafraîchissement du token");
                return StatusCode(500, new { message = "Erreur lors du rafraîchissement du token", error = ex.Message });
            }
        }

        /// <summary>
        /// Révoque un refresh token spécifique (déconnexion d'un appareil)
        /// </summary>
        /// <param name="request">Contient le refresh token à révoquer</param>
        /// <returns>Confirmation de révocation</returns>
        [AllowAnonymous]
        [HttpPost("revoke-token")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<object>> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token requis" });
            }

            try
            {
                var revoked = await _refreshTokenService.RevokeRefreshTokenAsync(request.RefreshToken);
                if (revoked)
                {
                    _logger.LogInformation($"✅ Refresh token révoqué avec succès");
                    return Ok(new { message = "Refresh token révoqué avec succès" });
                }
                else
                {
                    return BadRequest(new { message = "Refresh token non trouvé ou déjà révoqué" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la révocation du token");
                return StatusCode(500, new { message = "Erreur lors de la révocation du token", error = ex.Message });
            }
        }

        /// <summary>
        /// Révoque tous les refresh tokens de l'utilisateur connecté (déconnexion de tous les appareils)
        /// </summary>
        /// <returns>Confirmation de révocation</returns>
        [HttpPost("revoke-all-tokens")]
        [Permission("Utilisateur.Update")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<object>> RevokeAllTokens()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var revoked = await _refreshTokenService.RevokeAllRefreshTokensAsync(userId);
                if (revoked)
                {
                    _logger.LogInformation($"✅ Tous les refresh tokens révoqués pour l'utilisateur {userId}");
                    return Ok(new { message = "Tous les refresh tokens ont été révoqués avec succès" });
                }
                else
                {
                    return Ok(new { message = "Aucun refresh token actif trouvé" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la révocation de tous les tokens");
                return StatusCode(500, new { message = "Erreur lors de la révocation des tokens", error = ex.Message });
            }
        }

        private static bool IsDuplicateEmailException(DbUpdateException ex)
        {
            // MariaDB/MySQL duplicate entry : error code 1062
            var mySqlEx = ex.InnerException as MySqlException
                          ?? ex.InnerException?.InnerException as MySqlException;

            if (mySqlEx != null)
            {
                if (mySqlEx.Number == 1062 || mySqlEx.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    return true;
            }

            var message = ex.InnerException?.Message ?? ex.Message;
            return !string.IsNullOrEmpty(message)
                   && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("email", StringComparison.OrdinalIgnoreCase);
        }
    }
}
