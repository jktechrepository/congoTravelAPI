using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using CongoTravel.Attributes;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CommunicationCampaignController : ControllerBase
    {
        private readonly ICommunicationCampaignRepository _campaignRepository;
        private readonly ICommunicationDispatchService _dispatchService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommunicationCampaignController> _logger;

        public CommunicationCampaignController(
            ICommunicationCampaignRepository campaignRepository,
            ICommunicationDispatchService dispatchService,
            IServiceScopeFactory scopeFactory,
            ILogger<CommunicationCampaignController> logger)
        {
            _campaignRepository = campaignRepository;
            _dispatchService = dispatchService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // GET: api/CommunicationCampaign
        [HttpGet]
        [Permission("CommunicationCampaign.ReadAll")]
        public async Task<ActionResult<IEnumerable<CommunicationCampaign>>> GetAll()
        {
            var campaigns = await _campaignRepository.GetAllAsync();
            return Ok(campaigns);
        }

        // GET: api/CommunicationCampaign/paged
        [HttpGet("paged")]
        [Permission("CommunicationCampaign.ReadAll")]
        public async Task<ActionResult<PagedResult<CommunicationCampaign>>> GetPaged([FromQuery] PagedRequest request)
        {
            var result = await _campaignRepository.GetPagedAsync(request);
            return Ok(result);
        }

        // GET: api/CommunicationCampaign/5
        [HttpGet("{id}")]
        [Permission("CommunicationCampaign.Read")]
        public async Task<ActionResult<CommunicationCampaign>> GetById(int id)
        {
            var campaign = await _campaignRepository.GetByIdAsync(id);
            if (campaign == null)
                return NotFound(new { message = $"Campagne {id} introuvable" });

            return Ok(campaign);
        }

        // POST: api/CommunicationCampaign
        [HttpPost]
        [Permission("CommunicationCampaign.Create")]
        public async Task<ActionResult<CommunicationCampaign>> Create([FromBody] CreateCommunicationCampaignDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Le corps de la requête est requis" });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Récupérer l'utilisateur courant depuis le token JWT
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new { message = "Utilisateur non authentifié" });
                }

                var campaign = new CommunicationCampaign
                {
                    Titre = dto.Titre,
                    Contenu = dto.Contenu,
                    TypeCampagne = dto.TypeCampagne,
                    IdSociete = dto.IdSociete,
                    IdUtilisateurCreateur = userId,
                    ActiverPush = dto.ActiverPush,
                    ActiverSms = dto.ActiverSms,
                    ActiverEmail = dto.ActiverEmail,
                    ActiverInApp = dto.ActiverInApp,
                    DateEnvoi = dto.DateEnvoi
                };

                // Sérialiser les critères de ciblage en JSON
                if (dto.CriteresCiblage != null)
                {
                    campaign.CriteresCiblage = System.Text.Json.JsonSerializer.Serialize(dto.CriteresCiblage);
                }

                var created = await _campaignRepository.CreateAsync(campaign);

                // Si pas de date d'envoi programmée, exécuter immédiatement
                if (!dto.DateEnvoi.HasValue || dto.DateEnvoi.Value <= DateTime.Now)
                {
                    // Créer un nouveau scope pour la tâche en arrière-plan
                    // pour éviter que le DbContext soit disposé avant la fin de l'exécution
                    var campaignId = created.IdCampagne;
                    _ = Task.Run(async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        try
                        {
                            var dispatchService = scope.ServiceProvider.GetRequiredService<ICommunicationDispatchService>();
                            await dispatchService.ExecuteCampaignAsync(campaignId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Erreur lors de l'exécution automatique de la campagne {CampaignId}", campaignId);
                        }
                    });
                }

                return CreatedAtAction(nameof(GetById), new { id = created.IdCampagne }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création de la campagne");
                return StatusCode(500, new { message = "Erreur lors de la création de la campagne", error = ex.Message });
            }
        }

        // PUT: api/CommunicationCampaign/5
        [HttpPut("{id}")]
        [Permission("CommunicationCampaign.Update")]
        public async Task<ActionResult<CommunicationCampaign>> Update(int id, [FromBody] UpdateCommunicationCampaignDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var existing = await _campaignRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = $"Campagne {id} introuvable" });

                // Mettre à jour uniquement les champs fournis
                if (dto.Titre != null)
                    existing.Titre = dto.Titre;
                if (dto.Contenu != null)
                    existing.Contenu = dto.Contenu;
                if (dto.TypeCampagne != null)
                    existing.TypeCampagne = dto.TypeCampagne;
                if (dto.IdSociete.HasValue)
                    existing.IdSociete = dto.IdSociete;
                if (dto.ActiverPush.HasValue)
                    existing.ActiverPush = dto.ActiverPush.Value;
                if (dto.ActiverSms.HasValue)
                    existing.ActiverSms = dto.ActiverSms.Value;
                if (dto.ActiverEmail.HasValue)
                    existing.ActiverEmail = dto.ActiverEmail.Value;
                if (dto.ActiverInApp.HasValue)
                    existing.ActiverInApp = dto.ActiverInApp.Value;
                if (dto.DateEnvoi.HasValue)
                    existing.DateEnvoi = dto.DateEnvoi;

                // Sérialiser les critères de ciblage si fournis
                if (dto.CriteresCiblage != null)
                {
                    existing.CriteresCiblage = System.Text.Json.JsonSerializer.Serialize(dto.CriteresCiblage);
                }

                var updated = await _campaignRepository.UpdateAsync(existing);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la mise à jour de la campagne {CampaignId}", id);
                return StatusCode(500, new { message = "Erreur lors de la mise à jour", error = ex.Message });
            }
        }

        // DELETE: api/CommunicationCampaign/5
        [HttpDelete("{id}")]
        [Permission("CommunicationCampaign.Delete")]
        public async Task<ActionResult<object>> Delete(int id)
        {
            try
            {
                var deleted = await _campaignRepository.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Campagne {id} introuvable" });

                return Ok(new 
                { 
                    message = "Campagne désactivée avec succès (soft delete)",
                    idCampagne = id,
                    note = "La campagne a été désactivée. Les données sont conservées pour l'historique."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression de la campagne {CampaignId}", id);
                return StatusCode(500, new { message = "Erreur lors de la suppression", error = ex.Message });
            }
        }

        // POST: api/CommunicationCampaign/5/execute
        [HttpPost("{id}/execute")]
        [Permission("CommunicationCampaign.Update")]
        public async Task<ActionResult<CommunicationCampaignDispatchResult>> Execute(int id)
        {
            try
            {
                var result = await _dispatchService.ExecuteCampaignAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'exécution de la campagne {CampaignId}", id);
                return StatusCode(500, new { message = "Erreur lors de l'exécution", error = ex.Message });
            }
        }

        // GET: api/CommunicationCampaign/5/preview
        [HttpGet("{id}/preview")]
        [Permission("CommunicationCampaign.Read")]
        public async Task<ActionResult<List<Client>>> Preview(int id)
        {
            try
            {
                var clients = await _dispatchService.PreviewTargetedClientsAsync(id);
                return Ok(new { count = clients.Count, clients });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la prévisualisation de la campagne {CampaignId}", id);
                return StatusCode(500, new { message = "Erreur lors de la prévisualisation", error = ex.Message });
            }
        }
    }
}

