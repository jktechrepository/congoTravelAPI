using CongoTravel.Services.PhotoStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CongoTravel.Controllers
{
    /// <summary>
    /// Backfill one-shot MEDIUMBLOB → S3 (Admin). Contrat API photoBase64 inchangé.
    /// </summary>
    [ApiController]
    [Route("api/admin/photo-storage")]
    [Authorize(Roles = "Admin,Super-Admin")]
    public class PhotoStorageAdminController : ControllerBase
    {
        private readonly IPhotoS3BackfillService _backfillService;
        private readonly ILogger<PhotoStorageAdminController> _logger;

        public PhotoStorageAdminController(
            IPhotoS3BackfillService backfillService,
            ILogger<PhotoStorageAdminController> logger)
        {
            _backfillService = backfillService;
            _logger = logger;
        }

        /// <summary>Backfill toutes les familles photo (véhicules, événements, restaurants, sites).</summary>
        [HttpPost("backfill")]
        public async Task<ActionResult<PhotoS3BackfillResult>> BackfillAll(
            [FromQuery] bool clearPhotoData = true,
            [FromQuery] int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            _logger.LogWarning(
                "Backfill photos S3 démarré par {User} — clearPhotoData={Clear}",
                User.Identity?.Name,
                clearPhotoData);

            var result = await _backfillService.BackfillAllAsync(
                clearPhotoData,
                batchSize,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("backfill/vehicules")]
        public async Task<ActionResult<PhotoS3BackfillResult>> BackfillVehicules(
            [FromQuery] bool clearPhotoData = true,
            [FromQuery] int batchSize = 50,
            CancellationToken cancellationToken = default)
        {
            var result = await _backfillService.BackfillVehiculesAsync(
                clearPhotoData,
                batchSize,
                cancellationToken);
            return Ok(result);
        }
    }
}
