using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelPlanificationService : IHotelPlanificationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<HotelPlanificationService> _logger;

        public HotelPlanificationService(
            CongoTravelDbContext context,
            ILogger<HotelPlanificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<HotelPlanificationListItemDto>> ListAsync(
            int idSociete,
            int? idHotel = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.HotelPlanifications.AsNoTracking()
                .Include(p => p.Hotel)
                .Where(p => p.IdSociete == idSociete);

            if (idHotel is > 0)
                query = query.Where(p => p.IdHotel == idHotel.Value);

            var items = await query
                .OrderByDescending(p => p.DateCreation)
                .ToListAsync(cancellationToken);

            var allotmentCounts = await _context.HotelNightAllotments.AsNoTracking()
                .Where(a => a.IdHotelPlanification.HasValue)
                .GroupBy(a => a.IdHotelPlanification!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

            var nightCounts = await _context.HotelNights.AsNoTracking()
                .Where(n => n.IdHotelPlanification.HasValue)
                .GroupBy(n => n.IdHotelPlanification!.Value)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

            return items.Select(p =>
            {
                var count = p.InventoryMode == HotelInventoryMode.GlobalQuota
                    ? nightCounts.GetValueOrDefault(p.IdHotelPlanification)
                    : allotmentCounts.GetValueOrDefault(p.IdHotelPlanification);
                return MapToListItem(p, count);
            }).ToList();
        }

        public async Task<HotelPlanificationResponseDto?> GetByIdAsync(
            int id,
            int? idSociete = null,
            CancellationToken cancellationToken = default)
        {
            var query = DetailQuery().Where(p => p.IdHotelPlanification == id);
            if (idSociete is > 0)
                query = query.Where(p => p.IdSociete == idSociete.Value);

            var entity = await query.FirstOrDefaultAsync(cancellationToken);
            if (entity == null)
                return null;

            var count = entity.InventoryMode == HotelInventoryMode.GlobalQuota
                ? await _context.HotelNights.AsNoTracking()
                    .CountAsync(n => n.IdHotelPlanification == id, cancellationToken)
                : await _context.HotelNightAllotments.AsNoTracking()
                    .CountAsync(a => a.IdHotelPlanification == id, cancellationToken);

            return MapToDetail(entity, count);
        }

        public async Task<HotelPlanificationResponseDto> CreateAsync(
            HotelCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            await ValidateRequestAsync(request, idSociete, cancellationToken);

            var entity = new HotelPlanification
            {
                IdSociete = idSociete,
                IdHotel = request.IdHotel,
                Libelle = request.Libelle.Trim(),
                JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList(),
                InventoryMode = request.InventoryMode,
                CodeDevise = NormalizeCodeDevise(request.CodeDevise),
                Statut = request.Statut,
                DateCreation = DateTime.UtcNow
            };

            AttachQuotas(entity, request);

            _context.HotelPlanifications.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Planification hôtel créée {Id} société {SocieteId} mode {Mode}",
                entity.IdHotelPlanification,
                idSociete,
                entity.InventoryMode);

            return (await GetByIdAsync(entity.IdHotelPlanification, idSociete, cancellationToken))!;
        }

        public async Task<HotelPlanificationResponseDto?> UpdateAsync(
            HotelUpdatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelPlanifications
                .Include(p => p.Lignes)
                .Include(p => p.GlobalQuota)
                .FirstOrDefaultAsync(
                    p => p.IdHotelPlanification == request.IdHotelPlanification
                         && p.IdSociete == idSociete,
                    cancellationToken);

            if (entity == null)
                return null;

            await ValidateRequestAsync(request, idSociete, cancellationToken);

            // Update template only — does not mutate already-generated allotments/nights.
            entity.Libelle = request.Libelle.Trim();
            entity.IdHotel = request.IdHotel;
            entity.JoursSemaine = request.JoursSemaine.Distinct().OrderBy(j => j).ToList();
            entity.InventoryMode = request.InventoryMode;
            entity.CodeDevise = NormalizeCodeDevise(request.CodeDevise);
            entity.Statut = request.Statut;
            entity.DateModification = DateTime.UtcNow;

            if (entity.GlobalQuota != null)
                _context.HotelPlanifGlobalQuotas.Remove(entity.GlobalQuota);
            if (entity.Lignes.Count > 0)
                _context.HotelPlanificationLignes.RemoveRange(entity.Lignes);

            entity.GlobalQuota = null;
            entity.Lignes = new List<HotelPlanificationLigne>();
            AttachQuotas(entity, request);

            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(entity.IdHotelPlanification, idSociete, cancellationToken);
        }

        public async Task<bool> ToggleStatutAsync(int id, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelPlanifications
                .FirstOrDefaultAsync(
                    p => p.IdHotelPlanification == id && p.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return false;

            entity.Statut = !entity.Statut;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int idSociete, CancellationToken cancellationToken = default)
        {
            var entity = await _context.HotelPlanifications
                .Include(p => p.Lignes)
                .Include(p => p.GlobalQuota)
                .FirstOrDefaultAsync(
                    p => p.IdHotelPlanification == id && p.IdSociete == idSociete,
                    cancellationToken);
            if (entity == null)
                return false;

            var allotments = await _context.HotelNightAllotments
                .Where(a => a.IdHotelPlanification == id)
                .ToListAsync(cancellationToken);
            var nights = await _context.HotelNights
                .Where(n => n.IdHotelPlanification == id)
                .ToListAsync(cancellationToken);

            if (allotments.Count > 0 || nights.Count > 0)
            {
                var hasStock =
                    allotments.Any(a => a.QuantiteHold > 0 || a.QuantiteVendue > 0)
                    || nights.Any(n => n.QuantiteHold > 0 || n.QuantiteVendue > 0);
                if (hasStock)
                {
                    entity.Statut = false;
                    entity.DateModification = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    return true;
                }

                if (allotments.Count > 0)
                    _context.HotelNightAllotments.RemoveRange(allotments);
                if (nights.Count > 0)
                    _context.HotelNights.RemoveRange(nights);
            }

            _context.HotelPlanifications.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private IQueryable<HotelPlanification> DetailQuery() =>
            _context.HotelPlanifications.AsNoTracking()
                .Include(p => p.Hotel)
                .Include(p => p.GlobalQuota)
                .Include(p => p.Lignes)
                    .ThenInclude(l => l.RoomType);

        private async Task ValidateRequestAsync(
            HotelCreatePlanificationRequestDto request,
            int idSociete,
            CancellationToken cancellationToken)
        {
            var hotel = await _context.Hotels.AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHotel == request.IdHotel, cancellationToken);
            if (hotel == null)
                throw new ArgumentException($"Hôtel {request.IdHotel} introuvable.");
            if (hotel.IdSociete != idSociete)
                throw new ArgumentException($"L'hôtel {request.IdHotel} n'appartient pas à la société {idSociete}.");

            if (request.InventoryMode == HotelInventoryMode.ClassQuota)
            {
                var roomTypeIds = (request.Lignes ?? new List<HotelCreatePlanificationLigneDto>())
                    .Select(l => l.IdHotelRoomType)
                    .Distinct()
                    .ToList();
                var roomTypes = await _context.HotelRoomTypes.AsNoTracking()
                    .Where(r => roomTypeIds.Contains(r.IdHotelRoomType))
                    .Select(r => new { r.IdHotelRoomType, r.IdHotel, r.IdSociete })
                    .ToListAsync(cancellationToken);

                if (roomTypes.Count != roomTypeIds.Count)
                    throw new ArgumentException("Un ou plusieurs types de chambre sont introuvables.");

                if (roomTypes.Any(r => r.IdSociete != idSociete || r.IdHotel != request.IdHotel))
                    throw new ArgumentException("Tous les types doivent appartenir à l'hôtel et à la société du template.");
            }
            else if (request.InventoryMode == HotelInventoryMode.GlobalQuota)
            {
                if (request.GlobalQuota == null)
                    throw new ArgumentException("GlobalQuota est obligatoire pour InventoryMode GlobalQuota.");
                if (request.GlobalQuota.CapaciteTotale <= 0)
                    throw new ArgumentException("CapaciteTotale doit être strictement positive.");
            }
        }

        private static void AttachQuotas(
            HotelPlanification entity,
            HotelCreatePlanificationRequestDto request)
        {
            switch (request.InventoryMode)
            {
                case HotelInventoryMode.GlobalQuota:
                    entity.GlobalQuota = new HotelPlanifGlobalQuota
                    {
                        CapaciteTotale = request.GlobalQuota!.CapaciteTotale,
                        PrixNuit = request.GlobalQuota.PrixNuit
                    };
                    break;

                case HotelInventoryMode.ClassQuota:
                    foreach (var l in request.Lignes!)
                    {
                        entity.Lignes.Add(new HotelPlanificationLigne
                        {
                            IdHotelRoomType = l.IdHotelRoomType,
                            CapaciteTotale = l.CapaciteTotale,
                            PrixNuit = l.PrixNuit
                        });
                    }
                    break;
            }
        }

        private static string? NormalizeCodeDevise(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var n = value.Trim().ToUpperInvariant();
            if (n.Length != 3)
                throw new ArgumentException("CodeDevise doit contenir 3 caractères.");
            return n;
        }

        private static HotelPlanificationListItemDto MapToListItem(HotelPlanification p, int count) =>
            new()
            {
                IdHotelPlanification = p.IdHotelPlanification,
                IdSociete = p.IdSociete,
                IdHotel = p.IdHotel,
                HotelNom = p.Hotel?.Nom,
                Libelle = p.Libelle,
                JoursSemaine = p.JoursSemaine?.ToList() ?? new List<int>(),
                InventoryMode = p.InventoryMode,
                CodeDevise = p.CodeDevise,
                Statut = p.Statut,
                NombreAllotmentsGeneres = count,
                DateCreation = p.DateCreation,
                DateModification = p.DateModification
            };

        private static HotelPlanificationResponseDto MapToDetail(HotelPlanification p, int count)
        {
            var dto = new HotelPlanificationResponseDto
            {
                IdHotelPlanification = p.IdHotelPlanification,
                IdSociete = p.IdSociete,
                IdHotel = p.IdHotel,
                HotelNom = p.Hotel?.Nom,
                Libelle = p.Libelle,
                JoursSemaine = p.JoursSemaine?.ToList() ?? new List<int>(),
                InventoryMode = p.InventoryMode,
                CodeDevise = p.CodeDevise,
                Statut = p.Statut,
                NombreAllotmentsGeneres = count,
                DateCreation = p.DateCreation,
                DateModification = p.DateModification,
                Lignes = (p.Lignes ?? Array.Empty<HotelPlanificationLigne>())
                    .Select(l => new HotelPlanificationLigneResponseDto
                    {
                        IdHotelPlanificationLigne = l.IdHotelPlanificationLigne,
                        IdHotelRoomType = l.IdHotelRoomType,
                        CodeRoomType = l.RoomType?.Code,
                        LibelleRoomType = l.RoomType?.Libelle,
                        CapaciteTotale = l.CapaciteTotale,
                        PrixNuit = l.PrixNuit
                    }).ToList()
            };

            if (p.GlobalQuota != null)
            {
                dto.GlobalQuota = new HotelPlanificationGlobalQuotaResponseDto
                {
                    CapaciteTotale = p.GlobalQuota.CapaciteTotale,
                    PrixNuit = p.GlobalQuota.PrixNuit
                };
            }

            return dto;
        }
    }
}
