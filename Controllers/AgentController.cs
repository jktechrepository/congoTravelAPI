using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Services;
using CongoTravel.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Data;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Données sensibles - Token JWT requis
    public class AgentController : ControllerBase
    {
        private readonly IAgentRepository _agentRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly CongoTravelDbContext _context;

        public AgentController(
            IAgentRepository agentRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            CongoTravelDbContext context)
        {
            _agentRepository = agentRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _context = context;
        }

        // GET: api/Agent
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Agent>>> GetAgents()
        {
            var agents = await _agentRepository.GetAllAsync();

            // 🔒 Restriction: exclure les comptes système (admin/superadmin)
            var emailsAExclure = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "admin@congotravel.cd",
                "superadmin@congotravel.cd"
            };

            var filtered = agents
                .Where(a =>
                    string.IsNullOrWhiteSpace(a.EmailAgent) ||
                    !emailsAExclure.Contains(a.EmailAgent.Trim()))
                .ToList();

            return Ok(filtered);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Gestion des rôles pour les agents
        // ═══════════════════════════════════════════════════════════════════
        // ⚠️ IMPORTANT : Ces routes doivent être AVANT la route {id} pour éviter les conflits

        /// <summary>
        /// Ajoute un ou plusieurs rôles à un agent
        /// Les rôles sont ajoutés à l'utilisateur associé à cet agent
        /// Le RoleAgent correspond au Nom du rôle dans la table Roles
        /// </summary>
        /// <param name="idAgent">ID de l'agent</param>
        /// <param name="request">Requête contenant la liste des rôles à ajouter (chaque rôle contient RoleAgent et IsPrimary)</param>
        /// <returns>Résultat détaillé de l'opération avec les rôles ajoutés avec succès et ceux qui ont échoué</returns>
        [HttpPost("{idAgent}/add-role")]
        [ProducesResponseType(typeof(AddRolesResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AddRolesResult>> AddRoleToAgent(int idAgent, [FromBody] AddRolesToAgentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Vérifier que l'agent existe
                var agent = await _agentRepository.GetByIdAsync(idAgent);
                if (agent == null)
                {
                    return NotFound(new AddRolesResult 
                    { 
                        Success = false,
                        Message = $"Agent avec l'ID {idAgent} non trouvé",
                        TotalRoles = request.Roles.Count
                    });
                }

                // Convertir la liste de strings en tuples pour le service
                // Utiliser la valeur par défaut false si IsPrimary n'est pas fourni
                var isPrimary = request.IsPrimary ?? false;
                var roles = request.Roles.Select(r => (RoleAgent: r, IsPrimary: isPrimary));

                // Ajouter les rôles à l'agent
                var result = await _agentRepository.AddRolesToAgentAsync(
                    idAgent, 
                    roles,
                    _currentUserService.UserId > 0 ? _currentUserService.UserId : null
                );

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new AddRolesResult
                { 
                    Success = false,
                    Message = $"Erreur lors de l'ajout des rôles à l'agent: {ex.Message}",
                    TotalRoles = request.Roles.Count
                });
            }
        }

            /// <summary>
            /// Remplace un RoleAgent par un autre pour un agent
            /// Le statut IsPrimary est conservé lors du remplacement
            /// </summary>
            /// <param name="idAgent">ID de l'agent</param>
            /// <param name="request">Requête contenant l'ancien et le nouveau RoleAgent</param>
            /// <returns>Résultat de l'opération</returns>
            [HttpPut("{idAgent}/replace-role")]
            [ProducesResponseType(typeof(object), 200)]
            [ProducesResponseType(400)]
            [ProducesResponseType(404)]
            [ProducesResponseType(401)]
            public async Task<ActionResult<object>> ReplaceRoleAgent(int idAgent, [FromBody] ReplaceRoleAgentRequest request)
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Vérifier que l'ancien et le nouveau rôle sont différents
                if (request.AncienRoleAgent.Trim().Equals(request.NouveauRoleAgent.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new 
                    { 
                        success = false,
                        message = "L'ancien et le nouveau RoleAgent doivent être différents"
                    });
                }

                try
                {
                    // Vérifier que l'agent existe
                    var agent = await _agentRepository.GetByIdAsync(idAgent);
                    if (agent == null)
                    {
                        return NotFound(new { message = $"Agent avec l'ID {idAgent} non trouvé" });
                    }

                    // Remplacer le rôle
                    var success = await _agentRepository.ReplaceRoleAgentAsync(
                        idAgent, 
                        request.AncienRoleAgent.Trim(), 
                        request.NouveauRoleAgent.Trim(),
                        _currentUserService.UserId > 0 ? _currentUserService.UserId : null
                    );

                    if (success)
                    {
                        return Ok(new 
                        { 
                            success = true,
                            message = $"Rôle '{request.AncienRoleAgent}' remplacé par '{request.NouveauRoleAgent}' avec succès pour l'agent {idAgent}",
                            idAgent = idAgent,
                            ancienRoleAgent = request.AncienRoleAgent,
                            nouveauRoleAgent = request.NouveauRoleAgent
                        });
                    }
                    else
                    {
                        return BadRequest(new 
                        { 
                            success = false,
                            message = $"Impossible de remplacer le rôle '{request.AncienRoleAgent}' par '{request.NouveauRoleAgent}' pour l'agent {idAgent}. " +
                                     $"Vérifiez que l'agent a bien l'ancien rôle et que les rôles existent dans la base de données."
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new 
                    { 
                        success = false,
                        message = "Erreur lors du remplacement du rôle",
                        error = ex.Message 
                    });
                }
            }

            // GET: api/Agent/5
            [HttpGet("{id}")]
        [Permission("Agent.Read")]
        public async Task<ActionResult<Agent>> GetAgent(int id)
        {
            var agent = await _agentRepository.GetByIdAsync(id);
            if (agent == null)
            {
                return NotFound();
            }
            return Ok(agent);
        }

        // GET: api/Agent/societe/5
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Agent>>> GetAgentsBySociete(int idSociete)
        {
            var agents = await _agentRepository.GetBySocieteAsync(idSociete);

            // 🔒 Restriction: exclure les comptes système (admin/superadmin)
            var emailsAExclure = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "admin@congotravel.cd",
                "superadmin@congotravel.cd"
            };

            var filtered = agents
                .Where(a =>
                    string.IsNullOrWhiteSpace(a.EmailAgent) ||
                    !emailsAExclure.Contains(a.EmailAgent.Trim()))
                .ToList();

            return Ok(filtered);
            
            return Ok(agents);
        }
        
        // GET: api/Agent/societe/paged
        [HttpGet("societe/{idSociete}/paged")]
        public async Task<ActionResult<PagedResult<Agent>>> GetAgentsPaged(int idSociete, [FromQuery] PagedRequest request)
        {
            var agents = await _agentRepository.GetPagedAsync(idSociete, request);
            
           // var mappedData = MapToClientResponseDtoList(agents.Data);
            var mappedResult = new PagedResult<Agent>(
                agents.Data,
                agents.TotalCount,
                agents.PageNumber,
                agents.PageSize
            );
            return Ok(mappedResult);
        }

        // GET: api/Agent/statut/true
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<Agent>>> GetAgentsByStatut(bool statut)
        {
            var agents = await _agentRepository.GetByStatutAsync(statut);
            return Ok(agents);
        }

        // GET: api/Agent/exists/5
        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> AgentExists(int id)
        {
            var exists = await _agentRepository.ExistsAsync(id);
            return Ok(exists);
        }

        // POST: api/Agent
        [HttpPost]
        public async Task<ActionResult<Agent>> CreateAgent(Agent agent)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdAgent = await _agentRepository.CreateAsync(agent);
                return CreatedAtAction(nameof(GetAgent), new { id = createdAgent.IdAgent }, createdAgent);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Cas d'unicité (email ou serial number)
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateAgentConstraint(ex))
            {
                return Conflict(new { message = "Cet email ou SerialNumber est déjà utilisé." });
            }
        }

        // POST: api/Agent/batch
        [HttpPost("batch")]
        public async Task<ActionResult<object>> CreateAgentsBatch(IEnumerable<Agent> agents)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agentList = agents.ToList();
            if (!agentList.Any())
            {
                return BadRequest(new { message = "La liste des agents est vide" });
            }

            try
            {
                var createdAgents = await _agentRepository.CreateBatchAsync(agentList);
                return Ok(new { 
                    message = $"{createdAgents.Count()} agent(s) créé(s) avec succès",
                    total = agentList.Count,
                    success = createdAgents.Count(),
                    failed = agentList.Count - createdAgents.Count(),
                    agents = createdAgents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la création par lot", error = ex.Message });
            }
        }

        // PUT: api/Agent/5
        /// <summary>
        /// Modifier les informations d'un agent
        /// </summary>
        /// <remarks>
        /// Permet de modifier les informations personnelles et professionnelles d'un agent.
        /// 
        /// Champs modifiables :
        /// - Informations personnelles (nom, prénom, email, téléphone, etc.)
        /// - Informations professionnelles (fonction, grade, date d'embauche)
        /// - Adresse complète
        /// 
        /// Champs protégés :
        /// - Matricule (auto-généré, immuable)
        /// - SerialNumber (endpoint dédié)
        /// - École (immuable)
        /// - Statut (endpoint toggle-statut)
        /// </remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Agent), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Agent>> UpdateAgent(int id, [FromBody] UpdateAgentDto dto)
        {
            if (id != dto.IdAgent)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Récupérer l'agent existant
            var existingAgent = await _agentRepository.GetByIdAsync(id);
            if (existingAgent == null)
            {
                return NotFound(new { message = "Agent non trouvé" });
            }

            var currentRole = _currentUserService.UserRole;
            var isSuperAdmin = _currentUserService.IsSuperAdmin;
            var currentAgentId = _currentUserService.AgentId;
            var currentSocieteId = _currentUserService.SocieteId;
            var isSelf = currentAgentId.HasValue && currentAgentId.Value == existingAgent.IdAgent;
            var existingRoleAgent = existingAgent.RoleAgent ?? string.Empty;

            if (!isSuperAdmin)
            {
                if (existingAgent.Statut != true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul un Super-Admin peut modifier un agent désactivé." });
                }

                if (!isSelf)
                {
                    if (string.Equals(existingRoleAgent, UserRoles.SUPER_ADMIN, StringComparison.OrdinalIgnoreCase))
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul un Super-Admin peut modifier ce profil." });
                    }

                    if (string.Equals(currentRole, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentSocieteId == 0 || existingAgent.IdSociete != currentSocieteId)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous ne pouvez modifier que les agents de votre école." });
                        }
                    }
                    else if (string.Equals(currentRole, UserRoles.GERANT, StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentSocieteId == 0 || existingAgent.IdSociete != currentSocieteId)
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous ne pouvez modifier que les agents de votre école." });
                        }

                        if (string.Equals(existingRoleAgent, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase))
                        {
                            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Un Directeur ne peut pas modifier le profil d'un Admin." });
                        }
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous n'êtes pas autorisé à modifier ce profil." });
                    }
                }
            }

            var requestedRoleAgent = string.IsNullOrWhiteSpace(dto.RoleAgent)
                ? existingAgent.RoleAgent
                : dto.RoleAgent.Trim();

            if (isSelf && !isSuperAdmin)
            {
                requestedRoleAgent = existingAgent.RoleAgent;
            }
            else if (!isSuperAdmin && !string.IsNullOrWhiteSpace(requestedRoleAgent))
            {
                if (string.Equals(requestedRoleAgent, UserRoles.SUPER_ADMIN, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seul un Super-Admin peut attribuer le rôle Super-Admin." });
                }

                if (string.Equals(requestedRoleAgent, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(currentRole, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Seuls un Super-Admin ou un Admin peuvent attribuer le rôle Admin." });
                }
            }

            // 📸 AUDIT: Snapshot AVANT
            var oldAgent = new Agent
            {
                IdAgent = existingAgent.IdAgent,
                NomComplet = existingAgent.NomComplet,
                EmailAgent = existingAgent.EmailAgent,
                TelephoneAgent = existingAgent.TelephoneAgent,
                Fonction = existingAgent.Fonction,
                RoleAgent = existingAgent.RoleAgent
            };

            // Mettre à jour seulement les champs autorisés
            existingAgent.NomComplet = dto.NomComplet;
            existingAgent.EmailAgent = dto.EmailAgent;
            existingAgent.TelephoneAgent = dto.TelephoneAgent;
            existingAgent.PhotoUrl = dto.PhotoUrl;
            if (dto.DateNaissance.HasValue)
                existingAgent.DateNaissance = dto.DateNaissance.Value;
            existingAgent.Genre = dto.Genre;
            existingAgent.EtatCivil = dto.EtatCivil;
            existingAgent.Fonction = dto.Fonction;
            existingAgent.RoleAgent = requestedRoleAgent;
            existingAgent.AdresseResidence = dto.AdresseResidence;
            
            // Champs protégés (JAMAIS modifiés ici)
            // ❌ Matricule → Immuable
            // ❌ SerialNumber → Endpoint dédié
            // ❌ IdSociete → Immuable
            // ❌ Statut → Endpoint toggle-statut
            // ❌ DateCreation → Immuable
            // ❌ RoleAgent → Endpoint dédié

            Agent? updatedAgent;
            try
            {
                updatedAgent = await _agentRepository.UpdateAsync(existingAgent);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (updatedAgent == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erreur lors de la mise à jour" });
            }

            // 📝 AUDIT
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldAgent, updatedAgent, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification agent");

            return Ok(updatedAgent);
        }

        // PUT: api/Agent/{idAgent}/AffecterAgentSite
        [HttpPut("{idAgent}/AffecterAgentSite")]
        [HttpPut("{idAgent}/site")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<object>> AffecterAgentSite(int idAgent, [FromBody] AffecterAgentSiteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var agent = await _agentRepository.GetByIdAsync(idAgent);
            if (agent == null)
            {
                return NotFound(new { message = $"Agent avec l'ID {idAgent} non trouvé." });
            }

            var site = await _context.Sites.AsNoTracking().FirstOrDefaultAsync(s => s.IdSite == dto.IdSite);
            if (site == null)
            {
                return NotFound(new { message = $"Site avec l'ID {dto.IdSite} non trouvé." });
            }

            if (site.IdSociete != agent.IdSociete)
            {
                return BadRequest(new
                {
                    message = "Le site sélectionné n'appartient pas à la même société que l'agent.",
                    details = new { agent.IdSociete, SiteSocieteId = site.IdSociete }
                });
            }

            var isSuperAdmin = _currentUserService.IsSuperAdmin;
            var currentRole = _currentUserService.UserRole;
            var currentSocieteId = _currentUserService.SocieteId;

            if (!isSuperAdmin)
            {
                if (currentSocieteId == 0 || currentSocieteId != agent.IdSociete)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "Vous ne pouvez affecter un site qu'aux agents de votre société."
                    });
                }

                if (string.Equals(currentRole, UserRoles.GERANT, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(agent.RoleAgent, UserRoles.ADMIN, StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "Un Gérant ne peut pas réaffecter un agent ayant le rôle Admin."
                    });
                }
            }

            var ancienIdSite = agent.IdSite;
            agent.IdSite = dto.IdSite;

            Agent? updated;
            try
            {
                updated = await _agentRepository.UpdateAsync(agent);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (updated == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Erreur lors de l'affectation du site à l'agent."
                });
            }

            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                new { IdAgent = agent.IdAgent, IdSite = ancienIdSite },
                new { IdAgent = updated.IdAgent, IdSite = updated.IdSite },
                ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent,
                "Affectation agent à un site");

            return Ok(new
            {
                message = "Agent affecté au site avec succès.",
                idAgent = updated.IdAgent,
                ancienIdSite,
                nouveauIdSite = updated.IdSite
            });
        }

        // DELETE: api/Agent/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgent(int id)
        {
            var success = await _agentRepository.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Agent/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _agentRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Agent non trouvé" });
                }

                var agent = await _agentRepository.GetByIdAsync(id);
                var estActif = agent != null;
                
                return Ok(new { 
                    message = "Statut modifié avec succès",
                    nouveauStatut = estActif,
                    agent = agent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du changement de statut", error = ex.Message });
            }
        }

        // ✅ GET: api/Agent/serial-number/{serialNumber}
        // Récupérer un agent par son numéro de série
        [HttpGet("serial-number/{serialNumber}")]
        public async Task<ActionResult<Agent>> GetAgentBySerialNumber(string serialNumber)
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
            {
                return BadRequest(new { message = "Le numéro de série ne peut pas être vide" });
            }

            var agent = await _agentRepository.GetBySerialNumberAsync(serialNumber);
            if (agent == null)
            {
                return NotFound(new { message = $"Aucun agent trouvé avec le numéro de série '{serialNumber}'" });
            }
            return Ok(agent);
        }

        // ✅ PUT: api/Agent/{idAgent}/serial-number
        // Mise à jour du Serial Number par IdAgent
        [HttpPut("{idAgent}/serial-number")]
        [Permission("Agent.Update")]
        public async Task<ActionResult<object>> UpdateSerialNumberById(int idAgent, [FromBody] UpdateSerialNumberDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _agentRepository.UpdateSerialNumberByIdAsync(idAgent, dto.SerialNumber);
                if (!success)
                {
                    return NotFound(new { message = $"Agent avec l'ID {idAgent} non trouvé" });
                }

                var agent = await _agentRepository.GetByIdAsync(idAgent);
                return Ok(new
                {
                    message = "Numéro de série mis à jour avec succès",
                    idAgent = idAgent,
                    serialNumber = dto.SerialNumber,
                    agent = agent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour du numéro de série", error = ex.Message });
            }
        }

        // ✅ PUT: api/Agent/matricule/{matricule}/serial-number
        // Mise à jour du Serial Number par Matricule
        [HttpPut("matricule/{matricule}/serial-number")]
        public async Task<ActionResult<object>> UpdateSerialNumberByMatricule(string matricule, [FromBody] UpdateSerialNumberDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(matricule))
            {
                return BadRequest(new { message = "Le matricule ne peut pas être vide" });
            }

            try
            {
                var success = await _agentRepository.UpdateSerialNumberByMatriculeAsync(matricule, dto.SerialNumber);
                if (!success)
                {
                    return NotFound(new { message = $"Agent avec le matricule '{matricule}' non trouvé" });
                }

                var agent = await _agentRepository.GetByMatriculeAsync(matricule);
                return Ok(new
                {
                    message = "Numéro de série mis à jour avec succès",
                    matricule = matricule,
                    serialNumber = dto.SerialNumber,
                    agent = agent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour du numéro de série", error = ex.Message });
            }
        }

        private static bool IsDuplicateAgentConstraint(DbUpdateException ex)
        {
            var mySqlEx = ex.InnerException as MySqlException
                          ?? ex.InnerException?.InnerException as MySqlException;

            if (mySqlEx != null)
            {
                if (mySqlEx.Number == 1062 || mySqlEx.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    return true;
            }

            var message = ex.InnerException?.Message ?? ex.Message;
            return !string.IsNullOrEmpty(message)
                   && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
        }
    }
}

