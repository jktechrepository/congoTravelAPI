using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using CongoTravel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Token JWT requis
    public class VehiculeController : ControllerBase
    {
        private readonly IVehiculeRepository _vehiculeRepository;
        private readonly IVehiculePhotoService _vehiculePhotoService;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ISiegeService _siegeService;
        private readonly IMapper _mapper;

        public VehiculeController(
            IVehiculeRepository vehiculeRepository,
            IVehiculePhotoService vehiculePhotoService,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ISiegeService siegeService,
            IMapper mapper)
        {
            _vehiculeRepository = vehiculeRepository;
            _vehiculePhotoService = vehiculePhotoService;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _siegeService = siegeService;
            _mapper = mapper;
        }

        // GET: api/Bus
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehicules()
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetAllAsync();
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules", error = ex.Message });
            }
        }

        // GET: api/Bus/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<VehiculeResponseDto>>> GetVehiculesPaged([FromQuery] PagedRequest request)
        {
            try
            {
                var result = await _vehiculeRepository.GetPagedAsync(request);
                var dtos = await MapWithRepartitionAsync(result.Data);

                return Ok(new PagedResult<VehiculeResponseDto>(dtos, result.TotalCount, result.PageNumber, result.PageSize));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération paginée des véhicules", error = ex.Message });
            }
        }

        // GET: api/Bus/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VehiculeResponseDto>> GetVehicule(int id)
        {
            try
            {
                var vehicule = await _vehiculeRepository.GetByIdAsync(id);
                if (vehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                var dto = await MapWithRepartitionAsync(vehicule);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération du véhicule", error = ex.Message });
            }
        }

        // GET: api/Bus/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehiculesBySociete(int idSociete)
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetBySocieteAsync(idSociete);
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules de la société", error = ex.Message });
            }
        }

        // GET: api/Bus/societe/{idSociete}/paged
        [HttpGet("societe/{idSociete}/paged")]
        public async Task<ActionResult<PagedResult<VehiculeResponseDto>>> GetVehiculesBySocietePaged(int idSociete, [FromQuery] PagedRequest request)
        {
            try
            {
                var result = await _vehiculeRepository.GetBySocietePagedAsync(idSociete, request);
                var dtos = await MapWithRepartitionAsync(result.Data);

                return Ok(new PagedResult<VehiculeResponseDto>(dtos, result.TotalCount, result.PageNumber, result.PageSize));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération paginée des véhicules de la société", error = ex.Message });
            }
        }

        // GET: api/Bus/type/{typeVehicule}
        [HttpGet("type/{typeVehicule}")]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehiculesByType(int typeVehicule)
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetByTypeVehiculeAsync(typeVehicule);
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules par type", error = ex.Message });
            }
        }

        // GET: api/Bus/societe/{idSociete}/type/{typeVehicule}
        [HttpGet("societe/{idSociete}/type/{typeVehicule}")]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehiculesBySocieteAndType(int idSociete, int typeVehicule)
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetBySocieteAndTypeAsync(idSociete, typeVehicule);
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules par société et type", error = ex.Message });
            }
        }

        // GET: api/Bus/alias/{aliasVehicule}/societe/{idSociete}
        // GET: api/Bus/numero/{aliasVehicule}/societe/{idSociete} — même comportement (alias texte), chemin historique
        [HttpGet("alias/{aliasVehicule}/societe/{idSociete}")]
        [HttpGet("numero/{aliasVehicule}/societe/{idSociete}")]
        public async Task<ActionResult<VehiculeResponseDto>> GetVehiculeByAlias(string aliasVehicule, int idSociete)
        {
            try
            {
                var vehicule = await _vehiculeRepository.GetByAliasVehiculeAsync(aliasVehicule, idSociete);
                if (vehicule == null)
                    return NotFound(new { message = $"Véhicule avec alias {aliasVehicule} non trouvé pour cette société" });

                var dto = await MapWithRepartitionAsync(vehicule);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la recherche du véhicule par alias", error = ex.Message });
            }
        }

        // GET: api/Bus/statut/{statut}
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehiculesByStatut(bool statut)
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetByStatutAsync(statut);
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules par statut", error = ex.Message });
            }
        }

        // GET: api/Bus/marque/{marque}
        [HttpGet("marque/{marque}")]
        public async Task<ActionResult<IEnumerable<VehiculeResponseDto>>> GetVehiculesByMarque(string marque)
        {
            try
            {
                var vehicules = await _vehiculeRepository.GetByMarqueAsync(marque);
                var dtos = await MapWithRepartitionAsync(vehicules);
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des véhicules par marque", error = ex.Message });
            }
        }

        // POST: api/Bus
        [HttpPost]
        public async Task<ActionResult<VehiculeResponseDto>> CreateVehicule([FromBody] CreateVehiculeDto createVehiculeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var vehicule = _mapper.Map<Vehicule>(createVehiculeDto);
                if (createVehiculeDto.RepartitionCategorieSieges is { Count: > 0 })
                    vehicule.NombreSiege = createVehiculeDto.RepartitionCategorieSieges.Sum(x => x.NombreSiegeParCategorie);
                var createdVehicule = await _vehiculeRepository.CreateAsync(vehicule);
                await _siegeService.EnsureSeatsForVehiculeWithCategorieDistributionAsync(
                    createdVehicule.IdVehicule,
                    createVehiculeDto.RepartitionCategorieSieges?
                        .Select(x => (x.IdCategorieSiege, x.NombreSiegeParCategorie))
                        .ToList());

                await _vehiculePhotoService.AddPhotosOnCreateAsync(
                    createdVehicule.IdVehicule,
                    createVehiculeDto.ResolvePhotosForPersistence());

                var reloadedVehicule = await _vehiculeRepository.GetByIdAsync(createdVehicule.IdVehicule) ?? createdVehicule;

                // Audit log
                await _auditService.LogCreateAsync(
                    reloadedVehicule,
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    "Création d'un nouveau véhicule"
                );

                var dto = await MapWithRepartitionAsync(reloadedVehicule);
                return CreatedAtAction(nameof(GetVehicule), new { id = reloadedVehicule.IdVehicule }, dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la création du véhicule", error = ex.Message });
            }
        }

        // PUT: api/Bus/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<VehiculeResponseDto>> UpdateVehicule(int id, [FromBody] UpdateVehiculeDto updateVehiculeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != updateVehiculeDto.IdVehicule)
                    return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête" });

                var existingVehicule = await _vehiculeRepository.GetByIdAsync(id);
                if (existingVehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                var vehicule = _mapper.Map<Vehicule>(updateVehiculeDto);
                if (updateVehiculeDto.RepartitionCategorieSieges is { Count: > 0 })
                    vehicule.NombreSiege = updateVehiculeDto.RepartitionCategorieSieges.Sum(x => x.NombreSiegeParCategorie);
                var updatedVehicule = await _vehiculeRepository.UpdateAsync(vehicule);

                if (updatedVehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                await _siegeService.EnsureSeatsForVehiculeWithCategorieDistributionAsync(
                    updatedVehicule.IdVehicule,
                    updateVehiculeDto.RepartitionCategorieSieges?
                        .Select(x => (x.IdCategorieSiege, x.NombreSiegeParCategorie))
                        .ToList());

                await _vehiculePhotoService.ReplaceAllPhotosOnUpdateAsync(
                    updatedVehicule.IdVehicule,
                    updateVehiculeDto.ResolvePhotosForPersistence());

                var reloadedAfterUpdate = await _vehiculeRepository.GetByIdAsync(updatedVehicule.IdVehicule) ?? updatedVehicule;

                // Audit log
                await _auditService.LogUpdateAsync(
                    existingVehicule,
                    reloadedAfterUpdate,
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    "Mise à jour d'un véhicule"
                );

                var dto = await MapWithRepartitionAsync(reloadedAfterUpdate);
                return Ok(dto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour du véhicule", error = ex.Message });
            }
        }

        // PUT: api/Bus/{id}/toggle-statut
        [HttpPut("{id}/toggle-statut")]
        public async Task<ActionResult<VehiculeResponseDto>> ToggleStatutVehicule(int id)
        {
            try
            {
                var vehicule = await _vehiculeRepository.GetByIdAsync(id);
                if (vehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                var oldStatut = vehicule.Statut;
                vehicule.Statut = !vehicule.Statut;
                vehicule.DateModification = DateTime.Now;

                var updatedVehicule = await _vehiculeRepository.UpdateAsync(vehicule);

                // Audit log
                await _auditService.LogUpdateAsync(
                    new { Statut = oldStatut },
                    new { Statut = updatedVehicule.Statut },
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Changement de statut du véhicule (ID: {id})"
                );

                var dto = await MapWithRepartitionAsync(updatedVehicule);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du changement de statut du véhicule", error = ex.Message });
            }
        }

        // DELETE: api/Bus/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteVehicule(int id)
        {
            try
            {
                var vehicule = await _vehiculeRepository.GetByIdAsync(id);
                if (vehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                var deleted = await _vehiculeRepository.DeleteAsync(id);
                if (!deleted)
                    return BadRequest(new { message = "Échec de la suppression du véhicule" });

                // Audit log
                await _auditService.LogDeleteAsync(
                    vehicule,
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    "Suppression d'un véhicule"
                );

                return Ok(new { message = "Véhicule supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression du véhicule", error = ex.Message });
            }
        }

        private async Task<VehiculeResponseDto> MapWithRepartitionAsync(Vehicule vehicule)
        {
            var dto = _mapper.Map<VehiculeResponseDto>(vehicule);
            await ApplyRepartitionAsync(new[] { dto });
            return dto;
        }

        private async Task<IReadOnlyList<VehiculeResponseDto>> MapWithRepartitionAsync(IEnumerable<Vehicule> vehicules)
        {
            var dtos = _mapper.Map<List<VehiculeResponseDto>>(vehicules);
            await ApplyRepartitionAsync(dtos);
            return dtos;
        }

        private async Task ApplyRepartitionAsync(IReadOnlyList<VehiculeResponseDto> dtos)
        {
            if (dtos.Count == 0)
                return;

            var repartitions = await _siegeService.GetActiveRepartitionByVehiculeIdsAsync(
                dtos.Select(d => d.IdVehicule).ToList());

            foreach (var dto in dtos)
            {
                dto.RepartitionCategorieSieges = repartitions.TryGetValue(dto.IdVehicule, out var repartition)
                    ? repartition
                    : new List<VehiculeCategorieSiegeRepartitionDto>();
            }
        }

        // GET: api/Vehicule/{id}/photos
        [HttpGet("{id}/photos")]
        public async Task<ActionResult<IEnumerable<PhotoVehiculeDto>>> GetVehiculePhotos(int id)
        {
            try
            {
                var vehicule = await _vehiculeRepository.GetByIdAsync(id);
                if (vehicule == null)
                    return NotFound(new { message = $"Véhicule avec ID {id} non trouvé" });

                var photos = await _vehiculePhotoService.GetByVehiculeIdAsync(id);
                return Ok(_mapper.Map<List<PhotoVehiculeDto>>(photos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des photos", error = ex.Message });
            }
        }

        // POST: api/Vehicule/{id}/photos
        [HttpPost("{id}/photos")]
        public async Task<ActionResult<PhotoVehiculeDto>> AddVehiculePhoto(int id, [FromBody] AddPhotoVehiculeDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var photo = await _vehiculePhotoService.AddPhotoAsync(id, dto);

                await _auditService.LogCreateAsync(
                    photo,
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Ajout d'une photo au véhicule (ID: {id})");

                return CreatedAtAction(nameof(GetVehiculePhotos), new { id }, _mapper.Map<PhotoVehiculeDto>(photo));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de l'ajout de la photo", error = ex.Message });
            }
        }

        // PUT: api/Vehicule/{id}/photos/{photoId}/ordre
        [HttpPut("{id}/photos/{photoId}/ordre")]
        public async Task<ActionResult<PhotoVehiculeDto>> UpdateVehiculePhotoOrdre(
            int id,
            int photoId,
            [FromBody] UpdatePhotoVehiculeOrdreDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var photo = await _vehiculePhotoService.UpdateOrdreAsync(id, photoId, dto.Ordre);
                if (photo == null)
                    return NotFound(new { message = $"Photo avec ID {photoId} non trouvée pour ce véhicule" });

                return Ok(_mapper.Map<PhotoVehiculeDto>(photo));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour de l'ordre", error = ex.Message });
            }
        }

        // DELETE: api/Vehicule/{id}/photos/{photoId}
        [HttpDelete("{id}/photos/{photoId}")]
        public async Task<ActionResult> DeleteVehiculePhoto(int id, int photoId)
        {
            try
            {
                var deleted = await _vehiculePhotoService.DeletePhotoAsync(id, photoId);
                if (!deleted)
                    return NotFound(new { message = $"Photo avec ID {photoId} non trouvée pour ce véhicule" });

                await _auditService.LogDeleteAsync(
                    new { IdVehicule = id, IdPhotoVehicule = photoId },
                    _currentUserService.UserId,
                    _currentUserService.UserName,
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Suppression d'une photo du véhicule (ID: {id})");

                return Ok(new { message = "Photo supprimée avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de la photo", error = ex.Message });
            }
        }

        // GET: api/Bus/count
        [HttpGet("count")]
        public async Task<ActionResult<int>> GetVehiculeCount()
        {
            try
            {
                var count = await _vehiculeRepository.CountAsync();
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du comptage des véhicules", error = ex.Message });
            }
        }

        // GET: api/Bus/societe/{idSociete}/count
        [HttpGet("societe/{idSociete}/count")]
        public async Task<ActionResult<int>> GetVehiculeCountBySociete(int idSociete)
        {
            try
            {
                var count = await _vehiculeRepository.CountBySocieteAsync(idSociete);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du comptage des véhicules de la société", error = ex.Message });
            }
        }

        // GET: api/Bus/type/{typeVehicule}/count
        [HttpGet("type/{typeVehicule}/count")]
        public async Task<ActionResult<int>> GetVehiculeCountByType(int typeVehicule)
        {
            try
            {
                var count = await _vehiculeRepository.CountByTypeVehiculeAsync(typeVehicule);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du comptage des véhicules par type", error = ex.Message });
            }
        }
    }
}
