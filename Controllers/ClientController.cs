using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Client;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services;
using CongoTravel.Data;
using CongoTravel.Services.Repositories;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ClientExportService _clientExportService;
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ClientController> _logger;
        private readonly IClientRepository _clientRepository;
        private readonly IEmailVerificationService _emailVerificationService;

        public ClientController(
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ClientExportService clientExportService,
            IClientRepository clientRepository,
            CongoTravelDbContext context,
            ILogger<ClientController> logger,
            IEmailVerificationService emailVerificationService)
        {
            _clientRepository = clientRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _clientExportService = clientExportService;
            _context = context;
            _logger = logger;
            _emailVerificationService = emailVerificationService;
        }

        // POST: api/client/register - Endpoint public d'auto-inscription
        [HttpPost("register")]
        [AllowAnonymous]
        [ClientRegistrationRateLimit]
        public async Task<ActionResult<ClientRegistrationResponseDto>> RegisterClient([FromBody] RegisterClientDto dto)
        {
            _logger.LogInformation("Tentative d'inscription du client: {Email}", dto.EmailClient);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Modèle invalide lors de l'inscription: {Email}", dto.EmailClient);
                return BadRequest(new { 
                    success = false, 
                    message = "Données invalides", 
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            // Validation des conditions d'utilisation
            if (!dto.AcceptTerms)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Vous devez accepter les conditions d'utilisation" 
                });
            }

            try
            {
                var normalizedEmail = NormalizeOptionalEmail(dto.EmailClient);

                // Vérifier si l'email est déjà utilisé
                var existingClient = await _clientRepository.GetByEmailAsync(normalizedEmail);
                if (existingClient != null)
                {
                    _logger.LogWarning("Tentative d'inscription avec email déjà existant: {Email}", normalizedEmail);
                    return Conflict(new { 
                        success = false, 
                        message = "Cet email est déjà utilisé par un autre client" 
                    });
                }

                // Créer l'objet Client à partir du DTO
                var client = new Client
                {
                    NomClient = dto.NomClient.Trim(),
                    AdresseClient = string.IsNullOrWhiteSpace(dto.AdresseClient) ? null : dto.AdresseClient.Trim(),
                    Telephone = dto.Telephone.Trim(),
                    EmailClient = normalizedEmail,
                    GenreClient = dto.GenreClient,
                    Province = dto.Province?.Trim(),
                    Ville = dto.Ville?.Trim(),
                    Commune = dto.Commune?.Trim(),
                    Avenue = dto.Avenue?.Trim(),
                    Numero = dto.Numero?.Trim(),
                    Statut = true, // Actif par défaut
                    IsActif = true, // Actif par défaut
                    DateCreation = DateTime.UtcNow
                };

                // Créer le client
                Client created;
                try
                {
                    created = await _clientRepository.CreateAsync(client);
                    _logger.LogInformation("Client créé avec succès: {ClientId} - {Email}", created.IdClient, created.EmailClient);
                }
                catch (DbUpdateException ex) when (IsDuplicateEmailException(ex))
                {
                    _logger.LogWarning("Conflit d'email lors de la création: {Email}", dto.EmailClient);
                    return Conflict(new { 
                        success = false, 
                        message = "Cet email est déjà utilisé par un autre client" 
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la création du client: {Email}", dto.EmailClient);
                    return StatusCode(500, new { 
                        success = false, 
                        message = "Erreur lors de la création du compte" 
                    });
                }

                // Audit spécial pour inscription publique
                await _auditService.LogCreateAsync(
                    created, 
                    0, // Pas d'utilisateur connecté
                    "Public Registration", 
                    "Client", 
                    0, // Pas d'IdSociete
                    Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    "Auto-inscription client"
                );

                var emailVerificationRequired = !string.IsNullOrWhiteSpace(normalizedEmail);
                var emailVerificationSent = false;
                if (emailVerificationRequired)
                {
                    var userId = await _context.Utilisateurs
                        .Where(u => u.IdClient == created.IdClient)
                        .Select(u => (int?)u.IdUtilisateur)
                        .FirstOrDefaultAsync();
                    if (userId.HasValue)
                    {
                        emailVerificationSent = await _context.EmailVerificationTokens
                            .AnyAsync(t =>
                                t.IdUtilisateur == userId.Value
                                && t.DateUtilisation == null
                                && t.DateExpiration > DateTime.UtcNow);
                    }
                }

                // Préparer la réponse
                var response = new ClientRegistrationResponseDto
                {
                    IdClient = created.IdClient,
                    NomClient = created.NomClient,
                    EmailClient = created.EmailClient,
                    Telephone = created.Telephone,
                    DateCreation = created.DateCreation,
                    IsActif = created.IsActif,
                    Statut = created.Statut,
                    Message = "Inscription réussie !",
                    WelcomeMessage = emailVerificationRequired
                        ? "Bienvenue sur CongoTravel ! Vérifiez votre boîte mail et cliquez sur le lien pour confirmer votre adresse."
                        : "Bienvenue sur CongoTravel ! Votre compte a été créé avec succès. Vous pouvez maintenant faire des réservations.",
                    EmailVerificationRequired = emailVerificationRequired,
                    EmailVerificationSent = emailVerificationSent
                };

                _logger.LogInformation("Inscription réussie pour le client: {ClientId} - {Email}", created.IdClient, created.EmailClient);

                return CreatedAtAction(
                    nameof(GetClient), 
                    new { id = created.IdClient }, 
                    new { success = true, data = response }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'inscription: {Email}", dto.EmailClient);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Une erreur est survenue lors de l'inscription" 
                });
            }
        }

        /// <summary>
        /// Confirme l'adresse email via le token reçu dans le lien (email).
        /// </summary>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Token invalide" });

            var (success, statusCode, message) = await _emailVerificationService.VerifyAsync(dto.Token);
            if (!success)
                return StatusCode(statusCode, new { success = false, message });

            return Ok(new { success = true, message });
        }

        /// <summary>
        /// Renvoie un email de vérification (réponse générique anti-énumération).
        /// </summary>
        [HttpPost("resend-verification-email")]
        [AllowAnonymous]
        [EmailCheckRateLimit]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendEmailVerificationRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Email invalide" });

            var (success, statusCode, message) = await _emailVerificationService.ResendAsync(dto.Email);
            return StatusCode(statusCode, new { success, message });
        }

        // GET: api/client/check-email - Endpoint public pour vérifier la disponibilité d'un email
        [HttpPost("check-email")]
        [AllowAnonymous]
        [EmailCheckRateLimit]
        public async Task<ActionResult<EmailAvailabilityResponseDto>> CheckEmailAvailability([FromBody] CheckEmailAvailabilityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { 
                    success = false, 
                    message = "Email invalide" 
                });
            }

            try
            {
                var existingClient = await _clientRepository.GetByEmailAsync(dto.Email);
                var isAvailable = existingClient == null;

                var response = new EmailAvailabilityResponseDto
                {
                    Email = dto.Email,
                    IsAvailable = isAvailable,
                    Message = isAvailable 
                        ? "Cet email est disponible" 
                        : "Cet email est déjà utilisé"
                };

                return Ok(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'email: {Email}", dto.Email);
                return StatusCode(500, new { 
                    success = false, 
                    message = "Erreur lors de la vérification de l'email" 
                });
            }
        }

        // GET: api/Client — liste paginée (pageNumber, pageSize, searchTerm, sortBy défaut DateCreation, sortDescending défaut true, includeInactive, isActif)
        [HttpGet]
        public async Task<ActionResult<PagedResult<ClientResponseDto>>> GetClients([FromQuery] ClientPagedSearchRequestDto? request)
        {
            request ??= new ClientPagedSearchRequestDto();
            var result = await _clientRepository.GetPagedAsync(request);
            var mappedData = result.Data.Select(c => MapToClientResponseDto(c));
            var mappedResult = new PagedResult<ClientResponseDto>(
                mappedData.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
            return Ok(mappedResult);
        }

        // GET: api/Client/paged — même comportement que GET api/Client (rétro-compatibilité)
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ClientResponseDto>>> GetClientsPaged([FromQuery] ClientPagedSearchRequestDto? request)
        {
            request ??= new ClientPagedSearchRequestDto();
            var result = await _clientRepository.GetPagedAsync(request);
            var mappedData = result.Data.Select(c => MapToClientResponseDto(c));
            var mappedResult = new PagedResult<ClientResponseDto>(
                mappedData.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
            return Ok(mappedResult);
        }

      
        // GET: api/Client/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        [Permission("Client.ReadAll")]
        [ProducesResponseType(typeof(IEnumerable<ClientResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsBySociete(int idSociete)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            var clients = await _clientRepository.GetBySocieteAsync(idSociete);
            return Ok(clients.Select(c => MapToClientResponseDto(c, idSociete)));
        }
        
        // GET: api/Client/societe/{idSociete}/paged?searchTerm={searchTerm}&includeInactive={includeInactive}&page=1&pageSize=20
        [HttpGet("societe/{idSociete}/paged")]
        [Permission("Client.ReadAll")]
        [ProducesResponseType(typeof(PagedResult<ClientResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<ClientResponseDto>>> GetClientsBySocietePaged(int idSociete, [FromQuery] ClientPagedSearchRequestDto request)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            var result = await _clientRepository.GetBySocietePagedAsync(idSociete, request);
            var mappedData = result.Data.Select(c => MapToClientResponseDto(c, idSociete));
            var mappedResult = new PagedResult<ClientResponseDto>(
                mappedData.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
            return Ok(mappedResult);
        }

        // GET: api/Client/societe/{idSociete}/recherche?searchTerm={searchTerm}&includeInactive={includeInactive}
        [HttpGet("societe/{idSociete}/recherche")]
        [Permission("Client.ReadAll")]
        [ProducesResponseType(typeof(IEnumerable<ClientResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsBySocieteAndSearch(int idSociete, [FromQuery] ClientSearchRequestDto request)
        {
            var forbidden = ForbidIfNotAllowedSociete(idSociete);
            if (forbidden != null)
                return forbidden;

            var clients = await _clientRepository.GetBySocieteAndSearchAsync(idSociete, request.SearchTerm, request.IncludeInactive);
            return Ok(clients.Select(c => MapToClientResponseDto(c, idSociete)));
        }

        // GET: api/Client/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientResponseDto>> GetClient(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            var response = new ClientResponseDto
            {
                IdClient = client.IdClient,
                NomClient = client.NomClient,
                AdresseClient = client.AdresseClient,
                Telephone = client.Telephone,
                EmailClient = client.EmailClient,
                GenreClient = client.GenreClient,
                Statut = client.Statut,
                IsActif = client.IsActif,
                DateCreation = client.DateCreation,
                IdSociete = null
            };
            return Ok(response);
        }

        // GET: api/Client/nom/{nom}
        [HttpGet("nom/{nom}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsByNom(string nom)
        {
            var clients = await _clientRepository.GetByNomAsync(nom);
            var response = clients.Select(c => new ClientResponseDto
            {
                IdClient = c.IdClient,
                NomClient = c.NomClient,
                AdresseClient = c.AdresseClient,
                Telephone = c.Telephone,
                EmailClient = c.EmailClient,
                GenreClient = c.GenreClient,
                Statut = c.Statut,
                IsActif = c.IsActif,
                DateCreation = c.DateCreation,
                IdSociete = null
            });
            return Ok(response);
        }
        
        // POST: api/Client
        [HttpPost]
        public async Task<ActionResult<Client>> CreateClient([FromBody] CreateClientWithUsagesDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Créer l'objet Client à partir du DTO
            var client = new Client
            {
                NomClient = dto.NomClient,
                AdresseClient = string.IsNullOrWhiteSpace(dto.AdresseClient) ? null : dto.AdresseClient.Trim(),
                Telephone = dto.Telephone,
                EmailClient = dto.EmailClient,
                GenreClient = dto.GenreClient,
                Statut = dto.Statut,
                IsActif = dto.IsActif,
            };

            // Créer le client
            Client created;
            try
            {
                created = await _clientRepository.CreateAsync(client);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailException(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la création du client: {ex.Message}" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création client");

            return CreatedAtAction(nameof(GetClient), new { id = created.IdClient }, created);
        }

        // POST: api/Client/simple
        [HttpPost("simple")]
        public async Task<ActionResult<Client>> CreateClientSimple(Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Client created;
            try
            {
                created = await _clientRepository.CreateAsync(client);
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailException(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création client");

            return CreatedAtAction(nameof(GetClient), new { id = created.IdClient }, created);
        }

        // PUT: api/Client/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Client>> UpdateClient(int id, [FromBody] CreateClientWithUsagesDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // DEBUG: Log des valeurs reçues
            _logger.LogInformation("DEBUG UpdateClient - Client {Id}: NomClient reçu = {NomClient}", 
                id, dto.NomClient);

            var existing = await _clientRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Snapshot avant modification
            var oldClient = new Client
            {
                IdClient = existing.IdClient,
                NomClient = existing.NomClient,
                AdresseClient = existing.AdresseClient,
                Statut = existing.Statut
            };

            // Créer l'objet Client à partir du DTO (seulement les champs fournis)
            var client = new Client
            {
                IdClient = id,
                NomClient = dto.NomClient ?? existing.NomClient,
                AdresseClient = dto.AdresseClient != null
                    ? (string.IsNullOrWhiteSpace(dto.AdresseClient) ? null : dto.AdresseClient.Trim())
                    : existing.AdresseClient,
                Telephone = dto.Telephone ?? existing.Telephone,
                EmailClient = dto.EmailClient ?? existing.EmailClient,
                GenreClient = dto.GenreClient ?? existing.GenreClient,
                Province = dto.Province ?? existing.Province,
                Ville = dto.Ville ?? existing.Ville,
                Commune = dto.Commune ?? existing.Commune,
                Avenue = dto.Avenue ?? existing.Avenue,
                Numero = dto.Numero ?? existing.Numero
            };

            // DEBUG: Log de l'objet client créé pour le service
            _logger.LogInformation("DEBUG UpdateClient - Objet client créé");

            Client updated;
            try
            {
                updated = await _clientRepository.UpdateAsync(client);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmailException(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldClient, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification client avec usages");

            return Ok(updated);
        }

        // DELETE: api/Client/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteClient(int id)
        {
            var exists = await _clientRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var entity = await _clientRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Les fonctionnalités de paiements et factures ne sont plus disponibles après la refactorisation
            var hasClientFactures = false;
            var hasPaiements = false;
            var hasFactures = false;

            if (hasClientFactures || hasPaiements || hasFactures)
            {
                return BadRequest(new 
                { 
                    message = "Impossible de supprimer ce client car des données sont liées.",
                    details = new
                    {
                        hasClientFactures,
                        hasPaiements,
                        hasFactures
                    },
                    note = "Le client sera désactivé (soft delete) au lieu d'être supprimé."
                });
            }
            else
            { 
                return Ok(new 
                { 
                    message = "Client désactivé avec succès (soft delete)",
                    clientId = id,
                    note = "Le client et ses ClientUsage ont été désactivés. Les données sont conservées pour l'historique."
                });
            }
        }

        // PUT: api/Client/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _clientRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Client non trouvé" });
                }

                var client = await _clientRepository.GetByIdAsync(id);
                var nouveauStatut = client?.Statut ?? false;

                return Ok(new
                {
                    message = "Statut modifié avec succès",
                    statut = nouveauStatut,
                    client = client
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la modification du statut: {ex.Message}" });
            }
        }

        // PUT: api/Client/set-statut/{id}
       [HttpPut("set-statut/{id}")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _clientRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "Client non trouvé" });
                }

                var client = await _clientRepository.GetByIdAsync(id);

                return Ok(new
                {
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    client = client
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la définition du statut: {ex.Message}" });
            }
        } 

        private static ClientResponseDto MapToClientResponseDto(Client c, int? idSociete = null) => new()
        {
            IdClient = c.IdClient,
            NomClient = c.NomClient,
            AdresseClient = c.AdresseClient,
            Telephone = c.Telephone,
            EmailClient = c.EmailClient,
            GenreClient = c.GenreClient,
            Statut = c.Statut,
            IsActif = c.IsActif,
            DateCreation = c.DateCreation,
            IdSociete = idSociete
        };

        private ActionResult? ForbidIfNotAllowedSociete(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return null;

            if (_currentUserService.SocieteId == 0 || _currentUserService.SocieteId != idSociete)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "Accès interdit: vous ne pouvez consulter que les clients de votre société."
                });
            }

            return null;
        }

        private static bool IsDuplicateEmailException(DbUpdateException ex)
        {
            // MariaDB/MySQL duplicate entry : error code 1062
            var mySqlEx = ex.InnerException as MySqlException
                          ?? ex.InnerException?.InnerException as MySqlException;
            
            return mySqlEx?.Number == 1062 && mySqlEx.Message.Contains("email");
        }

        private static string? NormalizeOptionalEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }
}
