using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.PlainteClient;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlainteClientController : ControllerBase
    {
        private readonly IPlainteClientRepository _plainteRepository;
        private readonly IPlainteClientNotificationService _notificationService;
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<PlainteClientController> _logger;

        public PlainteClientController(
            IPlainteClientRepository plainteRepository,
            IPlainteClientNotificationService notificationService,
            CongoTravelDbContext context,
            ILogger<PlainteClientController> logger)
        {
            _plainteRepository = plainteRepository;
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        // GET: api/PlainteClient
        [HttpGet]
        [Permission("PlainteClient.ReadAll")]
        public async Task<ActionResult<IEnumerable<PlainteClient>>> GetAll()
        {
            var plaintes = await _plainteRepository.GetAllAsync();
            return Ok(plaintes);
        }

        // GET: api/PlainteClient/paged
        [HttpGet("paged")]
        [Permission("PlainteClient.ReadAll")]
        public async Task<ActionResult<PagedResult<PlainteClient>>> GetPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] string? statut = null,
            [FromQuery] string? priorite = null,
            [FromQuery] int? idAgent = null,
            [FromQuery] int? idClient = null)
        {
            var result = await _plainteRepository.GetPagedAsync(
                request, statut, priorite, idAgent, idClient);
            return Ok(result);
        }

        // GET: api/PlainteClient/en-attente
        [HttpGet("en-attente")]
        [Permission("PlainteClient.ReadAll")]
        public async Task<ActionResult<IEnumerable<PlainteClient>>> GetEnAttente()
        {
            var plaintes = await _plainteRepository.GetEnAttenteAsync();
            return Ok(plaintes);
        }

        // GET: api/PlainteClient/assignees/{idAgent}
        [HttpGet("assignees/{idAgent}")]
        [Permission("PlainteClient.ReadAll")]
        public async Task<ActionResult<IEnumerable<PlainteClient>>> GetByAgent(int idAgent)
        {
            var plaintes = await _plainteRepository.GetByAgentAsync(idAgent);
            return Ok(plaintes);
        }

        // GET: api/PlainteClient/mes-plaintes
        [HttpGet("mes-plaintes")]
        public async Task<ActionResult<IEnumerable<PlainteClient>>> GetMesPlaintes()
        {
            // Récupérer l'ID du client depuis le token JWT
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Utilisateur non authentifié" });
            }

            var utilisateur = await _context.Utilisateurs.FindAsync(userId);
            if (utilisateur == null || !utilisateur.IdClient.HasValue)
            {
                return NotFound(new { message = "Client non trouvé pour cet utilisateur" });
            }

            var plaintes = await _plainteRepository.GetByClientAsync(utilisateur.IdClient.Value);
            return Ok(plaintes);
        }

        // GET: api/PlainteClient/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlainteClient>> GetById(int id)
        {
            var plainte = await _plainteRepository.GetByIdAsync(id);
            if (plainte == null)
                return NotFound(new { message = $"Plainte {id} introuvable" });

            // Vérifier que l'utilisateur a le droit de voir cette plainte
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                var utilisateur = await _context.Utilisateurs.FindAsync(userId);
                // Vérifier si l'utilisateur a un rôle autorisé (Admin, Agent, Technicien, Gerant)
                var isAuthorized = User.IsInRole("Super-Admin") || User.IsInRole("Admin") || 
                                  User.IsInRole("Agent") || User.IsInRole("Technicien") || 
                                  User.IsInRole("Gerant");
                
                // Si ce n'est pas un utilisateur autorisé, vérifier que c'est son client
                if (!isAuthorized && (utilisateur == null || !utilisateur.IdClient.HasValue || utilisateur.IdClient.Value != plainte.IdClient))
                {
                    return Forbid();
                }
            }

            return Ok(plainte);
        }

        // POST: api/PlainteClient
        [HttpPost]
        [Permission("PlainteClient.Create")]
        public async Task<ActionResult<PlainteClient>> Create([FromBody] CreatePlainteClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
            // Récupérer l'utilisateur courant depuis le token JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                int? userId = null;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                var plainte = new PlainteClient
                {
                    IdClient = dto.IdClient,
                    Titre = dto.Titre,
                    Description = dto.Description,
                    TypePanne = dto.TypePanne,
                    NiveauImportance = dto.NiveauImportance,
                    RisquesPrincipaux = dto.RisquesPrincipaux,
                    Priorite = dto.Priorite,
                    EstUrgente = dto.EstUrgente,
                    IdUtilisateurCreateur = userId
                };

                var created = await _plainteRepository.CreateAsync(plainte);

                // Notifier l'équipe d'intervention (en arrière-plan)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.NotifierEquipeInterventionAsync(created);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Erreur lors de la notification de l'équipe pour la plainte {PlainteId}", created.IdPlainte);
                    }
                });

                return CreatedAtAction(nameof(GetById), new { id = created.IdPlainte }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création de la plainte");
                return StatusCode(500, new { message = "Erreur lors de la création de la plainte", error = ex.Message });
            }
        }

        // PUT: api/PlainteClient/5
        [HttpPut("{id}")]
        [Permission("PlainteClient.Update")]
        public async Task<ActionResult<PlainteClient>> Update(int id, [FromBody] UpdatePlainteClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existing = await _plainteRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Plainte {id} introuvable" });

                // Mettre à jour uniquement les champs fournis
                if (dto.Titre != null)
                    existing.Titre = dto.Titre;
                if (dto.Description != null)
                    existing.Description = dto.Description;
                if (dto.TypePanne != null)
                    existing.TypePanne = dto.TypePanne;
                if (dto.NiveauImportance != null)
                    existing.NiveauImportance = dto.NiveauImportance;
                if (dto.RisquesPrincipaux != null)
                    existing.RisquesPrincipaux = dto.RisquesPrincipaux;
                if (dto.StatutPlainte != null)
                    existing.StatutPlainte = dto.StatutPlainte;
                if (dto.Priorite != null)
                    existing.Priorite = dto.Priorite;
                if (dto.IdAgentAssigné.HasValue)
                    existing.IdAgentAssigné = dto.IdAgentAssigné;
                if (dto.CommentaireResolution != null)
                    existing.CommentaireResolution = dto.CommentaireResolution;
                if (dto.DateResolution.HasValue)
                    existing.DateResolution = dto.DateResolution;
                if (dto.EstUrgente.HasValue)
                    existing.EstUrgente = dto.EstUrgente.Value;

                var updated = await _plainteRepository.UpdateAsync(existing);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise à jour de la plainte {PlainteId}", id);
                return StatusCode(500, new { message = "Erreur lors de la mise à jour", error = ex.Message });
            }
        }

        // PATCH: api/PlainteClient/5/assigner
        [HttpPatch("{id}/assigner")]
        [Permission("PlainteClient.Update")]
        public async Task<ActionResult<PlainteClient>> Assigner(int id, [FromBody] AssignerPlainteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var plainte = await _plainteRepository.GetByIdAsync(id);
                if (plainte == null)
                    return NotFound(new { message = $"Plainte {id} introuvable" });

                // Vérifier que l'agent existe
                var agent = await _context.Agents.FindAsync(dto.IdAgentAssigné);
                if (agent == null)
                    return NotFound(new { message = $"Agent {dto.IdAgentAssigné} introuvable" });

                plainte.IdAgentAssigné = dto.IdAgentAssigné;
                plainte.StatutPlainte = "En cours";
                plainte.DateDerniereModification = DateTime.Now;

                var updated = await _plainteRepository.UpdateAsync(plainte);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'assignation de la plainte {PlainteId}", id);
                return StatusCode(500, new { message = "Erreur lors de l'assignation", error = ex.Message });
            }
        }

        // PATCH: api/PlainteClient/5/statut
        [HttpPatch("{id}/statut")]
        [Permission("PlainteClient.Update")]
        public async Task<ActionResult<PlainteClient>> ChangerStatut(int id, [FromBody] ChangerStatutPlainteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var plainte = await _plainteRepository.GetByIdAsync(id);
                if (plainte == null)
                    return NotFound(new { message = $"Plainte {id} introuvable" });

                plainte.StatutPlainte = dto.StatutPlainte;
                plainte.DateDerniereModification = DateTime.Now;

                var updated = await _plainteRepository.UpdateAsync(plainte);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du changement de statut de la plainte {PlainteId}", id);
                return StatusCode(500, new { message = "Erreur lors du changement de statut", error = ex.Message });
            }
        }

        // PATCH: api/PlainteClient/5/resoudre
        [HttpPatch("{id}/resoudre")]
        [Permission("PlainteClient.Update")]
        public async Task<ActionResult<PlainteClient>> Resoudre(int id, [FromBody] ResoudrePlainteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var plainte = await _plainteRepository.GetByIdAsync(id);
                if (plainte == null)
                    return NotFound(new { message = $"Plainte {id} introuvable" });

                plainte.StatutPlainte = "Résolu";
                plainte.CommentaireResolution = dto.CommentaireResolution;
                plainte.DateResolution = DateTime.Now;
                plainte.DateDerniereModification = DateTime.Now;

                var updated = await _plainteRepository.UpdateAsync(plainte);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la résolution de la plainte {PlainteId}", id);
                return StatusCode(500, new { message = "Erreur lors de la résolution", error = ex.Message });
            }
        }

        // DELETE: api/PlainteClient/5
        [HttpDelete("{id}")]
        [Permission("PlainteClient.Delete")]
        public async Task<ActionResult<object>> Delete(int id)
        {
            try
            {
                var deleted = await _plainteRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Plainte {id} introuvable" });

                return Ok(new 
                { 
                    message = "Plainte désactivée avec succès (soft delete)",
                    idPlainte = id,
                    note = "La plainte a été désactivée. Les données sont conservées pour l'historique."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression de la plainte {PlainteId}", id);
                return StatusCode(500, new { message = "Erreur lors de la suppression", error = ex.Message });
            }
        }
    }
}

