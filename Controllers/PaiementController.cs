using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Services.Repositories;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Controller pour la gestion des paiements
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaiementController : ControllerBase
    {
        private readonly IPaiementRepository _paiementRepository;
        private readonly ILogger<PaiementController> _logger;
        private readonly CongoTravelDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public PaiementController(
            IPaiementRepository paiementRepository,
            ILogger<PaiementController> logger,
            CongoTravelDbContext context,
            ICurrentUserService currentUserService)
        {
            _paiementRepository = paiementRepository;
            _logger = logger;
            _context = context;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Récupérer tous les paiements (filtrés par société JWT).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? idSociete = null)
        {
            try
            {
                var societeId = TenantGuard.ResolveListSocieteId(
                    _currentUserService.SocieteId,
                    _currentUserService.IsSuperAdmin,
                    idSociete);
                var paiements = await _paiementRepository.GetBySocieteAsync(societeId);
                return Ok(PaiementApiResponseMapper.Map(paiements));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les paiements");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupérer un paiement par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var paiement = await _paiementRepository.GetByIdAsync(id);
                if (paiement == null)
                {
                    return NotFound(new { message = $"Paiement avec ID {id} non trouvé" });
                }
                return Ok(PaiementApiResponseMapper.Map(paiement));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Créer un nouveau paiement
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(PaiementResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreatePaiementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto.IdSite.HasValue)
                {
                    try
                    {
                        await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                            _context, dto.IdSite, dto.IdSociete);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }
                }

                // Mapper DTO vers modèle
                var codeDevisePaiement = dto.CodeDevisePaiement.Trim().ToUpperInvariant();
                var datePaiement = dto.DatePaiement ?? DateTime.UtcNow;
                var conversion = await ResolveConversionAsync(dto.IdSociete, codeDevisePaiement, datePaiement);
                if (!conversion.Success)
                {
                    return BadRequest(new { message = conversion.ErrorMessage });
                }

                string? reservationOrigine = null;
                if (dto.IdReservation.HasValue)
                {
                    reservationOrigine = await _context.Reservations.AsNoTracking()
                        .Where(r => r.IdReservation == dto.IdReservation.Value)
                        .Select(r => r.Origine)
                        .FirstOrDefaultAsync();
                }

                var origine = OrigineOperationResolver.ResolveForPaiement(_currentUserService, reservationOrigine);

                var paiement = new Paiement
                {
                    MontantAPaye = dto.MontantAPaye,
                    MontantPaye = dto.MontantPaye,
                    CodeDevisePaiement = codeDevisePaiement,
                    CodeDevisePrincipale = conversion.CodeDevisePrincipale!,
                    TauxVersDevisePrincipale = conversion.Taux,
                    MontantAPayeDevisePrincipale = Math.Round(dto.MontantAPaye * conversion.Taux, 2, MidpointRounding.AwayFromZero),
                    MontantPayeDevisePrincipale = dto.MontantPaye.HasValue
                        ? Math.Round(dto.MontantPaye.Value * conversion.Taux, 2, MidpointRounding.AwayFromZero)
                        : null,
                    MethodePaiement = dto.MethodePaiement,
                    ReferenceTransaction = dto.ReferenceTransaction,
                    Statut = dto.Statut ?? true,
                    IdUtilisateur = dto.IdUtilisateur,
                    IdReservation = dto.IdReservation,
                    IdSociete = dto.IdSociete,
                    IdSite = dto.IdSite,
                    DatePaiement = datePaiement,
                    DateCreation = DateTime.UtcNow,
                    Origine = origine
                };

                paiement.MettreAJourResteAPaye();
                paiement.ResteAPayeDevisePrincipale = paiement.MontantPayeDevisePrincipale.HasValue
                    ? Math.Round(paiement.MontantAPayeDevisePrincipale - paiement.MontantPayeDevisePrincipale.Value, 2, MidpointRounding.AwayFromZero)
                    : paiement.MontantAPayeDevisePrincipale;

                var createdPaiement = await _paiementRepository.CreateAsync(paiement);
                var createdDto = await MapPaiementResponseAsync(createdPaiement.IdPaiement);
                if (createdDto == null)
                    return StatusCode(500, new { message = "Paiement créé mais introuvable lors de la projection de réponse." });

                return CreatedAtAction(nameof(GetById), new { id = createdPaiement.IdPaiement }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du paiement");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Mettre à jour un paiement
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(PaiementResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePaiementDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingPaiement = await _paiementRepository.GetByIdAsync(id);
                if (existingPaiement == null)
                {
                    return NotFound(new { message = $"Paiement avec ID {id} non trouvé" });
                }

                if (dto.DesassocierSite)
                {
                    existingPaiement.IdSite = null;
                }
                else if (dto.IdSite.HasValue)
                {
                    try
                    {
                        await SiteSocieteValidation.EnsureSiteBelongsToSocieteAsync(
                            _context, dto.IdSite, existingPaiement.IdSociete);
                    }
                    catch (InvalidOperationException ex)
                    {
                        return BadRequest(new { message = ex.Message });
                    }

                    existingPaiement.IdSite = dto.IdSite;
                }

                // Mettre à jour les propriétés
                existingPaiement.MontantAPaye = dto.MontantAPaye ?? existingPaiement.MontantAPaye;
                existingPaiement.MontantPaye = dto.MontantPaye ?? existingPaiement.MontantPaye;
                existingPaiement.MethodePaiement = dto.MethodePaiement ?? existingPaiement.MethodePaiement;
                existingPaiement.ReferenceTransaction = dto.ReferenceTransaction ?? existingPaiement.ReferenceTransaction;
                existingPaiement.Statut = dto.Statut ?? existingPaiement.Statut;
                existingPaiement.DatePaiement = dto.DatePaiement ?? existingPaiement.DatePaiement;
                if (!string.IsNullOrWhiteSpace(dto.CodeDevisePaiement))
                {
                    existingPaiement.CodeDevisePaiement = dto.CodeDevisePaiement.Trim().ToUpperInvariant();
                }

                var updateConversion = await ResolveConversionAsync(
                    existingPaiement.IdSociete,
                    existingPaiement.CodeDevisePaiement,
                    existingPaiement.DatePaiement);
                if (!updateConversion.Success)
                {
                    return BadRequest(new { message = updateConversion.ErrorMessage });
                }

                existingPaiement.CodeDevisePrincipale = updateConversion.CodeDevisePrincipale!;
                existingPaiement.TauxVersDevisePrincipale = updateConversion.Taux;
                existingPaiement.MontantAPayeDevisePrincipale = Math.Round(
                    existingPaiement.MontantAPaye * updateConversion.Taux, 2, MidpointRounding.AwayFromZero);
                existingPaiement.MontantPayeDevisePrincipale = existingPaiement.MontantPaye.HasValue
                    ? Math.Round(existingPaiement.MontantPaye.Value * updateConversion.Taux, 2, MidpointRounding.AwayFromZero)
                    : null;
                existingPaiement.DateModification = DateTime.UtcNow;

                existingPaiement.MettreAJourResteAPaye();
                existingPaiement.ResteAPayeDevisePrincipale = existingPaiement.MontantPayeDevisePrincipale.HasValue
                    ? Math.Round(existingPaiement.MontantAPayeDevisePrincipale - existingPaiement.MontantPayeDevisePrincipale.Value, 2, MidpointRounding.AwayFromZero)
                    : existingPaiement.MontantAPayeDevisePrincipale;

                var updatedPaiement = await _paiementRepository.UpdateAsync(existingPaiement);
                var updatedDto = await MapPaiementResponseAsync(updatedPaiement.IdPaiement);
                if (updatedDto == null)
                    return StatusCode(500, new { message = "Paiement mis à jour mais introuvable lors de la projection de réponse." });

                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Supprimer un paiement
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _paiementRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Paiement avec ID {id} non trouvé" });
                }
                return Ok(new { message = "Paiement supprimé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du paiement {Id}", id);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupérer les paiements par réservation
        /// </summary>
        [HttpGet("reservation/{idReservation}")]
        public async Task<IActionResult> GetByReservation(int idReservation)
        {
            try
            {
                var paiements = await _paiementRepository.GetByReservationAsync(idReservation);
                return Ok(PaiementApiResponseMapper.Map(paiements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour la réservation {IdReservation}", idReservation);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupérer les paiements par client
        /// </summary>
        [HttpGet("client/{idClient}")]
        public async Task<IActionResult> GetByClient(int idClient)
        {
            try
            {
                var paiements = await _paiementRepository.GetByClientAsync(idClient);
                return Ok(PaiementApiResponseMapper.Map(paiements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour le client {IdClient}", idClient);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupérer les paiements par société
        /// </summary>
        [HttpGet("societe/{idSociete}")]
        public async Task<IActionResult> GetBySociete(int idSociete)
        {
            try
            {
                var paiements = await _paiementRepository.GetBySocieteAsync(idSociete);
                return Ok(PaiementApiResponseMapper.Map(paiements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour la société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupérer les paiements par société avec pagination
        /// </summary>
        [HttpGet("societe/{idSociete}/paged")]
        public async Task<IActionResult> GetBySocietePaged(int idSociete, [FromQuery] PaiementPagedRequest request)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.OrigineGroupe)
                    && !OrigineOperationGroupe.IsValid(request.OrigineGroupe))
                {
                    return BadRequest(new
                    {
                        message = "origineGroupe invalide. Valeurs acceptées : CLIENT, AGENT, INCONNU."
                    });
                }

                var paiements = await _paiementRepository.GetBySocietePagedAsync(idSociete, request);
                return Ok(PaiementApiResponseMapper.Map(paiements));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des paiements pour la société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        private async Task<PaiementResponseDto?> MapPaiementResponseAsync(int idPaiement)
        {
            var paiement = await _paiementRepository.GetByIdAsync(idPaiement);
            return paiement == null ? null : PaiementApiResponseMapper.Map(paiement);
        }

        private async Task<(bool Success, string? ErrorMessage, string? CodeDevisePrincipale, decimal Taux)> ResolveConversionAsync(
            int idSociete,
            string codeDevisePaiement,
            DateTime datePaiement)
        {
            var societe = await _context.Societes.AsNoTracking().FirstOrDefaultAsync(s => s.IdSociete == idSociete);
            if (societe == null)
            {
                return (false, $"Société {idSociete} introuvable.", null, 0m);
            }

            var codePrincipale = string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale)
                ? "CDF"
                : societe.CodeDevisePrincipale.Trim().ToUpperInvariant();

            var devisePaiementExiste = await _context.DevisesMonetaires.AsNoTracking()
                .AnyAsync(d => d.CodeDevise == codeDevisePaiement && d.Statut);
            if (!devisePaiementExiste)
            {
                return (false, $"La devise de paiement '{codeDevisePaiement}' n'est pas active.", null, 0m);
            }

            if (codeDevisePaiement == codePrincipale)
            {
                return (true, null, codePrincipale, 1m);
            }

            var taux = await _context.TauxChanges.AsNoTracking()
                .Where(t => t.IdSociete == idSociete
                            && t.CodeDeviseSource == codeDevisePaiement
                            && t.CodeDeviseCible == codePrincipale
                            && t.Statut
                            && t.DateEffet <= datePaiement)
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync();

            if (!taux.HasValue)
            {
                return (false,
                    $"Aucun taux actif trouvé pour {codeDevisePaiement}->{codePrincipale} à la date {datePaiement:yyyy-MM-dd}.",
                    null,
                    0m);
            }

            return (true, null, codePrincipale, taux.Value);
        }
    }
}
