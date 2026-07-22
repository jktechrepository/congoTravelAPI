using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Destination;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DestinationController : ControllerBase
    {
        private readonly IDestinationRepository _destinationRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly CongoTravel.Data.CongoTravelDbContext _context;
        private readonly ILogger<DestinationController> _logger;

        public DestinationController(
            IDestinationRepository destinationRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            CongoTravel.Data.CongoTravelDbContext context,
            ILogger<DestinationController> logger)
        {
            _destinationRepository = destinationRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _context = context;
            _logger = logger;
        }

        // GET: api/Destination
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DestinationResponseDto>>> GetDestinations()
        {
            try
            {
                var destinations = await _destinationRepository.GetAllAsync();
                var response = MapToDestinationResponseDtoList(destinations);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de toutes les destinations");
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // GET: api/Destination/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<DestinationResponseDto>>> GetDestinationsPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] int? idSociete = null)
        {
            try
            {
                if (idSociete.HasValue)
                {
                    var forbidden = ForbidIfNotAllowedSociete(idSociete.Value);
                    if (forbidden != null)
                        return forbidden;
                }

                var result = await _destinationRepository.GetPagedAsync(request, idSociete);
                var mappedData = MapToDestinationResponseDtoList(result.Data);
                var mappedResult = new PagedResult<DestinationResponseDto>(
                    mappedData.ToList(),
                    result.TotalCount,
                    result.PageNumber,
                    result.PageSize
                );
                return Ok(mappedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des destinations");
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // GET: api/Destination/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DestinationResponseDto>> GetDestination(int id)
        {
            try
            {
                var destination = await _destinationRepository.GetByIdAsync(id);
                if (destination == null)
                {
                    return NotFound(new { Message = $"Destination avec l'ID {id} non trouvée" });
                }

                var forbidden = ForbidIfNotAllowedSociete(destination.IdSociete);
                if (forbidden != null)
                    return forbidden;

                var response = MapToDestinationResponseDto(destination);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la destination {Id}", id);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // GET: api/Destination/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        [ProducesResponseType(typeof(IEnumerable<DestinationResponseDto>), 200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<DestinationResponseDto>>> GetDestinationsBySociete(int idSociete)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var destinations = await _destinationRepository.GetBySocieteAsync(idSociete);
                var response = MapToDestinationResponseDtoList(destinations);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des destinations de la société {IdSociete}", idSociete);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // GET: api/Destination/societe/{idSociete}/paged
        [HttpGet("societe/{idSociete}/paged")]
        [ProducesResponseType(typeof(PagedResult<DestinationResponseDto>), 200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<PagedResult<DestinationResponseDto>>> GetDestinationsBySocietePaged(int idSociete, [FromQuery] PagedRequest request)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var result = await _destinationRepository.GetBySocietePagedAsync(idSociete, request);
                var mappedData = MapToDestinationResponseDtoList(result.Data);
                var mappedResult = new PagedResult<DestinationResponseDto>(
                    mappedData.ToList(),
                    result.TotalCount,
                    result.PageNumber,
                    result.PageSize
                );
                return Ok(mappedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des destinations de la société {IdSociete}", idSociete);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // GET: api/Destination/search
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<DestinationResponseDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<DestinationResponseDto>>> SearchDestinations(
            [FromQuery] string villeDepart,
            [FromQuery] string villeArrivee,
            [FromQuery] int? idSociete = null)
        {
            try
            {
                if (string.IsNullOrEmpty(villeDepart) || string.IsNullOrEmpty(villeArrivee))
                {
                    return BadRequest(new { Message = "Les villes de départ et d'arrivée sont obligatoires" });
                }

                if (!_currentUserService.IsSuperAdmin)
                {
                    if (!idSociete.HasValue)
                    {
                        return BadRequest(new { Message = "Le paramètre idSociete est obligatoire" });
                    }

                    var forbidden = ForbidIfNotAllowedSociete(idSociete.Value);
                    if (forbidden != null)
                        return forbidden;
                }
                else if (!idSociete.HasValue)
                {
                    return BadRequest(new { Message = "Le paramètre idSociete est obligatoire" });
                }

                var destinations = await _destinationRepository.GetByVillesAsync(idSociete.Value, villeDepart, villeArrivee);
                var response = MapToDestinationResponseDtoList(destinations);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche des destinations entre {VilleDepart} et {VilleArrivee}", villeDepart, villeArrivee);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // POST: api/Destination
        [HttpPost]
        [ProducesResponseType(typeof(DestinationResponseDto), 201)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<DestinationResponseDto>> CreateDestination([FromBody] CreateDestinationDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var forbidden = ForbidIfNotAllowedSociete(createDto.IdSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                var destination = new Destination
                {
                    VilleDepart = createDto.VilleDepart,
                    VilleArrivee = createDto.VilleArrivee,
                    Montant = createDto.Montant,
                    IdSociete = createDto.IdSociete,
                    JourDepart = createDto.JourDepart,
                    Statut = true
                };

                var createdDestination = await _destinationRepository.CreateAsync(destination);
                var response = MapToDestinationResponseDto(createdDestination);

                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(createdDestination, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création destination");

                return CreatedAtAction(nameof(GetDestination), new { id = createdDestination.IdDestination }, response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflit base lors de la création de la destination");
                return Conflict(new { message = "Une destination avec ces villes existe déjà pour cette société." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la destination");
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // PUT: api/Destination/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(DestinationResponseDto), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<DestinationResponseDto>> UpdateDestination(int id, [FromBody] UpdateDestinationDto updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDestination = await _destinationRepository.GetByIdAsync(id);
            if (existingDestination == null)
            {
                return NotFound(new { Message = $"Destination avec l'ID {id} non trouvée" });
            }

            var forbidden = ForbidIfNotAllowedSociete(existingDestination.IdSociete);
            if (forbidden != null)
                return forbidden;

            try
            {
                if (!string.IsNullOrEmpty(updateDto.VilleDepart))
                    existingDestination.VilleDepart = updateDto.VilleDepart;

                if (!string.IsNullOrEmpty(updateDto.VilleArrivee))
                    existingDestination.VilleArrivee = updateDto.VilleArrivee;

                if (updateDto.Montant.HasValue)
                    existingDestination.Montant = updateDto.Montant.Value;

                if (updateDto.Statut.HasValue)
                    existingDestination.Statut = updateDto.Statut.Value;

                if (updateDto.JourDepart != null)
                    existingDestination.JourDepart = updateDto.JourDepart;

                var updatedDestination = await _destinationRepository.UpdateAsync(existingDestination);
                var response = MapToDestinationResponseDto(updatedDestination);

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(updatedDestination, updatedDestination, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Mise à jour destination");

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Conflit base lors de la mise à jour de la destination {Id}", id);
                return Conflict(new { message = "Une destination avec ces villes existe déjà pour cette société." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la destination {Id}", id);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // DELETE: api/Destination/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(403)]
        public async Task<ActionResult> DeleteDestination(int id)
        {
            try
            {
                var destination = await _destinationRepository.GetByIdAsync(id);
                if (destination == null)
                {
                    return NotFound(new { Message = $"Destination avec l'ID {id} non trouvée" });
                }

                var forbidden = ForbidIfNotAllowedSociete(destination.IdSociete);
                if (forbidden != null)
                    return forbidden;

                var success = await _destinationRepository.DeleteAsync(id);
                if (!success)
                {
                    return StatusCode(500, new { Message = "Échec de la suppression de la destination" });
                }

                var ctx = this.GetAuditContext();
                await _auditService.LogDeleteAsync(destination, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Suppression destination");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la destination {Id}", id);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
            }
        }

        // PUT: api/Destination/{id}/toggle-statut
        [HttpPut("{id}/toggle-statut")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        public async Task<ActionResult> ToggleDestinationStatut(int id)
        {
            try
            {
                var existing = await _destinationRepository.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { Message = $"Destination avec l'ID {id} non trouvée" });
                }

                var forbidden = ForbidIfNotAllowedSociete(existing.IdSociete);
                if (forbidden != null)
                    return forbidden;

                var success = await _destinationRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { Message = $"Destination avec l'ID {id} non trouvée" });
                }

                var destination = await _destinationRepository.GetByIdAsync(id);

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(destination, destination, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Basculement statut destination");

                return Ok(new { Message = "Statut de la destination basculé avec succès", Statut = destination?.Statut });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du basculement du statut de la destination {Id}", id);
                return StatusCode(500, new { Message = "Une erreur interne est survenue" });
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
                    message = "Accès interdit: vous ne pouvez gérer que les destinations de votre société."
                });
            }

            return null;
        }

        #region Mapping Methods

        private static IEnumerable<DestinationResponseDto> MapToDestinationResponseDtoList(IEnumerable<Destination> destinations)
        {
            return destinations.Select(MapToDestinationResponseDto);
        }

        private static DestinationResponseDto MapToDestinationResponseDto(Destination destination)
        {
            return new DestinationResponseDto
            {
                IdDestination = destination.IdDestination,
                VilleDepart = destination.VilleDepart,
                VilleArrivee = destination.VilleArrivee,
                Montant = destination.Montant,
                JourDepart = destination.JourDepart,
                Statut = destination.Statut,
                DateCreation = destination.DateCreation,
                DateModification = destination.DateModification,
                IdSociete = destination.IdSociete,
                NomSociete = destination.Societe?.Nom,
                DeviseSociete = destination.Societe?.Devise
            };
        }

        #endregion
    }
}
