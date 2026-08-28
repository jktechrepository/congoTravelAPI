using System;
using System.Linq;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.ConfigSociete;
using CongoTravel.Models.Enums;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Gestion des sociétés - Token JWT requis
    public class SocieteController : ControllerBase
    {
        private readonly ISocieteRepository _societeRepository;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public SocieteController(
            ISocieteRepository societeRepository,
            IConfigSocieteRepository configSocieteRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _societeRepository = societeRepository;
            _configSocieteRepository = configSocieteRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/Societe
        [HttpGet]
        [Permission("Societe.ReadAll")]
        public async Task<ActionResult<IEnumerable<Societe>>> GetSocietes()
        {
            var societes = await _societeRepository.GetAllAsync();
            return Ok(societes);
        }

        // GET: api/Societe/5
        [HttpGet("{id}")]
        [Permission("Societe.Read")]
        public async Task<ActionResult<Societe>> GetSociete(int id)
        {
            var societe = await _societeRepository.GetByIdAsync(id);
            if (societe == null)
            {
                return NotFound();
            }
            return Ok(societe);
        }

        // GET: api/Societe/nom/{nom}
        [HttpGet("nom/{nom}")]
        [Permission("Societe.Read")]
        public async Task<ActionResult<Societe>> GetSocieteByNom(string nom)
        {
            var societe = await _societeRepository.GetByNomAsync(nom);
            if (societe == null)
            {
                return NotFound();
            }
            return Ok(societe);
        }

        // GET: api/Societe/code/{code}
        [HttpGet("code/{code}")]
        //public async Task<ActionResult<Societe>> GetSocieteByCode(string code)
        //{
        //    var societe = await _societeRepository.GetByCodeAsync(code);
        //    if (societe == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(societe);
        //}

        // GET: api/Societe/statut/{statut}
        [HttpGet("statut/{statut}")]
        //public async Task<ActionResult<IEnumerable<Societe>>> GetSocietesByStatut(bool statut)
        //{
        //    var societes = await _societeRepository.GetByStatutAsync(statut);
        //    return Ok(societes);
        //}

        // GET: api/Societe/5/utilisateurs
        [HttpGet("{id}/utilisateurs")]
        [Permission("Societe.Read")]
        public async Task<ActionResult<IEnumerable<Utilisateur>>> GetSocieteUtilisateurs(int id)
        {
            var utilisateurs = await _societeRepository.GetUtilisateursAsync(id);
            return Ok(utilisateurs);
        }

        // GET: api/Societe/5/agents
        [HttpGet("{id}/agents")]
        [Permission("Societe.Read")]
        public async Task<ActionResult<IEnumerable<Agent>>> GetSocieteAgents(int id)
        {
            var agents = await _societeRepository.GetAgentsAsync(id);
            return Ok(agents);
        }

        // GET: api/Societe/5/agents/caissiers
        [HttpGet("{id}/agents/caissiers")]
        [Permission("Societe.Read")]
        public async Task<ActionResult<PagedResult<Agent>>> GetSocieteCaissiers(
            int id,
            [FromQuery] PagedRequest request)
        {
            var caissiers = await _societeRepository.GetAgentsByRoleAsync(id, "Caissier", request);
            return Ok(caissiers);
        }

        /// <summary>
        /// Crée une société avec site initial. Le compte gérant est généré automatiquement à partir du bloc <c>site</c>
        /// (<c>nomResponsableSite</c>, <c>genre</c>, <c>email</c> / <c>telephone</c>), comme pour la création de site.
        /// </summary>
        // POST: api/Societe
        [HttpPost]
        [Permission("Societe.Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<object>> CreateSociete([FromBody] CreateSocieteWithBootstrapDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _societeRepository.CreateWithBootstrapAsync(dto);
                var createdSociete = result.Societe;
                var admin = result.AdminUtilisateur;

                var response = new
                {
                    societe = createdSociete,
                    site = new
                    {
                        id = result.Site.IdSite,
                        code = result.Site.CodeSite,
                        nom = result.Site.NomSite,
                        idSociete = result.Site.IdSociete
                    },
                    adminUser = admin != null ? new
                    {
                        email = admin.Email,
                        telephone = admin.Telephone,
                        motDePasse = "123456",
                        nomComplet = admin.NomComplet ?? "Administrateur",
                        idSite = admin.IdSite,
                        message = "Email de bienvenue envoyé automatiquement à l'administrateur"
                    } : null,
                    gerantUser = new
                    {
                        email = result.GerantUtilisateur.Email,
                        telephone = result.GerantUtilisateur.Telephone,
                        username = result.GerantUtilisateur.DefaultUsername,
                        motDePasse = result.GerantMotDePasseParDefaut,
                        nomComplet = result.GerantUtilisateur.NomComplet ?? "Gérant",
                        idSite = result.GerantUtilisateur.IdSite,
                        idAgent = result.GerantAgent.IdAgent,
                        message = result.GerantWelcomeEmailQueued
                            ? "Email de bienvenue envoyé automatiquement au gérant"
                            : "Aucun email de site : identifiant gérant = téléphone ; pas d’email de bienvenue automatique"
                    }
                };

                return CreatedAtAction(nameof(GetSociete), new { id = createdSociete.IdSociete }, response);
            }
            catch (SocieteBootstrapConflictException ex)
            {
                return Conflict(new { code = ex.Reason.ToString(), message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par une autre société." });
            }
        }

        // PUT: api/Societe/5
        [HttpPut("{id}")]
        [Permission("Societe.Update")]
        [ProducesResponseType(typeof(Societe), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Societe>> UpdateSociete(int id, [FromBody] UpdateSocieteDto dto)
        {
            if (id != dto.IdSociete)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingSociete = await _societeRepository.GetByIdAsync(id);
            if (existingSociete == null)
            {
                return NotFound(new { message = "École non trouvée" });
            }

            if (!_currentUserService.IsSuperAdmin)
            {
                if (_currentUserService.SocieteId == 0)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Impossible de déterminer votre école. Veuillez-vous reconnecter." });
                }

                if (_currentUserService.SocieteId != existingSociete.IdSociete)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous ne pouvez modifier que votre propre école." });
                }

                if (!new[] { UserRoles.ADMIN, UserRoles.GERANT, UserRoles.SOUS_DIRECTEUR }.Contains(_currentUserService.UserRole))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Votre rôle ne permet pas de modifier les informations de l'école." });
                }
            }

            // 📸 AUDIT: Snapshot AVANT modification
            var oldSociete = new Societe
            {
                IdSociete = existingSociete.IdSociete,
                Nom = existingSociete.Nom,
                Description = existingSociete.Description,
                Devise = existingSociete.Devise,
                Type = existingSociete.Type,
                Telephone = existingSociete.Telephone,
                EmailContact = existingSociete.EmailContact
            };

            // Mettre à jour seulement les champs autorisés
            existingSociete.Nom = dto.Nom;
            existingSociete.Description = dto.Description;
            existingSociete.Devise = dto.Devise;
            existingSociete.Type = dto.Type;
            existingSociete.Logo = dto.Logo;
            existingSociete.SiteWeb = dto.SiteWeb;
            existingSociete.Telephone = dto.Telephone;
            existingSociete.EmailContact = dto.EmailContact;
            existingSociete.NomCompletResponsable = dto.NomCompletResponsable;
            existingSociete.GenreResponsable = dto.GenreResponsable;
            existingSociete.AdresseResidence = dto.AdresseResidence;

            Societe updatedSociete;
            try
            {
                updatedSociete = await _societeRepository.UpdateAsync(existingSociete);
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par une autre société." });
            }
            if (updatedSociete == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // 📝 AUDIT: Enregistrer
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldSociete, updatedSociete, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification école");

            return Ok(updatedSociete);
        }

        // DELETE: api/Societe/5
        [HttpDelete("{id}")]
        [Permission("Societe.Delete")]
        public async Task<IActionResult> DeleteSociete(int id)
        {
            var exists = await _societeRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound();
            }

            await _societeRepository.DeleteAsync(id);
            return NoContent();
        }

        // PUT: api/Societe/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        [Permission("Societe.Update")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _societeRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "École non trouvée" });
                }

                // Récupérer l'école après le toggle pour connaître le nouveau statut
                // Note: GetByIdAsync retourne null si l'école est désactivée à cause du filtre Statut
                var societeApresToggle = await _societeRepository.GetByIdAsync(id);
                var nouveauStatut = societeApresToggle != null;
                
                return Ok(new { 
                    message = "Statut modifié avec succès",
                    nouveauStatut = nouveauStatut,
                    statut = nouveauStatut
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du changement de statut", error = ex.Message });
            }
        }
        
        // PUT: api/Societe/set-statut/{id}
        [HttpPut("set-statut/{id}")]
        [Permission("Societe.Update")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _societeRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "École non trouvée" });
                }

                var societe = await _societeRepository.GetByIdAsync(id);
                
                return Ok(new { 
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    societe = societe
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la modification du statut", error = ex.Message });
            }
        }

        private static bool IsDuplicateEmail(DbUpdateException ex)
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
                   && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("email", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Configuration métier de la société (règles billet, réaffectation, horizon réservation).</summary>
        [HttpGet("{id:int}/config")]
        [Permission("ConfigSociete.Read")]
        public async Task<ActionResult<ConfigSocieteResponseDto>> GetConfig(int id, CancellationToken ct)
        {
            var societe = await _societeRepository.GetByIdAsync(id);
            if (societe == null)
                return NotFound(new { message = $"Société {id} introuvable." });

            var config = await _configSocieteRepository.GetOrCreateAsync(id, ct);
            return Ok(MapConfigToDto(config, societe.CodeDevisePrincipale));
        }

        /// <summary>Met à jour la configuration métier de la société.</summary>
        [HttpPut("{id:int}/config")]
        [Permission("ConfigSociete.Update")]
        public async Task<ActionResult<ConfigSocieteResponseDto>> UpdateConfig(
            int id,
            [FromBody] ConfigSocieteUpdateDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var societe = await _societeRepository.GetByIdAsync(id);
            if (societe == null)
                return NotFound(new { message = $"Société {id} introuvable." });

            try
            {
                var updated = await _configSocieteRepository.UpdateAsync(id, dto, ct);
                return Ok(MapConfigToDto(updated, societe.CodeDevisePrincipale));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static ConfigSocieteResponseDto MapConfigToDto(ConfigSociete config, string? codeDevisePrincipale) =>
            new()
            {
                IdConfigSociete = config.IdConfigSociete,
                IdSociete = config.IdSociete,
                DureeValiditeBilletJours = config.DureeValiditeBilletJours,
                PenaliteReaffectationPourcentage = config.PenaliteReaffectationPourcentage,
                JoursAvanceMaxReservation = config.JoursAvanceMaxReservation,
                HeuresLimiteReaffectation = config.HeuresLimiteReaffectation,
                HeuresOuvertureEmbarquementAvantDepart = config.HeuresOuvertureEmbarquementAvantDepart,
                HeuresFermetureEmbarquementApresJourDepart = config.HeuresFermetureEmbarquementApresJourDepart,
                HeuresOuvertureEntreeEvenementAvantDebut = config.HeuresOuvertureEntreeEvenementAvantDebut,
                HeuresOuvertureEntreeRestaurantAvantDebut = config.HeuresOuvertureEntreeRestaurantAvantDebut,
                DureeHoldFlexPayMinutes = config.DureeHoldFlexPayMinutes,
                ReaffectationActive = config.ReaffectationActive,
                ReservationIsActif = config.ReservationIsActif,
                ActiviteTransport = config.ActiviteTransport,
                ActiviteEvenement = config.ActiviteEvenement,
                ActiviteSiteTouristique = config.ActiviteSiteTouristique,
                ActiviteRestaurant = config.ActiviteRestaurant,
                ActiviteHotel = config.ActiviteHotel,
                AutoReversementPaiementElectronique = config.AutoReversementPaiementElectronique,
                PourcentageReversementSite = config.PourcentageReversementSite,
                FraisPlateforme = config.FraisPlateforme,
                CodeDeviseFraisPlateforme = config.CodeDeviseFraisPlateforme,
                MontAddPaieElectronique = config.MontAddPaieElectronique,
                CodeDeviseMontAddPaieElectronique = config.CodeDeviseMontAddPaieElectronique,
                PoidsBagageParKiloOffert = config.PoidsBagageParKiloOffert,
                CodeDevisePrincipale = codeDevisePrincipale,
                DateCreation = config.DateCreation,
                DateModification = config.DateModification
            };

    }
}
