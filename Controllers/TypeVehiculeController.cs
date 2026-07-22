using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using AutoMapper;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TypeVehiculeController : ControllerBase
    {
        private readonly ITypeVehiculeRepository _typeVehiculeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<TypeVehiculeController> _logger;

        public TypeVehiculeController(
            ITypeVehiculeRepository typeVehiculeRepository,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<TypeVehiculeController> logger)
        {
            _typeVehiculeRepository = typeVehiculeRepository;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TypeVehiculeResponseDto>>> GetAll()
        {
            try
            {
                var items = await _typeVehiculeRepository.GetAllAsync();
                var dtos = _mapper.Map<IEnumerable<TypeVehiculeResponseDto>>(items);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les types de véhicule");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpGet("societe/{idSociete:int}")]
        [ProducesResponseType(typeof(IEnumerable<TypeVehiculeResponseDto>), 200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<TypeVehiculeResponseDto>>> GetBySociete(int idSociete)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var items = await _typeVehiculeRepository.GetBySocieteAsync(idSociete);
                var dtos = _mapper.Map<IEnumerable<TypeVehiculeResponseDto>>(items);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de véhicule pour la société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TypeVehiculeResponseDto>> GetById(int id)
        {
            try
            {
                var entity = await _typeVehiculeRepository.GetByIdAsync(id);
                if (entity == null)
                    return NotFound(new { message = $"Type de véhicule avec l'ID {id} non trouvé" });

                var forbidden = ForbidIfNotAllowedSociete(entity.IdSociete);
                if (forbidden != null)
                    return forbidden;

                var dto = _mapper.Map<TypeVehiculeResponseDto>(entity);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de véhicule {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpPost("paged")]
        public async Task<ActionResult<PagedResult<TypeVehiculeResponseDto>>> GetPaged(
            [FromBody] PagedRequest request,
            [FromQuery] int? idSociete = null)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (idSociete.HasValue)
                {
                    var forbidden = ForbidIfNotAllowedSociete(idSociete.Value);
                    if (forbidden != null)
                        return forbidden;
                }

                var pagedResult = await _typeVehiculeRepository.GetPagedAsync(request, idSociete);
                var pagedDtos = _mapper.Map<PagedResult<TypeVehiculeResponseDto>>(pagedResult);
                return Ok(pagedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des types de véhicule");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<TypeVehiculeResponseDto>>> GetByStatut(bool statut)
        {
            try
            {
                var items = await _typeVehiculeRepository.GetByStatutAsync(statut);
                var dtos = _mapper.Map<IEnumerable<TypeVehiculeResponseDto>>(items);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de véhicule avec statut {statut}", statut);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(TypeVehiculeResponseDto), 201)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<TypeVehiculeResponseDto>> Create([FromBody] CreateTypeVehiculeDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var forbidden = ForbidIfNotAllowedSociete(createDto.IdSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var entity = _mapper.Map<TypeVehicule>(createDto);
                var created = await _typeVehiculeRepository.CreateAsync(entity);
                var resultDto = _mapper.Map<TypeVehiculeResponseDto>(created);

                return CreatedAtAction(nameof(GetById), new { id = resultDto.IdTypeVehicule }, resultDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflit base lors de la création du type de véhicule");
                return Conflict(new { message = "Un type de véhicule avec ce libellé existe déjà pour cette société." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de véhicule");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(TypeVehiculeResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<TypeVehiculeResponseDto>> Update(int id, [FromBody] UpdateTypeVehiculeDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != updateDto.IdTypeVehicule)
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });

            var existing = await _typeVehiculeRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Type de véhicule avec l'ID {id} non trouvé" });

            var forbidden = ForbidIfNotAllowedSociete(existing.IdSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var entity = _mapper.Map<TypeVehicule>(updateDto);
                entity.IdSociete = existing.IdSociete;
                var updated = await _typeVehiculeRepository.UpdateAsync(entity);

                if (updated == null)
                    return NotFound(new { message = $"Type de véhicule avec l'ID {id} non trouvé" });

                var resultDto = _mapper.Map<TypeVehiculeResponseDto>(updated);
                return Ok(resultDto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflit base lors de la mise à jour du type de véhicule {Id}", id);
                return Conflict(new { message = "Un type de véhicule avec ce libellé existe déjà pour cette société." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du type de véhicule {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult> Delete(int id)
        {
            var existing = await _typeVehiculeRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = $"Type de véhicule avec l'ID {id} non trouvé" });

            var forbidden = ForbidIfNotAllowedSociete(existing.IdSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var deleted = await _typeVehiculeRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Type de véhicule avec l'ID {id} non trouvé" });

                return Ok(new { message = "Type de véhicule supprimé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de véhicule {id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        private ActionResult? ForbidIfNotAllowedSociete(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return null;

            if (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != idSociete)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: vous ne pouvez gérer que les types de véhicule de votre société."
                });
            }

            return null;
        }
    }
}
