using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueJourneeService : ISiteTouristiqueJourneeService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<SiteTouristiqueJourneeService> _logger;

        public SiteTouristiqueJourneeService(
            CongoTravelDbContext context,
            ILogger<SiteTouristiqueJourneeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SiteTouristiqueJourneeResponseDto> CreateDraftAsync(
            SiteTouristiqueCreateJourneeRequestDto request,
            int idSociete,
            int? idSiteTouristiquePlanification = null,
            CancellationToken cancellationToken = default)
        {
            var inventoryMode = ParseInventoryMode(request.InventoryMode);
            request.SalesOpenAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesOpenAtUtc);
            request.SalesCloseAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesCloseAtUtc);
            ValidateCreateRequest(request, inventoryMode);

            var lieu = await _context.SiteTouristiques
                .FirstOrDefaultAsync(
                    l => l.IdSiteTouristique == request.IdSiteTouristique && l.IdSociete == idSociete,
                    cancellationToken);

            if (lieu == null)
                throw new KeyNotFoundException($"Lieu touristique {request.IdSiteTouristique} introuvable.");

            var exists = await _context.SiteTouristiqueJournees
                .AsNoTracking()
                .AnyAsync(
                    j => j.IdSiteTouristique == request.IdSiteTouristique
                         && j.DateVisite == request.DateVisite,
                    cancellationToken);

            if (exists)
            {
                throw new SiteTouristiqueJourneeConflictException(
                    $"Une journée existe déjà pour le lieu {request.IdSiteTouristique} à la date {request.DateVisite:yyyy-MM-dd}.");
            }

            var codeDevise = NormalizeCodeDevise(request.CodeDevise);
            var utcNow = DateTime.UtcNow;
            var journee = new SiteTouristiqueJournee
            {
                IdSociete = idSociete,
                IdSiteTouristique = request.IdSiteTouristique,
                DateVisite = request.DateVisite,
                InventoryMode = inventoryMode,
                Status = SiteTouristiqueStatus.Draft,
                CodeDevise = codeDevise,
                SalesOpenAtUtc = request.SalesOpenAtUtc,
                SalesCloseAtUtc = request.SalesCloseAtUtc,
                IdSiteTouristiquePlanification = idSiteTouristiquePlanification,
                DateCreation = utcNow
            };

            switch (inventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    AttachGlobalQuota(journee, request.GlobalQuota!);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    await AttachClassQuotasAsync(
                        journee, request.ClassQuotas!, idSociete, cancellationToken);
                    break;
            }

            _context.SiteTouristiqueJournees.Add(journee);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique Draft créée — Id={Id}, Lieu={IdLieu}, Date={Date}, Mode={Mode}",
                journee.IdSiteTouristiqueJournee,
                request.IdSiteTouristique,
                request.DateVisite,
                inventoryMode);

            return await LoadJourneeResponseAsync(journee.IdSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public async Task<SiteTouristiqueJourneeResponseDto?> GetByIdAsync(
            int idSiteTouristiqueJournee,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = JourneeDetailQuery()
                .Where(j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee);

            if (idSociete.HasValue && idSociete.Value > 0)
                query = query.Where(j => j.IdSociete == idSociete.Value);

            var journee = await query.FirstOrDefaultAsync(cancellationToken);
            return journee == null ? null : SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        public async Task<SiteTouristiqueJourneeResponseDto?> GetPublishedByIdAsync(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken = default)
        {
            var journee = await JourneeDetailQuery()
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee
                         && j.Status == SiteTouristiqueStatus.Published,
                    cancellationToken);

            return journee == null ? null : SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListAsync(
            int idSociete,
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var journees = await BuildListQuery(idSociete, filter)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListPublishedGlobalAsync(
            SiteTouristiqueJourneeListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var query = JourneeListQuery()
                .Where(j => j.Status == SiteTouristiqueStatus.Published
                            && j.DateVisite >= today);

            if (filter?.IdSociete.HasValue == true && filter.IdSociete.Value > 0)
                query = query.Where(j => j.IdSociete == filter.IdSociete.Value);

            if (filter?.IdSiteTouristique.HasValue == true && filter.IdSiteTouristique.Value > 0)
                query = query.Where(j => j.IdSiteTouristique == filter.IdSiteTouristique.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(j => j.InventoryMode == filter.InventoryMode.Value);

            var journees = await query
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByStatusAsync(
            SiteTouristiqueStatus status,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueJourneeListFilter { Status = status },
                cancellationToken);

        public Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByInventoryModeAsync(
            SiteTouristiqueInventoryMode inventoryMode,
            int idSociete,
            CancellationToken cancellationToken = default) =>
            ListAsync(
                idSociete,
                new SiteTouristiqueJourneeListFilter { InventoryMode = inventoryMode },
                cancellationToken);

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateAsync(
            DateOnly dateVisite,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journees = await JourneeListQuery()
                .Where(j => j.IdSociete == idSociete && j.DateVisite == dateVisite)
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<IReadOnlyList<SiteTouristiqueJourneeListItemDto>> ListByDateRangeAsync(
            DateOnly dateDebut,
            DateOnly dateFin,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journees = await JourneeListQuery()
                .Where(j => j.IdSociete == idSociete
                            && j.DateVisite >= dateDebut
                            && j.DateVisite <= dateFin)
                .OrderBy(j => j.DateVisite)
                .ToListAsync(cancellationToken);

            return journees
                .Select(SiteTouristiqueJourneeMapper.ToListItemDto)
                .ToList();
        }

        public async Task<SiteTouristiqueJourneeResponseDto> PublishAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                    .ThenInclude(q => q.Classe)
                .Include(j => j.Lieu)
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            if (journee.Status != SiteTouristiqueStatus.Draft)
            {
                throw new InvalidOperationException(
                    $"Seule une journée Draft peut être publiée (statut actuel : {journee.Status}).");
            }

            if (journee.Lieu == null || journee.Lieu.Status != SiteTouristiqueStatus.Published)
            {
                throw new InvalidOperationException(
                    "Le lieu associé doit être Published avant de publier une journée.");
            }

            ValidateInventoryForPublish(journee);

            journee.Status = SiteTouristiqueStatus.Published;
            journee.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique publiée — Id={Id}, Societe={IdSociete}, Mode={Mode}",
                journee.IdSiteTouristiqueJournee,
                idSociete,
                journee.InventoryMode);

            return await LoadJourneeResponseAsync(journee.IdSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public async Task<SiteTouristiqueJourneeResponseDto> UpdateAsync(
            int idSiteTouristiqueJournee,
            SiteTouristiqueUpdateJourneeRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new InvalidOperationException("Le corps de la requête est obligatoire.");

            request.SalesOpenAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesOpenAtUtc);
            request.SalesCloseAtUtc = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(request.SalesCloseAtUtc);

            var journee = await _context.SiteTouristiqueJournees
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            switch (journee.Status)
            {
                case SiteTouristiqueStatus.Draft:
                    await UpdateDraftAsync(journee, request, idSociete, cancellationToken);
                    break;

                case SiteTouristiqueStatus.Published:
                    await UpdatePublishedAsync(journee, request, idSociete, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Impossible de modifier une journée au statut {journee.Status} (Draft ou Published uniquement).");
            }

            ApplySalesWindows(journee, request);
            journee.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique mise à jour — Id={Id}, Statut={Status}, Societe={IdSociete}",
                journee.IdSiteTouristiqueJournee,
                journee.Status,
                idSociete);

            return await LoadJourneeResponseAsync(journee.IdSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public async Task DeleteAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            if (await HasActiveSalesAsync(idSiteTouristiqueJournee, cancellationToken))
            {
                throw new SiteTouristiqueJourneeConflictException(
                    "Impossible de supprimer la journée : des réservations actives (HOLD/CONFIRMED) existent.");
            }

            var hasPendingCommande = await _context.SiteTouristiqueCommandesEnAttente
                .AsNoTracking()
                .AnyAsync(
                    c => c.IdSiteTouristiqueJournee == idSiteTouristiqueJournee,
                    cancellationToken);

            if (hasPendingCommande)
            {
                throw new SiteTouristiqueJourneeConflictException(
                    "Impossible de supprimer la journée : des commandes FlexPay en attente existent.");
            }

            // FK Restrict : CANCELLED/EXPIRED bloquent aussi le hard delete (tickets/paiements liés).
            var hasHistoricalReservations = await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .AnyAsync(r => r.IdSiteTouristiqueJournee == idSiteTouristiqueJournee, cancellationToken);

            if (hasHistoricalReservations)
            {
                throw new SiteTouristiqueJourneeConflictException(
                    "Impossible de supprimer la journée : des réservations historiques (CANCELLED/EXPIRED) existent encore.");
            }

            _context.SiteTouristiqueJournees.Remove(journee);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique supprimée — Id={Id}, Societe={IdSociete}, Statut={Status}",
                idSiteTouristiqueJournee,
                idSociete,
                journee.Status);
        }

        public async Task<SiteTouristiqueJourneeResponseDto> CancelAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            if (journee.Status == SiteTouristiqueStatus.Closed)
            {
                throw new InvalidOperationException(
                    "Impossible d'annuler une journée Closed (déjà clôturée opérationnellement).");
            }

            if (journee.Status == SiteTouristiqueStatus.Cancelled)
                return await LoadJourneeResponseAsync(idSiteTouristiqueJournee, idSociete, cancellationToken);

            journee.Status = SiteTouristiqueStatus.Cancelled;
            journee.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique annulée (soft-delete) — Id={Id}, Societe={IdSociete}",
                idSiteTouristiqueJournee,
                idSociete);

            return await LoadJourneeResponseAsync(idSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        public async Task<SiteTouristiqueJourneeResponseDto> CloseAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var journee = await _context.SiteTouristiqueJournees
                .FirstOrDefaultAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
                throw new KeyNotFoundException($"Journée site touristique {idSiteTouristiqueJournee} introuvable.");

            if (journee.Status == SiteTouristiqueStatus.Cancelled)
            {
                throw new InvalidOperationException(
                    "Impossible de clôturer une journée Cancelled (annulée).");
            }

            if (journee.Status == SiteTouristiqueStatus.Closed)
                return await LoadJourneeResponseAsync(idSiteTouristiqueJournee, idSociete, cancellationToken);

            journee.Status = SiteTouristiqueStatus.Closed;
            journee.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Journée site touristique clôturée — Id={Id}, Societe={IdSociete}",
                idSiteTouristiqueJournee,
                idSociete);

            return await LoadJourneeResponseAsync(idSiteTouristiqueJournee, idSociete, cancellationToken);
        }

        private async Task UpdateDraftAsync(
            SiteTouristiqueJournee journee,
            SiteTouristiqueUpdateJourneeRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            if (request.DateVisite.HasValue)
            {
                var newDate = request.DateVisite.Value;
                if (newDate != journee.DateVisite)
                {
                    var exists = await _context.SiteTouristiqueJournees
                        .AsNoTracking()
                        .AnyAsync(
                            j => j.IdSiteTouristique == journee.IdSiteTouristique
                                 && j.DateVisite == newDate
                                 && j.IdSiteTouristiqueJournee != journee.IdSiteTouristiqueJournee,
                            cancellationToken);

                    if (exists)
                    {
                        throw new SiteTouristiqueJourneeConflictException(
                            $"Une journée existe déjà pour le lieu {journee.IdSiteTouristique} à la date {newDate:yyyy-MM-dd}.");
                    }

                    journee.DateVisite = newDate;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CodeDevise))
                journee.CodeDevise = NormalizeCodeDevise(request.CodeDevise);

            if (TouchesInventory(request, journee.InventoryMode))
                await ApplyInventoryUpdateAsync(journee, request, idSociete, cancellationToken);
        }

        private async Task UpdatePublishedAsync(
            SiteTouristiqueJournee journee,
            SiteTouristiqueUpdateJourneeRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            if (request.DateVisite.HasValue)
            {
                throw new InvalidOperationException(
                    "DateVisite ne peut pas être modifiée sur une journée Published.");
            }

            if (!string.IsNullOrWhiteSpace(request.CodeDevise))
            {
                throw new InvalidOperationException(
                    "CodeDevise ne peut pas être modifié sur une journée Published.");
            }

            if (!TouchesInventory(request, journee.InventoryMode))
                return;

            if (await HasActiveSalesAsync(journee.IdSiteTouristiqueJournee, cancellationToken))
            {
                throw new SiteTouristiqueJourneeConflictException(
                    "Impossible de modifier capacité/prix : des réservations actives existent sur cette journée.");
            }

            await ApplyInventoryUpdateAsync(journee, request, idSociete, cancellationToken);
        }

        private static bool TouchesInventory(
            SiteTouristiqueUpdateJourneeRequestDto request,
            SiteTouristiqueInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                SiteTouristiqueInventoryMode.GlobalQuota => request.GlobalQuota != null,
                SiteTouristiqueInventoryMode.ClassQuota => request.ClassQuotas != null,
                _ => false
            };

        private async Task ApplyInventoryUpdateAsync(
            SiteTouristiqueJournee journee,
            SiteTouristiqueUpdateJourneeRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    ValidateGlobalQuotaCreate(request.GlobalQuota);
                    if (journee.GlobalQuota == null)
                        AttachGlobalQuota(journee, request.GlobalQuota!);
                    else
                    {
                        journee.GlobalQuota.CapaciteTotale = request.GlobalQuota!.CapaciteTotale;
                        journee.GlobalQuota.PrixUnitaire = request.GlobalQuota.PrixUnitaire;
                    }

                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    ValidateClassQuotasCreate(request.ClassQuotas);
                    if (journee.ClassQuotas.Count > 0)
                        _context.SiteTouristiqueClassQuotas.RemoveRange(journee.ClassQuotas);
                    journee.ClassQuotas.Clear();
                    await AttachClassQuotasAsync(journee, request.ClassQuotas!, idSociete, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"InventoryMode {journee.InventoryMode} non supporté pour la mise à jour.");
            }
        }

        private async Task<bool> HasActiveSalesAsync(
            int idSiteTouristiqueJournee,
            CancellationToken cancellationToken) =>
            await _context.SiteTouristiqueReservations
                .AsNoTracking()
                .AnyAsync(
                    r => r.IdSiteTouristiqueJournee == idSiteTouristiqueJournee
                         && (r.Status == SiteTouristiqueReservationStatus.HOLD
                             || r.Status == SiteTouristiqueReservationStatus.CONFIRMED),
                    cancellationToken);

        private static void ApplySalesWindows(
            SiteTouristiqueJournee journee,
            SiteTouristiqueUpdateJourneeRequestDto request)
        {
            if (request.SalesOpenAtUtc.HasValue)
                journee.SalesOpenAtUtc = request.SalesOpenAtUtc;
            if (request.SalesCloseAtUtc.HasValue)
                journee.SalesCloseAtUtc = request.SalesCloseAtUtc;

            ValidateSalesWindow(journee.SalesOpenAtUtc, journee.SalesCloseAtUtc);
        }

        private static void ValidateSalesWindow(DateTime? salesOpenAtUtc, DateTime? salesCloseAtUtc)
        {
            if (salesCloseAtUtc.HasValue
                && salesOpenAtUtc.HasValue
                && salesCloseAtUtc.Value < salesOpenAtUtc.Value)
            {
                throw new InvalidOperationException(
                    "SalesCloseAtUtc doit être postérieur ou égal à SalesOpenAtUtc.");
            }
        }

        private IQueryable<SiteTouristiqueJournee> BuildListQuery(
            int idSociete,
            SiteTouristiqueJourneeListFilter? filter)
        {
            var query = JourneeListQuery()
                .Where(j => j.IdSociete == idSociete);

            if (filter?.Status.HasValue == true)
                query = query.Where(j => j.Status == filter.Status.Value);

            if (filter?.InventoryMode.HasValue == true)
                query = query.Where(j => j.InventoryMode == filter.InventoryMode.Value);

            if (filter?.IdSiteTouristique.HasValue == true && filter.IdSiteTouristique.Value > 0)
                query = query.Where(j => j.IdSiteTouristique == filter.IdSiteTouristique.Value);

            if (filter?.DateVisite.HasValue == true)
                query = query.Where(j => j.DateVisite == filter.DateVisite.Value);

            if (filter?.DateVisiteFrom.HasValue == true)
                query = query.Where(j => j.DateVisite >= filter.DateVisiteFrom.Value);

            if (filter?.DateVisiteTo.HasValue == true)
                query = query.Where(j => j.DateVisite <= filter.DateVisiteTo.Value);

            return query.OrderByDescending(j => j.DateVisite);
        }

        private IQueryable<SiteTouristiqueJournee> JourneeListQuery() =>
            _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Include(j => j.Societe)
                .Include(j => j.Lieu!)
                    .ThenInclude(l => l.Site)
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas);

        private IQueryable<SiteTouristiqueJournee> JourneeDetailQuery() =>
            _context.SiteTouristiqueJournees
                .AsNoTracking()
                .Include(j => j.Societe)
                .Include(j => j.Lieu!)
                    .ThenInclude(l => l.Site)
                .Include(j => j.GlobalQuota)
                .Include(j => j.ClassQuotas)
                    .ThenInclude(q => q.Classe);

        private async Task<SiteTouristiqueJourneeResponseDto> LoadJourneeResponseAsync(
            int idSiteTouristiqueJournee,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var journee = await JourneeDetailQuery()
                .FirstAsync(
                    j => j.IdSiteTouristiqueJournee == idSiteTouristiqueJournee && j.IdSociete == idSociete,
                    cancellationToken);

            return SiteTouristiqueJourneeMapper.ToResponseDto(journee);
        }

        private static void AttachGlobalQuota(
            SiteTouristiqueJournee journee,
            SiteTouristiqueCreateJourneeGlobalQuotaDto global)
        {
            journee.GlobalQuota = new SiteTouristiqueGlobalQuota
            {
                CapaciteTotale = global.CapaciteTotale,
                QuantiteHold = 0,
                QuantiteVendue = 0,
                PrixUnitaire = global.PrixUnitaire
            };
        }

        private async Task AttachClassQuotasAsync(
            SiteTouristiqueJournee journee,
            IReadOnlyList<SiteTouristiqueCreateJourneeClassQuotaDto> classQuotas,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var classeIds = classQuotas.Select(q => q.IdSiteTouristiqueClasse).Distinct().ToList();
            var classes = await _context.SiteTouristiqueClasses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete && classeIds.Contains(c.IdSiteTouristiqueClasse))
                .ToListAsync(cancellationToken);

            if (classes.Count != classeIds.Count)
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont introuvables pour cette société.");
            }

            if (classes.Any(c => !c.Actif))
            {
                throw new InvalidOperationException(
                    "Une ou plusieurs classes référencées sont inactives.");
            }

            foreach (var item in classQuotas)
            {
                journee.ClassQuotas.Add(new SiteTouristiqueClassQuota
                {
                    IdSiteTouristiqueClasse = item.IdSiteTouristiqueClasse,
                    CapaciteTotale = item.CapaciteTotale,
                    QuantiteHold = 0,
                    QuantiteVendue = 0,
                    PrixUnitaire = item.PrixUnitaire
                });
            }
        }

        private static void ValidateCreateRequest(
            SiteTouristiqueCreateJourneeRequestDto request,
            SiteTouristiqueInventoryMode inventoryMode)
        {
            if (request.IdSiteTouristique <= 0)
                throw new InvalidOperationException("IdSiteTouristique est obligatoire.");

            if (request.SalesCloseAtUtc.HasValue
                && request.SalesOpenAtUtc.HasValue
                && request.SalesCloseAtUtc.Value < request.SalesOpenAtUtc.Value)
            {
                throw new InvalidOperationException(
                    "SalesCloseAtUtc doit être postérieur ou égal à SalesOpenAtUtc.");
            }

            switch (inventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    ValidateGlobalQuotaCreate(request.GlobalQuota);
                    break;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    ValidateClassQuotasCreate(request.ClassQuotas);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"InventoryMode {inventoryMode} non supporté pour la création.");
            }
        }

        private static void ValidateGlobalQuotaCreate(SiteTouristiqueCreateJourneeGlobalQuotaDto? global)
        {
            if (global == null)
                throw new InvalidOperationException("GlobalQuota est obligatoire pour InventoryMode GlobalQuota.");

            if (global.CapaciteTotale <= 0)
                throw new InvalidOperationException("CapaciteTotale doit être strictement positive.");

            if (global.PrixUnitaire < 0)
                throw new InvalidOperationException("PrixUnitaire ne peut pas être négatif.");
        }

        private static void ValidateClassQuotasCreate(List<SiteTouristiqueCreateJourneeClassQuotaDto>? classQuotas)
        {
            if (classQuotas == null || classQuotas.Count == 0)
            {
                throw new InvalidOperationException(
                    "ClassQuotas est obligatoire pour InventoryMode ClassQuota (au moins une classe).");
            }

            var duplicateClasse = classQuotas
                .GroupBy(q => q.IdSiteTouristiqueClasse)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicateClasse != null)
            {
                throw new InvalidOperationException(
                    $"ClassQuotas contient un doublon pour IdSiteTouristiqueClasse={duplicateClasse.Key}.");
            }

            foreach (var quota in classQuotas)
            {
                if (quota.CapaciteTotale <= 0)
                {
                    throw new InvalidOperationException(
                        $"CapaciteTotale invalide pour IdSiteTouristiqueClasse={quota.IdSiteTouristiqueClasse}.");
                }

                if (quota.PrixUnitaire < 0)
                {
                    throw new InvalidOperationException(
                        $"PrixUnitaire invalide pour IdSiteTouristiqueClasse={quota.IdSiteTouristiqueClasse}.");
                }
            }
        }

        private static void ValidateInventoryForPublish(SiteTouristiqueJournee journee)
        {
            switch (journee.InventoryMode)
            {
                case SiteTouristiqueInventoryMode.GlobalQuota:
                    if (journee.GlobalQuota == null || journee.GlobalQuota.CapaciteTotale <= 0)
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : quota global manquant ou capacité invalide.");
                    }

                    return;

                case SiteTouristiqueInventoryMode.ClassQuota:
                    if (journee.ClassQuotas.Count == 0
                        || journee.ClassQuotas.All(q => q.CapaciteTotale <= 0))
                    {
                        throw new InvalidOperationException(
                            "Publication impossible : au moins un quota classe valide est requis.");
                    }

                    return;

                default:
                    throw new InvalidOperationException(
                        $"Publication Mode {journee.InventoryMode} : non implémentée.");
            }
        }

        private static SiteTouristiqueInventoryMode ParseInventoryMode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SiteTouristiqueInventoryMode.GlobalQuota;

            if (!Enum.TryParse<SiteTouristiqueInventoryMode>(value.Trim(), ignoreCase: true, out var mode))
            {
                throw new InvalidOperationException(
                    $"InventoryMode invalide : '{value}'. Valeurs : ClassQuota, GlobalQuota.");
            }

            return mode;
        }

        private static string NormalizeCodeDevise(string codeDevise)
        {
            var normalized = string.IsNullOrWhiteSpace(codeDevise)
                ? "CDF"
                : codeDevise.Trim().ToUpperInvariant();

            if (normalized is not ("CDF" or "USD"))
            {
                throw new InvalidOperationException(
                    "CodeDevise invalide. Valeurs acceptées : CDF, USD.");
            }

            return normalized;
        }
    }
}
