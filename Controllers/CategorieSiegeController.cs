using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategorieSiegeController : ControllerBase
    {
        private readonly ICategorieSiegeRepository _repository;
        private readonly ICurrentUserService _currentUserService;

        public CategorieSiegeController(
            ICategorieSiegeRepository repository,
            ICurrentUserService currentUserService)
        {
            _repository = repository;
            _currentUserService = currentUserService;
        }

        /// <summary>Liste les catégories de siège d'une société (référentiel phase 1).</summary>
        [HttpGet("societe/{idSociete:int}")]
        [ProducesResponseType(typeof(IEnumerable<CategorieSiegeResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<CategorieSiegeResponseDto>>> GetBySociete(
            int idSociete,
            [FromQuery] bool actifsSeulement = false)
        {
            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != idSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: vous ne pouvez consulter que les catégories de votre société."
                });
            }

            var list = await _repository.GetBySocieteAsync(idSociete, actifsSeulement);
            var dtos = list.Select(c => new CategorieSiegeResponseDto
            {
                IdCategorieSiege = c.IdCategorieSiege,
                IdSociete = c.IdSociete,
                CodeCategorieSiege = c.CodeCategorieSiege,
                Libelle = c.Libelle,
                Statut = c.Statut
            });
            return Ok(dtos);
        }

        [HttpGet("{idCategorieSiege:int}")]
        [ProducesResponseType(typeof(CategorieSiegeResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategorieSiegeResponseDto>> GetById(int idCategorieSiege)
        {
            var item = await _repository.GetByIdAsync(idCategorieSiege);
            if (item == null)
                return NotFound(new { message = "Categorie de siege introuvable." });

            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != item.IdSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: cette catégorie n'appartient pas à votre société."
                });
            }

            return Ok(new CategorieSiegeResponseDto
            {
                IdCategorieSiege = item.IdCategorieSiege,
                IdSociete = item.IdSociete,
                CodeCategorieSiege = item.CodeCategorieSiege,
                Libelle = item.Libelle,
                Statut = item.Statut
            });
        }

        [HttpPost]
        [ProducesResponseType(typeof(CategorieSiegeResponseDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<CategorieSiegeResponseDto>> Create([FromBody] CreateCategorieSiegeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != dto.IdSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: vous ne pouvez créer des catégories que dans votre société."
                });
            }

            try
            {
                var created = await _repository.CreateAsync(new CategorieSiege
                {
                    IdSociete = dto.IdSociete,
                    CodeCategorieSiege = dto.CodeCategorieSiege,
                    Libelle = dto.Libelle,
                    Statut = dto.Statut
                });

                return CreatedAtAction(nameof(GetById), new { idCategorieSiege = created.IdCategorieSiege },
                    new CategorieSiegeResponseDto
                    {
                        IdCategorieSiege = created.IdCategorieSiege,
                        IdSociete = created.IdSociete,
                        CodeCategorieSiege = created.CodeCategorieSiege,
                        Libelle = created.Libelle,
                        Statut = created.Statut
                    });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{idCategorieSiege:int}")]
        [ProducesResponseType(typeof(CategorieSiegeResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategorieSiegeResponseDto>> Update(
            int idCategorieSiege,
            [FromBody] UpdateCategorieSiegeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (idCategorieSiege != dto.IdCategorieSiege)
                return BadRequest(new { message = "L'ID URL ne correspond pas à l'ID payload." });

            var existing = await _repository.GetByIdAsync(idCategorieSiege);
            if (existing == null)
                return NotFound(new { message = "Categorie de siege introuvable." });

            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != existing.IdSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: cette catégorie n'appartient pas à votre société."
                });
            }

            try
            {
                var updated = await _repository.UpdateAsync(new CategorieSiege
                {
                    IdCategorieSiege = dto.IdCategorieSiege,
                    CodeCategorieSiege = dto.CodeCategorieSiege,
                    Libelle = dto.Libelle,
                    Statut = dto.Statut
                });
                if (updated == null)
                    return NotFound(new { message = "Categorie de siege introuvable." });

                return Ok(new CategorieSiegeResponseDto
                {
                    IdCategorieSiege = updated.IdCategorieSiege,
                    IdSociete = updated.IdSociete,
                    CodeCategorieSiege = updated.CodeCategorieSiege,
                    Libelle = updated.Libelle,
                    Statut = updated.Statut
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{idCategorieSiege:int}/toggle-statut")]
        [ProducesResponseType(typeof(CategorieSiegeResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CategorieSiegeResponseDto>> ToggleStatut(int idCategorieSiege)
        {
            var existing = await _repository.GetByIdAsync(idCategorieSiege);
            if (existing == null)
                return NotFound(new { message = "Categorie de siege introuvable." });

            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != existing.IdSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: cette catégorie n'appartient pas à votre société."
                });
            }

            var updated = await _repository.ToggleStatutAsync(idCategorieSiege);
            if (updated == null)
                return NotFound(new { message = "Categorie de siege introuvable." });

            return Ok(new CategorieSiegeResponseDto
            {
                IdCategorieSiege = updated.IdCategorieSiege,
                IdSociete = updated.IdSociete,
                CodeCategorieSiege = updated.CodeCategorieSiege,
                Libelle = updated.Libelle,
                Statut = updated.Statut
            });
        }

        [HttpDelete("{idCategorieSiege:int}")]
        [Permission("CategorieSiege.Delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> Delete(int idCategorieSiege)
        {
            var existing = await _repository.GetByIdAsync(idCategorieSiege);
            if (existing == null)
                return NotFound(new { message = "Categorie de siege introuvable." });

            if (!_currentUserService.IsSuperAdmin &&
                (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != existing.IdSociete))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: cette catégorie n'appartient pas à votre société."
                });
            }

            var deleted = await _repository.DeleteAsync(idCategorieSiege);
            if (!deleted)
                return NotFound(new { message = "Categorie de siege introuvable." });

            return Ok(new { message = "Categorie de siege supprimée avec succès." });
        }
    }
}
