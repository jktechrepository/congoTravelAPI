using CongoTravel.Attributes;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des permissions RBAC
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Toutes les routes nécessitent une authentification
    
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionController> _logger;

        public PermissionController(
            IPermissionService permissionService,
            ILogger<PermissionController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // LECTURE DES PERMISSIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Récupère toutes les permissions du système
        /// </summary>
        [HttpGet]
        [Permission("Permission.ReadAll")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetAllPermissions()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des permissions");
                return StatusCode(500, "Erreur lors de la récupération des permissions");
            }
        }

        /*
         Permission.ReadAll
        Permission.Read
         */

        /// <summary>
        /// Récupère une permission spécifique par son ID
        /// </summary>
        [HttpGet("{id}")]
        [Permission("Permission.Read")]
        public async Task<ActionResult<Permission>> GetPermission(int id)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByIdAsync(id);
                
                if (permission == null)
                {
                    return NotFound($"Permission avec l'ID {id} introuvable");
                }

                return Ok(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la permission {PermissionId}", id);
                return StatusCode(500, "Erreur lors de la récupération de la permission");
            }
        }

        /// <summary>
        /// Récupère les permissions par catégorie (ex: "Societe", "Paiement", etc.)
        /// </summary>
        [HttpGet("by-category/{category}")]
      //  [Permission("Permission.ReadAll")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetPermissionsByCategory(string category)
        {
            try
            {
                var permissions = await _permissionService.GetPermissionsByCategoryAsync(category);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des permissions pour la catégorie {Category}", category);
                return StatusCode(500, "Erreur lors de la récupération des permissions");
            }
        }

        /// <summary>
        /// Récupère toutes les permissions d'un rôle spécifique
        /// </summary>
        [HttpGet("role/{roleId}")]
      //  [Permission("Permission.ReadAll")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetRolePermissions(int roleId)
        {
            try
            {
                var permissions = await _permissionService.GetRolePermissionsAsync(roleId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des permissions du rôle {RoleId}", roleId);
                return StatusCode(500, "Erreur lors de la récupération des permissions du rôle");
            }
        }

        /// <summary>
        /// Récupère toutes les permissions de l'utilisateur connecté
        /// </summary>
        [HttpGet("my-permissions")]
        [Authorize] // Pas besoin de Permission ici, tout utilisateur authentifié peut voir ses permissions
        public async Task<ActionResult<IEnumerable<string>>> GetMyPermissions()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var permissions = await _permissionService.GetUserPermissionsAsync(userId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des permissions de l'utilisateur");
                return StatusCode(500, "Erreur lors de la récupération de vos permissions");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // GESTION DES PERMISSIONS (CRUD)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Crée une nouvelle permission (Super-Admin uniquement)
        /// </summary>
        [HttpPost]
        [Permission("Permission.Create")]
        public async Task<ActionResult<Permission>> CreatePermission([FromBody] Permission permission)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdPermission = await _permissionService.CreatePermissionAsync(permission);
                return CreatedAtAction(nameof(GetPermission), new { id = createdPermission.IdPermission }, createdPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la permission");
                return StatusCode(500, "Erreur lors de la création de la permission");
            }
        }

        /// <summary>
        /// Met à jour une permission existante (Super-Admin uniquement)
        /// </summary>
        [HttpPut("{id}")]
        [Permission("Permission.Update")]
        public async Task<ActionResult<Permission>> UpdatePermission(int id, [FromBody] UpdatePermissionDto dto)
        {
            try
            {
                if (id != dto.IdPermission)
                {
                    return BadRequest(new { message = "L'ID de la permission ne correspond pas" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existing = await _permissionService.GetPermissionByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = $"Permission avec l'ID {id} introuvable" });
                }

                existing.Nom = dto.Nom;
                existing.Description = dto.Description;
                existing.Categorie = dto.Categorie;

                var updated = await _permissionService.UpdatePermissionAsync(existing);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la permission {PermissionId}", id);
                return StatusCode(500, "Erreur lors de la mise à jour de la permission");
            }
        }

        /// <summary>
        /// Supprime une permission (Super-Admin uniquement)
        /// </summary>
        [HttpDelete("{id}")]
      //  [Permission("Permission.Delete")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            try
            {
                var deleted = await _permissionService.DeletePermissionAsync(id);
                
                if (!deleted)
                {
                    return NotFound($"Permission avec l'ID {id} introuvable");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la permission {PermissionId}", id);
                return StatusCode(500, "Erreur lors de la suppression de la permission");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // ASSIGNATION / RÉVOCATION DE PERMISSIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Assigne une permission à un rôle
        /// </summary>
        /// <param name="request">Objet contenant roleId et permissionId</param>
        [HttpPost("assign")]
      //  [Permission("Permission.Assign")]
        public async Task<IActionResult> AssignPermissionToRole([FromBody] AssignPermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var assigned = await _permissionService.AssignPermissionToRoleAsync(request.RoleId, request.PermissionId);
                
                if (!assigned)
                {
                    return BadRequest("Impossible d'assigner la permission (déjà assignée ou rôle/permission introuvable)");
                }

                return Ok(new { message = "Permission assignée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'assignation de la permission");
                return StatusCode(500, "Erreur lors de l'assignation de la permission");
            }
        }

        /// <summary>
        /// Retire une permission d'un rôle
        /// </summary>
        /// <param name="request">Objet contenant roleId et permissionId</param>
        [HttpPost("revoke")]
       // [Permission("Permission.Revoke")]
        public async Task<IActionResult> RevokePermissionFromRole([FromBody] AssignPermissionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var revoked = await _permissionService.RevokePermissionFromRoleAsync(request.RoleId, request.PermissionId);
                
                if (!revoked)
                {
                    return NotFound("Permission non trouvée pour ce rôle");
                }

                return Ok(new { message = "Permission retirée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la révocation de la permission");
                return StatusCode(500, "Erreur lors de la révocation de la permission");
            }
        }

        /// <summary>
        /// Assigne plusieurs permissions à un rôle en une seule opération
        /// </summary>
        [HttpPost("assign-bulk")]
      //  [Permission("Permission.Assign")]
        public async Task<IActionResult> AssignMultiplePermissions([FromBody] AssignMultiplePermissionsRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                int successCount = 0;
                var errors = new List<string>();

                foreach (var permissionId in request.PermissionIds)
                {
                    var assigned = await _permissionService.AssignPermissionToRoleAsync(request.RoleId, permissionId);
                    if (assigned)
                    {
                        successCount++;
                    }
                    else
                    {
                        errors.Add($"Erreur pour la permission ID {permissionId}");
                    }
                }

                return Ok(new 
                { 
                    message = $"{successCount}/{request.PermissionIds.Count} permissions assignées avec succès",
                    successCount,
                    totalRequested = request.PermissionIds.Count,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'assignation multiple de permissions");
                return StatusCode(500, "Erreur lors de l'assignation multiple de permissions");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // VÉRIFICATION DE PERMISSIONS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vérifie si l'utilisateur connecté possède une permission spécifique
        /// </summary>
        [HttpGet("check/{permissionName}")]
        [Authorize]
        public async Task<ActionResult<bool>> CheckUserHasPermission(string permissionName)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var hasPermission = await _permissionService.UserHasPermissionAsync(userId, permissionName);
                return Ok(new { permissionName, hasPermission });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de la permission {PermissionName}", permissionName);
                return StatusCode(500, "Erreur lors de la vérification de la permission");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // DTOs (Data Transfer Objects)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Request pour assigner/retirer une permission à/d'un rôle
    /// </summary>
    public class AssignPermissionRequest
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
    }

    /// <summary>
    /// Request pour assigner plusieurs permissions à un rôle
    /// </summary>
    public class AssignMultiplePermissionsRequest
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new();
    }
}

