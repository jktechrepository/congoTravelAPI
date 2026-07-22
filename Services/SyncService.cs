using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CongoTravel.Helpers;
using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Sync;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    /// <summary>
    /// Implémentation du service de synchronisation offline.
    /// Version initiale: synchronise Clients + Paiements (utilisés comme "arriérés" si reste à payer).
    /// </summary>
    public class SyncService : ISyncService
    {
        private readonly CongoTravelDbContext _db;
        private readonly IWatermarkService _watermarkService;
        private readonly ICursorService _cursorService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            CongoTravelDbContext db,
            IWatermarkService watermarkService,
            ICursorService cursorService,
            ICurrentUserService currentUserService,
            ILogger<SyncService> logger)
        {
            _db = db;
            _watermarkService = watermarkService;
            _cursorService = cursorService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<SyncBootstrapDto> GetBootstrapAsync(int societeId)
        {
            var watermark = _watermarkService.CreateWatermark(DateTime.UtcNow, 0);
            var todayUtc = DateTime.UtcNow.Date;

            var voyages = await _db.Voyages
                .AsNoTracking()
                .ForSociete(societeId)
                .Where(v => v.Statut == true && v.DateDepart.Date >= todayUtc && v.DateDepart.Date < todayUtc.AddDays(2))
                .Include(v => v.Destination)
                .Include(v => v.Vehicule)
                .OrderBy(v => v.DateDepart)
                .ThenBy(v => v.HeureDepart)
                .Select(v => new VoyageSyncDto
                {
                    IdVoyage = v.Id,
                    IdSociete = v.IdSociete,
                    IdSite = v.IdSite,
                    DateDepart = v.DateDepart,
                    HeureDepart = v.HeureDepart,
                    Prix = v.Prix,
                    CodeDevisePrix = v.CodeDevisePrix,
                    VilleDepart = v.Destination != null ? v.Destination.VilleDepart : null,
                    VilleArrivee = v.Destination != null ? v.Destination.VilleArrivee : null,
                    IdVehicule = v.IdVehicule,
                    CapaciteSieges = v.Vehicule != null ? v.Vehicule.NombreSiege : null,
                    Statut = v.Statut ?? true,
                    UpdatedAt = (v.DateModification ?? v.DateCreation).ToUniversalTime()
                })
                .Take(500)
                .ToListAsync();

            var clientsPage = await GetClientsAsync(societeId, new SyncRequestDto { PageSize = 500 });
            var arrearsPage = await GetArrearsAsync(societeId, new SyncArrearsRequestDto { PageSize = 500, OnlyOutstanding = true });

            var configEntity = await _db.ConfigSocietes.AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdSociete == societeId);

            ConfigSocieteSyncDto? configDto = null;
            if (configEntity != null)
            {
                configDto = new ConfigSocieteSyncDto
                {
                    IdSociete = configEntity.IdSociete,
                    JoursAvanceMaxReservation = configEntity.JoursAvanceMaxReservation,
                    DureeValiditeBilletJours = configEntity.DureeValiditeBilletJours,
                    ReaffectationActive = configEntity.ReaffectationActive,
                    HeuresLimiteReaffectation = configEntity.HeuresLimiteReaffectation,
                    DureeHoldFlexPayMinutes = configEntity.DureeHoldFlexPayMinutes,
                    PenaliteReaffectationPourcentage = configEntity.PenaliteReaffectationPourcentage,
                    UpdatedAt = (configEntity.DateModification ?? configEntity.DateCreation).ToUniversalTime()
                };
            }

            return new SyncBootstrapDto
            {
                Watermark = watermark,
                Clients = clientsPage.Items,
                Arrears = arrearsPage.Items,
                Voyages = voyages,
                ConfigSociete = configDto,
                ReservationWorkflowV2 = new ReservationWorkflowV2ApiHintsDto()
            };
        }

        public async Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(int societeId, SyncRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");

            var since = ParseSinceOrNull(request.Since);
            var cursor = ParseCursorOrNull(request.Cursor);

            var baseQuery = _db.Clients.AsNoTracking()
                .Where(c => _db.Reservations.Any(r =>
                    r.IdSociete == societeId && r.Statut && r.IdClient == c.IdClient));

            if (since != null)
            {
                baseQuery = baseQuery.Where(c =>
                    (c.UpdatedAt ?? c.DateCreation) > since.Value.lastModified ||
                    ((c.UpdatedAt ?? c.DateCreation) == since.Value.lastModified && c.IdClient > since.Value.lastId));
            }

            if (cursor != null)
            {
                baseQuery = baseQuery.Where(c =>
                    (c.UpdatedAt ?? c.DateCreation) > cursor.Value.updatedAt ||
                    ((c.UpdatedAt ?? c.DateCreation) == cursor.Value.updatedAt && c.IdClient > cursor.Value.id));
            }

            var pageSize = ClampPageSize(request.PageSize);
            var items = await baseQuery
                .OrderBy(c => (c.UpdatedAt ?? c.DateCreation))
                .ThenBy(c => c.IdClient)
                .Take(pageSize + 1)
                .Select(c => new ClientSyncDto
                {
                    IdClient = c.IdClient,
                    NomClient = c.NomClient,
                    AdresseClient = c.AdresseClient,
                    Telephone = c.Telephone,
                    EmailClient = c.EmailClient,
                    GenreClient = c.GenreClient,
                    // Clients ayant au moins une réservation dans la société (aligné GET /api/Client/societe/{idSociete}).
                    IdSociete = societeId,
                    IdCategorieClient = null,
                    IsActif = c.IsActif,
                    Statut = c.Statut,
                    IsDeleted = c.IsDeleted ?? false,
                    UpdatedAt = (c.UpdatedAt ?? c.DateCreation).ToUniversalTime()
                })
                .ToListAsync();

            var hasMore = items.Count > pageSize;
            if (hasMore)
                items = items.Take(pageSize).ToList();

            var last = items.LastOrDefault();
            var nextCursor = last != null
                ? _cursorService.CreateCursor(new CursorPayload
                {
                    UpdatedAt = last.UpdatedAt,
                    Id = last.IdClient
                })
                : null;

            var nextSince = items.Count > 0
                ? _watermarkService.CreateWatermark(items[^1].UpdatedAt, items[^1].IdClient)
                : request.Since;

            return new SyncPageDto<ClientSyncDto>
            {
                Snapshot = snapshot,
                Items = items,
                NextCursor = hasMore ? nextCursor : null,
                HasMore = hasMore,
                NextSince = nextSince
            };
        }

        public async Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(int societeId, SyncArrearsRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");

            var since = ParseSinceOrNull(request.Since);
            var cursor = ParseCursorOrNull(request.Cursor);

            // Dans ce backend voyage, on mappe "arrears" = paiements avec un reste à payer.
            var baseQuery = _db.Paiements
                .AsNoTracking()
                .Where(p => p.IdSociete == societeId && !p.IsDeleted && p.Statut);

            if (request.OnlyOutstanding)
                baseQuery = baseQuery.Where(p => (p.ResteAPaye ?? 0) > 0);

            if (since != null)
            {
                baseQuery = baseQuery.Where(p =>
                    ((p.DateModification ?? p.DateCreation) > since.Value.lastModified) ||
                    ((p.DateModification ?? p.DateCreation) == since.Value.lastModified && p.IdPaiement > since.Value.lastId));
            }

            if (cursor != null)
            {
                baseQuery = baseQuery.Where(p =>
                    ((p.DateModification ?? p.DateCreation) > cursor.Value.updatedAt) ||
                    ((p.DateModification ?? p.DateCreation) == cursor.Value.updatedAt && p.IdPaiement > cursor.Value.id));
            }

            var pageSize = ClampPageSize(request.PageSize);
            var items = await baseQuery
                .OrderBy(p => (p.DateModification ?? p.DateCreation))
                .ThenBy(p => p.IdPaiement)
                .Take(pageSize + 1)
                .Select(p => new ArrearSyncDto
                {
                    IdPaiement = p.IdPaiement,
                    IdFacture = p.IdReservation,
                    IdClient = 0, // Non disponible directement sur Paiement (travel)
                    NumeroFacture = p.IdReservation.HasValue ? $"RES-{p.IdReservation.Value}" : null,
                    DateEmission = (p.DateCreation).ToUniversalTime(),
                    Mois = p.DateCreation.ToString("MM"),
                    Annees = p.DateCreation.Year,
                    MontantTotal = p.MontantAPaye,
                    MontantPaye = p.MontantPaye ?? 0,
                    MontantDu = p.ResteAPaye ?? (p.MontantAPaye - (p.MontantPaye ?? 0)),
                    LibelleUsage = null,
                    EstArrierePreExistant = false,
                    DateModification = (p.DateModification ?? p.DateCreation).ToUniversalTime()
                })
                .ToListAsync();

            var hasMore = items.Count > pageSize;
            if (hasMore)
                items = items.Take(pageSize).ToList();

            var last = items.LastOrDefault();
            var nextCursor = last != null
                ? _cursorService.CreateCursor(new CursorPayload
                {
                    UpdatedAt = last.DateModification,
                    Id = last.IdPaiement
                })
                : null;

            var nextSince = items.Count > 0
                ? _watermarkService.CreateWatermark(items[^1].DateModification, items[^1].IdPaiement)
                : request.Since;

            return new SyncPageDto<ArrearSyncDto>
            {
                Snapshot = snapshot,
                Items = items,
                NextCursor = hasMore ? nextCursor : null,
                HasMore = hasMore,
                NextSince = nextSince
            };
        }

        public async Task<SyncDeletionsDto> GetDeletionsAsync(int societeId, SyncDeletionsRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");

            var since = _watermarkService.ParseWatermark(request.Since);
            var sinceDate = since.lastModified;
            var sinceId = since.lastId;

            var deletedClientIds = await _db.Clients
                .AsNoTracking()
                .Where(c =>
                    (c.IsDeleted ?? false) &&
                    ((c.UpdatedAt ?? c.DateCreation) > sinceDate ||
                     ((c.UpdatedAt ?? c.DateCreation) == sinceDate && c.IdClient > sinceId)))
                .OrderBy(c => (c.UpdatedAt ?? c.DateCreation))
                .ThenBy(c => c.IdClient)
                .Select(c => c.IdClient)
                .Take(5000)
                .ToListAsync();

            var deletedPaymentIds = await _db.Paiements
                .AsNoTracking()
                .Where(p =>
                    p.IdSociete == societeId &&
                    p.IsDeleted &&
                    ((p.DateModification ?? p.DateCreation) > sinceDate ||
                     ((p.DateModification ?? p.DateCreation) == sinceDate && p.IdPaiement > sinceId)))
                .OrderBy(p => (p.DateModification ?? p.DateCreation))
                .ThenBy(p => p.IdPaiement)
                .Select(p => p.IdPaiement)
                .Take(5000)
                .ToListAsync();

            var nextSince = _watermarkService.CreateWatermark(DateTime.UtcNow, 0);

            return new SyncDeletionsDto
            {
                Snapshot = snapshot,
                DeletedClientIds = deletedClientIds,
                RemovedClientFactureIds = new List<int>(),
                DeletedPaymentIds = deletedPaymentIds,
                NextSince = nextSince
            };
        }

        public async Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(int societeId, PaymentBatchRequestDto request)
        {
            var result = new PaymentBatchResultDto
            {
                Results = new List<PaymentResultDto>(),
                Summary = new PaymentSummaryDto { Total = request.Items.Count }
            };

            if (request.Items.Count == 0)
                return result;

            // Idempotence pragmatique: on utilise ReferenceTransaction si présente, sinon ClientRequestId.
            // (Le schéma actuel ne contient pas encore un champ dédié ClientRequestId.)
            foreach (var item in request.Items)
            {
                try
                {
                    try
                    {
                        MethodePaiementHelper.EnsureAllowedForSyncBatch(item.MethodePaiement);
                    }
                    catch (InvalidOperationException ex)
                    {
                        result.Summary.Errors++;
                        result.Results.Add(new PaymentResultDto
                        {
                            ClientRequestId = item.ClientRequestId,
                            Status = "rejected",
                            ErrorCode = "ELECTRONIC_NOT_ALLOWED_IN_BATCH",
                            Message = ex.Message
                        });
                        continue;
                    }

                    var referenceIdempotence = string.IsNullOrWhiteSpace(item.ReferenceTransaction)
                        ? item.ClientRequestId
                        : item.ReferenceTransaction.Trim();

                    var duplicate = await _db.Paiements.AsNoTracking().AnyAsync(p =>
                        p.IdSociete == societeId &&
                        !p.IsDeleted &&
                        p.ReferenceTransaction != null &&
                        p.ReferenceTransaction == referenceIdempotence);

                    if (duplicate)
                    {
                        result.Summary.Duplicates++;
                        result.Results.Add(new PaymentResultDto
                        {
                            ClientRequestId = item.ClientRequestId,
                            Status = "duplicate",
                            Message = "Paiement déjà enregistré (idempotence via ReferenceTransaction)."
                        });
                        continue;
                    }

                    var paiement = new Paiement
                    {
                        IdSociete = societeId,
                        IdUtilisateur = _currentUserService.UserId,
                        IdReservation = item.IdFacture, // compat: IdFacture transporté par la spec
                        MontantAPaye = item.MontantPaye,
                        MontantPaye = item.MontantPaye,
                        MethodePaiement = MethodePaiementHelper.NormalizeForStorage(item.MethodePaiement),
                        ReferenceTransaction = referenceIdempotence,
                        Statut = true,
                        DateCreation = item.DatePaiementUtc.ToUniversalTime(),
                        DateModification = item.DatePaiementUtc.ToUniversalTime()
                    };
                    paiement.MettreAJourResteAPaye();

                    _db.Paiements.Add(paiement);
                    await _db.SaveChangesAsync();

                    result.Summary.Created++;
                    result.Results.Add(new PaymentResultDto
                    {
                        ClientRequestId = item.ClientRequestId,
                        Status = "created",
                        IdPaiement = paiement.IdPaiement,
                        NewMontantDu = paiement.ResteAPaye ?? 0,
                        Message = "Paiement enregistré."
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur technique pendant ProcessPaymentsBatchAsync (ClientRequestId={ClientRequestId})", item.ClientRequestId);
                    result.Summary.Errors++;
                    result.Results.Add(new PaymentResultDto
                    {
                        ClientRequestId = item.ClientRequestId,
                        Status = "error",
                        ErrorCode = "TECH_ERROR",
                        Message = "Erreur technique lors de l'enregistrement du paiement."
                    });
                }
            }

            return result;
        }

        public async Task<SyncPageDto<VoyageSyncDto>> GetVoyagesAsync(int societeId, SyncRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");
            var since = ParseSinceOrNull(request.Since);
            var cursor = ParseCursorOrNull(request.Cursor);

            var baseQuery = _db.Voyages.AsNoTracking()
                .ForSociete(societeId)
                .Where(v => v.Statut == true);

            if (since != null)
            {
                baseQuery = baseQuery.Where(v =>
                    ((v.DateModification ?? v.DateCreation) > since.Value.lastModified) ||
                    ((v.DateModification ?? v.DateCreation) == since.Value.lastModified && v.Id > since.Value.lastId));
            }

            if (cursor != null)
            {
                baseQuery = baseQuery.Where(v =>
                    ((v.DateModification ?? v.DateCreation) > cursor.Value.updatedAt) ||
                    ((v.DateModification ?? v.DateCreation) == cursor.Value.updatedAt && v.Id > cursor.Value.id));
            }

            var pageSize = ClampPageSize(request.PageSize);
            var items = await baseQuery
                .Include(v => v.Destination)
                .Include(v => v.Vehicule)
                .OrderBy(v => (v.DateModification ?? v.DateCreation))
                .ThenBy(v => v.Id)
                .Take(pageSize + 1)
                .Select(v => new VoyageSyncDto
                {
                    IdVoyage = v.Id,
                    IdSociete = v.IdSociete,
                    IdSite = v.IdSite,
                    DateDepart = v.DateDepart,
                    HeureDepart = v.HeureDepart,
                    Prix = v.Prix,
                    CodeDevisePrix = v.CodeDevisePrix,
                    VilleDepart = v.Destination != null ? v.Destination.VilleDepart : null,
                    VilleArrivee = v.Destination != null ? v.Destination.VilleArrivee : null,
                    IdVehicule = v.IdVehicule,
                    CapaciteSieges = v.Vehicule != null ? v.Vehicule.NombreSiege : null,
                    Statut = v.Statut ?? true,
                    UpdatedAt = (v.DateModification ?? v.DateCreation).ToUniversalTime()
                })
                .ToListAsync();

            return BuildSyncPage(items, snapshot, request.Since, pageSize, i => i.UpdatedAt, i => i.IdVoyage);
        }

        public async Task<SyncPageDto<ReservationSyncDto>> GetReservationsAsync(int societeId, SyncRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");
            var since = ParseSinceOrNull(request.Since);
            var cursor = ParseCursorOrNull(request.Cursor);

            var baseQuery = _db.Reservations.AsNoTracking().ForSociete(societeId);

            if (since != null)
            {
                baseQuery = baseQuery.Where(r =>
                    ((r.DateModification ?? r.DateCreation) > since.Value.lastModified) ||
                    ((r.DateModification ?? r.DateCreation) == since.Value.lastModified && r.IdReservation > since.Value.lastId));
            }

            if (cursor != null)
            {
                baseQuery = baseQuery.Where(r =>
                    ((r.DateModification ?? r.DateCreation) > cursor.Value.updatedAt) ||
                    ((r.DateModification ?? r.DateCreation) == cursor.Value.updatedAt && r.IdReservation > cursor.Value.id));
            }

            var pageSize = ClampPageSize(request.PageSize);
            var items = await baseQuery
                .OrderBy(r => (r.DateModification ?? r.DateCreation))
                .ThenBy(r => r.IdReservation)
                .Take(pageSize + 1)
                .Select(r => new ReservationSyncDto
                {
                    IdReservation = r.IdReservation,
                    IdSociete = r.IdSociete,
                    IdVoyage = r.IdVoyage,
                    IdClient = r.IdClient,
                    IdSite = r.IdSite,
                    NombreDePlace = r.NombreDePlace,
                    StatutReservation = r.StatutReservation,
                    DateReservation = r.DateReservation,
                    UpdatedAt = (r.DateModification ?? r.DateCreation).ToUniversalTime()
                })
                .ToListAsync();

            return BuildSyncPage(items, snapshot, request.Since, pageSize, i => i.UpdatedAt, i => i.IdReservation);
        }

        public async Task<SyncPageDto<BilletSyncDto>> GetBilletsAsync(int societeId, SyncRequestDto request)
        {
            var snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O");
            var since = ParseSinceOrNull(request.Since);
            var cursor = ParseCursorOrNull(request.Cursor);

            var baseQuery = _db.Billets.AsNoTracking().ForSociete(societeId);

            if (since != null)
            {
                baseQuery = baseQuery.Where(b =>
                    ((b.DateModification ?? b.DateCreation) > since.Value.lastModified) ||
                    ((b.DateModification ?? b.DateCreation) == since.Value.lastModified && b.IdBillet > since.Value.lastId));
            }

            if (cursor != null)
            {
                baseQuery = baseQuery.Where(b =>
                    ((b.DateModification ?? b.DateCreation) > cursor.Value.updatedAt) ||
                    ((b.DateModification ?? b.DateCreation) == cursor.Value.updatedAt && b.IdBillet > cursor.Value.id));
            }

            var pageSize = ClampPageSize(request.PageSize);
            var items = await baseQuery
                .OrderBy(b => (b.DateModification ?? b.DateCreation))
                .ThenBy(b => b.IdBillet)
                .Take(pageSize + 1)
                .Select(b => new BilletSyncDto
                {
                    IdBillet = b.IdBillet,
                    IdSociete = b.IdSociete,
                    IdReservation = b.IdReservation,
                    IdReservationPassenger = b.IdReservationPassenger,
                    IdSiege = b.IdSiege,
                    QrCode = b.QrCode,
                    IsUsed = b.IsUsed,
                    DateGeneration = b.DateGeneration,
                    UpdatedAt = (b.DateModification ?? b.DateCreation).ToUniversalTime()
                })
                .ToListAsync();

            return BuildSyncPage(items, snapshot, request.Since, pageSize, i => i.UpdatedAt, i => i.IdBillet);
        }

        private SyncPageDto<T> BuildSyncPage<T>(
            List<T> items,
            string snapshot,
            string? since,
            int pageSize,
            Func<T, DateTime> updatedAtSelector,
            Func<T, int> idSelector)
        {
            var hasMore = items.Count > pageSize;
            if (hasMore)
                items = items.Take(pageSize).ToList();

            var last = items.LastOrDefault();
            var nextCursor = last != null
                ? _cursorService.CreateCursor(new CursorPayload
                {
                    UpdatedAt = updatedAtSelector(last),
                    Id = idSelector(last)
                })
                : null;

            var nextSince = items.Count > 0
                ? _watermarkService.CreateWatermark(updatedAtSelector(items[^1]), idSelector(items[^1]))
                : since;

            return new SyncPageDto<T>
            {
                Snapshot = snapshot,
                Items = items,
                NextCursor = hasMore ? nextCursor : null,
                HasMore = hasMore,
                NextSince = nextSince
            };
        }

        private static int ClampPageSize(int pageSize)
        {
            if (pageSize < 1) return 1;
            if (pageSize > 5000) return 5000;
            return pageSize;
        }

        private (DateTime lastModified, int lastId)? ParseSinceOrNull(string? since)
        {
            if (string.IsNullOrWhiteSpace(since))
                return null;

            try
            {
                return _watermarkService.ParseWatermark(since);
            }
            catch
            {
                return null;
            }
        }

        private (DateTime updatedAt, int id)? ParseCursorOrNull(string? cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor))
                return null;

            try
            {
                return _cursorService.ParseCursor(cursor);
            }
            catch
            {
                return null;
            }
        }

        private sealed class CursorPayload
        {
            public DateTime? UpdatedAt { get; init; }
            public int Id { get; init; }
        }
    }
}

