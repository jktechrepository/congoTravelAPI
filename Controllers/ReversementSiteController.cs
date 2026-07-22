using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CongoTravel.Attributes;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.ReversementSite;
using CongoTravel.Services;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReversementSiteController : ControllerBase
    {
        private readonly IReversementSiteService _reversementSiteService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReversementSiteController> _logger;

        public ReversementSiteController(
            IReversementSiteService reversementSiteService,
            ICurrentUserService currentUserService,
            ILogger<ReversementSiteController> logger)
        {
            _reversementSiteService = reversementSiteService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        [HttpPost]
        [Permission("ReversementSite.Create")]
        [ProducesResponseType(typeof(ReversementSiteResponseDto), 200)]
        public async Task<IActionResult> Create([FromBody] InitierReversementSiteDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId != dto.IdSociete)
                return Forbid();

            try
            {
                var result = await _reversementSiteService.InitierAsync(dto, _currentUserService.UserId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur initiation reversement site {IdSite}", dto.IdSite);
                return StatusCode(500, new { message = "Erreur interne lors du reversement." });
            }
        }

        [HttpGet("{id:int}")]
        [Permission("ReversementSite.Read")]
        [ProducesResponseType(typeof(ReversementSiteResponseDto), 200)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _reversementSiteService.GetByIdAsync(
                id, _currentUserService.SocieteId, _currentUserService.IsSuperAdmin);

            if (result == null)
                return NotFound(new { message = $"Reversement {id} introuvable." });

            return Ok(result);
        }

        [HttpGet("site/{idSite:int}")]
        [Permission("ReversementSite.Read")]
        [ProducesResponseType(typeof(PagedResponse<ReversementSiteResponseDto>), 200)]
        public async Task<IActionResult> GetBySite(int idSite, [FromQuery] PagedRequest request, [FromQuery] int? idSociete = null)
        {
            var societeId = _currentUserService.IsSuperAdmin && idSociete.HasValue
                ? idSociete.Value
                : _currentUserService.SocieteId;

            if (societeId <= 0)
                return BadRequest(new { message = "IdSociete requis." });

            try
            {
                var result = await _reversementSiteService.GetBySitePagedAsync(
                    idSite, societeId, request, _currentUserService.IsSuperAdmin);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("verifier/{orderNumber}")]
        [Permission("ReversementSite.Read")]
        [ProducesResponseType(typeof(ReversementSiteResponseDto), 200)]
        public async Task<IActionResult> Verifier(string orderNumber)
        {
            try
            {
                var result = await _reversementSiteService.VerifierEtFinaliserAsync(
                    orderNumber, _currentUserService.SocieteId, _currentUserService.IsSuperAdmin);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur vérification reversement {OrderNumber}", orderNumber);
                return StatusCode(500, new { message = "Erreur interne." });
            }
        }
    }
}
