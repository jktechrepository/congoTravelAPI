using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Gestion des rôles - Token JWT requis
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;

        public RoleController(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        // GET: api/Role
        /// <summary>
        /// Récupère tous les rôles actifs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetAllRoles()
        {
            var roles = await _roleRepository.GetAllAsync();
            return Ok(roles);
        }

        // GET: api/Role/nomRole/{nomRole}
        [HttpGet("nomRole/{nomRole}")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles(string nomRole)
        {
            var roles = await _roleRepository.GetAllAsync(nomRole);
            return Ok(roles);
        }

        // GET: api/Role/5
        [HttpGet("{id}")] 
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }

        // GET: api/Role/nom/Admin
        [HttpGet("nom/{nom}")]
        public async Task<ActionResult<Role>> GetRoleByNom(string nom)
        {
            var role = await _roleRepository.GetByNomAsync(nom);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }

        // GET: api/Role/exists/5
        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> RoleExists(int id)
        {
            var exists = await _roleRepository.ExistsAsync(id);
            return Ok(exists);
        }

        // POST: api/Role
        [HttpPost]
        [Permission("Role.Create")]
        public async Task<ActionResult<Role>> CreateRole(Role role)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdRole = await _roleRepository.CreateAsync(role);
            return CreatedAtAction(nameof(GetRole), new { id = createdRole.IdRole }, createdRole);
        }

        // PUT: api/Role/5
        [HttpPut("{id}")]
        [Permission("Role.Update")]
        public async Task<ActionResult<Role>> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
        {
            if (id != dto.IdRole)
            {
                return BadRequest(new { message = "L'ID ne correspond pas" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _roleRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Rôle non trouvé" });
            }

            existing.Nom = dto.Nom;
            existing.Description = dto.Description;
            existing.Niveau = dto.Niveau;

            var updated = await _roleRepository.UpdateAsync(existing);
            return Ok(updated);
        }

        // DELETE: api/Role/5
        [HttpDelete("{id}")]
        [Permission("Role.Delete")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var success = await _roleRepository.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Role/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        [Permission("Role.Update")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _roleRepository.ToggleStatutAsync(id);
                if (!success)
                    return NotFound(new { message = "Rôle non trouvé" });

                var role = await _roleRepository.GetByIdAsync(id);
                return Ok(new { 
                    message = "Statut modifié avec succès",
                    nouveauStatut = role != null,
                    role = role
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur", error = ex.Message });
            }
        }
    }
}
