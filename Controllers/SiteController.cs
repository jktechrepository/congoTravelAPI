using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Attributes;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Site;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SiteController : ControllerBase
    {
        private readonly ISiteRepository _repository;
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteController> _logger;

        public SiteController(
            ISiteRepository repository,
            CongoTravelDbContext context,
            ILogger<SiteController> logger)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }

        private static SiteResponseDto MapToDto(Site a) => new()
        {
            IdSite = a.IdSite,
            IdSociete = a.IdSociete,
            CodeSite = a.CodeSite,
            NomSite = a.NomSite,
            Ville = a.Ville,
            Adresse = a.Adresse,
            Telephone = a.Telephone,
            NumeroMobileMoney = a.NumeroMobileMoney,
            NomResponsableSite = a.NomResponsableSite,
            Email = a.Email,
            Genre = a.Genre,
            Statut = a.Statut,
            IsSitePrincipal = a.IsSitePrincipal,
            DateCreation = a.DateCreation,
            DateModification = a.DateModification
        };

        /// <summary>Liste tous les sites.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SiteResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteResponseDto>>> GetAll(CancellationToken ct)
        {
            var list = await _repository.GetAllAsync(ct);
            return Ok(list.Select(MapToDto));
        }

        /// <summary>Détail d'un site.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SiteResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteResponseDto>> GetById(int id, CancellationToken ct)
        {
            var a = await _repository.GetByIdAsync(id, ct);
            if (a == null)
                return NotFound(new { message = $"Site {id} introuvable." });
            return Ok(MapToDto(a));
        }

        /// <summary>Sites d'une société.</summary>
        [HttpGet("societe/{idSociete:int}")]
        [ProducesResponseType(typeof(IEnumerable<SiteResponseDto>), 200)]
        public async Task<ActionResult<IEnumerable<SiteResponseDto>>> GetBySociete(int idSociete, CancellationToken ct)
        {
            var list = await _repository.GetBySocieteAsync(idSociete, ct);
            return Ok(list.Select(MapToDto));
        }

        /// <summary>
        /// Créer un site et provisionner automatiquement un Agent + Utilisateur Gérant.
        /// Règle métier: au moins un contact doit être fourni pour le responsable du site (Email ou Telephone).
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<ActionResult<object>> Create([FromBody] SiteCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var societeOk = await _context.Societes.AsNoTracking().AnyAsync(s => s.IdSociete == dto.IdSociete, ct);
            if (!societeOk)
                return BadRequest(new { message = $"Société {dto.IdSociete} introuvable." });

            try
            {
                var result = await _repository.CreateWithGerantAsync(dto, ct);
                var response = new
                {
                    site = MapToDto(result.Site),
                    gerantUser = new
                    {
                        email = result.GerantUtilisateur.Email,
                        telephone = result.GerantUtilisateur.Telephone,
                        username = result.GerantUtilisateur.DefaultUsername,
                        motDePasse = result.GerantMotDePasseParDefaut,
                        nomComplet = result.GerantUtilisateur.NomComplet ?? "Gérant",
                        idSite = result.GerantUtilisateur.IdSite,
                        idAgent = result.GerantAgent.IdAgent,
                        message = "Email de bienvenue envoyé automatiquement au gérant"
                    }
                };
                return CreatedAtAction(nameof(GetById), new { id = result.Site.IdSite }, response);
            }
            catch (SiteBootstrapConflictException ex)
            {
                return Conflict(new { code = ex.Reason.ToString(), message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Création site refusée");
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Mettre à jour un site.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SiteResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<SiteResponseDto>> Update(int id, [FromBody] SiteUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != dto.IdSite)
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas au corps." });

            var existing = await _context.Sites.FirstOrDefaultAsync(a => a.IdSite == id, ct);
            if (existing == null)
                return NotFound(new { message = $"Site {id} introuvable." });

            try
            {
                var updated = await _repository.UpdateAsync(new Site
                {
                    IdSite = dto.IdSite,
                    IdSociete = existing.IdSociete,
                    CodeSite = dto.CodeSite,
                    NomSite = dto.NomSite,
                    Ville = dto.Ville,
                    Adresse = dto.Adresse,
                    Telephone = dto.Telephone,
                    NumeroMobileMoney = dto.NumeroMobileMoney,
                    NomResponsableSite = dto.NomResponsableSite,
                    Email = dto.Email,
                    Genre = dto.Genre,
                    Statut = dto.Statut
                }, dto.IsSitePrincipal, ct);

                return Ok(MapToDto(updated!));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Mise à jour site refusée");
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>Activer / désactiver un site.</summary>
        [HttpPut("toggle-statut/{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ToggleStatut(int id, CancellationToken ct)
        {
            var ok = await _repository.ToggleStatutAsync(id, ct);
            if (!ok)
                return NotFound(new { message = $"Site {id} introuvable." });
            return Ok(new { message = "Statut modifié avec succès" });
        }

        /// <summary>Supprimer un site.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            try
            {
                var ok = await _repository.DeleteAsync(id, ct);
                if (!ok)
                    return NotFound(new { message = $"Site {id} introuvable." });
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Suppression site {Id} bloquée (références)", id);
                return Conflict(new { message = "Impossible de supprimer ce site : des enregistrements y sont encore liés." });
            }
        }
    }
}
