using CongoTravel.Data;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel.Strategies;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelReservationService : IHotelReservationService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelInventoryCancelStrategyFactory _cancelStrategyFactory;
        public HotelReservationService(CongoTravelDbContext context,
            IHotelInventoryCancelStrategyFactory cancelStrategyFactory)
        {
            _context = context; _cancelStrategyFactory = cancelStrategyFactory;
        }

        public async Task<HotelReservationResponseDto?> GetByIdAsync(int id, int idSociete,
            CancellationToken cancellationToken = default)
        {
            var value = await Query(true).FirstOrDefaultAsync(
                r => r.IdHotelReservation == id && r.IdSociete == idSociete, cancellationToken);
            return value == null ? null : HotelReservationMapper.ToResponse(value);
        }

        public async Task<IReadOnlyList<HotelReservationListItemDto>> ListAsync(int idSociete,
            HotelReservationListFilter? filter = null, CancellationToken cancellationToken = default)
        {
            var query = Query(true).Where(r => r.IdSociete == idSociete);
            return await ListQueryAsync(query, filter, cancellationToken);
        }

        public async Task<IReadOnlyList<HotelReservationListItemDto>> ListByClientAsync(
            int idClient,
            HotelReservationListFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            if (idClient <= 0)
                throw new ArgumentException("idClient doit être strictement positif.", nameof(idClient));

            var query = Query(true).Where(r => r.IdClient == idClient);
            return await ListQueryAsync(query, filter, cancellationToken);
        }

        private static async Task<IReadOnlyList<HotelReservationListItemDto>> ListQueryAsync(
            IQueryable<HotelReservation> query,
            HotelReservationListFilter? filter,
            CancellationToken cancellationToken)
        {
            if (filter?.Status != null) query = query.Where(r => r.Status == filter.Status);
            if (filter?.IdHotel != null) query = query.Where(r => r.IdHotel == filter.IdHotel);
            if (filter?.IdUtilisateur != null) query = query.Where(r => r.IdUtilisateur == filter.IdUtilisateur);
            if (filter?.IdClient != null) query = query.Where(r => r.IdClient == filter.IdClient);
            var rows = await query.OrderByDescending(r => r.DateCreation).ToListAsync(cancellationToken);
            return rows.Select(r =>
            {
                var dto = HotelReservationMapper.ToResponse(r);
                return new HotelReservationListItemDto
                {
                    IdHotelReservation = dto.IdHotelReservation, IdSociete = dto.IdSociete,
                    IdHotel = dto.IdHotel, IdSite = dto.IdSite, IdUtilisateur = dto.IdUtilisateur,
                    IdClient = dto.IdClient, ReferenceReservation = dto.ReferenceReservation,
                    CustomerRef = dto.CustomerRef, CheckInDate = dto.CheckInDate,
                    CheckOutDate = dto.CheckOutDate, NombreNuits = dto.NombreNuits,
                    Status = dto.Status, ExpiresAtUtc = dto.ExpiresAtUtc,
                    CheckedInAtUtc = dto.CheckedInAtUtc, CheckedOutAtUtc = dto.CheckedOutAtUtc,
                    MontantSejour = dto.MontantSejour, MontantSousTotal = dto.MontantSousTotal,
                    CodeDevise = dto.CodeDevise, InventoryMode = dto.InventoryMode,
                    DateCreation = dto.DateCreation,
                    DateModification = dto.DateModification, Lines = dto.Lines, Payments = dto.Payments,
                    RoomAssignments = dto.RoomAssignments,
                    Extras = dto.Extras,
                    MontantExtras = dto.MontantExtras
                };
            }).ToList();
        }

        public async Task<HotelReservationResponseDto> AssignRoomsAsync(
            int idHotelReservation, int idSociete, HotelAssignRoomsRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins une attribution est requise.");

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                var reservation = await Query(false).FirstOrDefaultAsync(
                    r => r.IdHotelReservation == idHotelReservation && r.IdSociete == idSociete,
                    cancellationToken)
                    ?? throw new KeyNotFoundException($"Réservation hôtel {idHotelReservation} introuvable.");

                if (reservation.Status != HotelReservationStatus.CONFIRMED)
                {
                    throw new InvalidOperationException(
                        $"Attribution possible uniquement sur une réservation CONFIRMED (statut actuel : {reservation.Status}).");
                }

                var lineById = reservation.Lines.ToDictionary(l => l.IdHotelReservationLine);
                var roomIds = request.Items.Select(i => i.IdHotelRoom).Distinct().ToList();
                if (roomIds.Count != request.Items.Count)
                    throw new InvalidOperationException("Une même chambre ne peut être attribuée qu'une fois sur la réservation.");

                var rooms = await _context.HotelRooms
                    .Where(r => roomIds.Contains(r.IdHotelRoom) && r.IdSociete == idSociete)
                    .ToListAsync(cancellationToken);
                if (rooms.Count != roomIds.Count)
                    throw new KeyNotFoundException("Une ou plusieurs chambres sont introuvables.");

                var roomById = rooms.ToDictionary(r => r.IdHotelRoom);
                var countsByLine = new Dictionary<int, int>();

                foreach (var item in request.Items)
                {
                    if (!lineById.TryGetValue(item.IdHotelReservationLine, out var line))
                    {
                        throw new InvalidOperationException(
                            $"La ligne {item.IdHotelReservationLine} n'appartient pas à la réservation.");
                    }

                    var room = roomById[item.IdHotelRoom];
                    if (room.IdHotel != reservation.IdHotel)
                        throw new InvalidOperationException($"La chambre {room.Numero} n'appartient pas à l'hôtel de la réservation.");
                    if (!room.IsActif)
                        throw new InvalidOperationException($"La chambre {room.Numero} est inactive.");

                    if (line.LineType == HotelReservationLineType.ClassQuota
                        && line.IdHotelRoomType is int expectedType
                        && room.IdHotelRoomType != expectedType)
                    {
                        throw new InvalidOperationException(
                            $"La chambre {room.Numero} n'est pas du type attendu pour la ligne {line.IdHotelReservationLine}.");
                    }

                    countsByLine[item.IdHotelReservationLine] =
                        countsByLine.GetValueOrDefault(item.IdHotelReservationLine) + 1;
                }

                foreach (var line in reservation.Lines)
                {
                    var assigned = countsByLine.GetValueOrDefault(line.IdHotelReservationLine);
                    if (assigned != line.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"La ligne {line.IdHotelReservationLine} attend {line.Quantity} chambre(s), reçu {assigned}.");
                    }
                }

                var checkIn = reservation.CheckInDate.Date;
                var checkOut = reservation.CheckOutDate.Date;
                var overlap = await (
                    from a in _context.HotelRoomAssignments.AsNoTracking()
                    join res in _context.HotelReservations.AsNoTracking()
                        on a.IdHotelReservation equals res.IdHotelReservation
                    where roomIds.Contains(a.IdHotelRoom)
                          && a.IdHotelReservation != reservation.IdHotelReservation
                          && res.Status == HotelReservationStatus.CONFIRMED
                          && res.CheckInDate < checkOut
                          && checkIn < res.CheckOutDate
                    select new { a.IdHotelRoom, res.IdHotelReservation }
                ).FirstOrDefaultAsync(cancellationToken);

                if (overlap != null)
                {
                    var numero = roomById[overlap.IdHotelRoom].Numero;
                    throw new HotelRoomAssignmentConflictException(
                        $"La chambre {numero} est déjà attribuée à la réservation {overlap.IdHotelReservation} sur une période chevauchante.");
                }

                var existing = await _context.HotelRoomAssignments
                    .Where(a => a.IdHotelReservation == reservation.IdHotelReservation)
                    .ToListAsync(cancellationToken);
                if (existing.Count > 0)
                    _context.HotelRoomAssignments.RemoveRange(existing);

                var now = DateTime.UtcNow;
                foreach (var item in request.Items)
                {
                    _context.HotelRoomAssignments.Add(new HotelRoomAssignment
                    {
                        IdHotelReservation = reservation.IdHotelReservation,
                        IdHotelReservationLine = item.IdHotelReservationLine,
                        IdHotelRoom = item.IdHotelRoom,
                        DateAttributionUtc = now
                    });
                }

                reservation.DateModification = now;
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);

                var refreshed = await Query(true).FirstAsync(
                    r => r.IdHotelReservation == reservation.IdHotelReservation, cancellationToken);
                return HotelReservationMapper.ToResponse(refreshed);
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<HotelReservationResponseDto> CheckInAsync(
            int idHotelReservation, int idSociete, CancellationToken cancellationToken = default)
        {
            var reservation = await Query(false).FirstOrDefaultAsync(
                r => r.IdHotelReservation == idHotelReservation && r.IdSociete == idSociete,
                cancellationToken)
                ?? throw new KeyNotFoundException($"Réservation hôtel {idHotelReservation} introuvable.");

            if (reservation.Status != HotelReservationStatus.CONFIRMED)
            {
                throw new InvalidOperationException(
                    $"Check-in possible uniquement sur une réservation CONFIRMED (statut actuel : {reservation.Status}).");
            }

            if (reservation.CheckedInAtUtc == null)
            {
                reservation.CheckedInAtUtc = DateTime.UtcNow;
                reservation.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return HotelReservationMapper.ToResponse(
                await Query(true).FirstAsync(r => r.IdHotelReservation == idHotelReservation, cancellationToken));
        }

        public async Task<HotelReservationResponseDto> CheckOutAsync(
            int idHotelReservation, int idSociete, CancellationToken cancellationToken = default)
        {
            var reservation = await Query(false).FirstOrDefaultAsync(
                r => r.IdHotelReservation == idHotelReservation && r.IdSociete == idSociete,
                cancellationToken)
                ?? throw new KeyNotFoundException($"Réservation hôtel {idHotelReservation} introuvable.");

            if (reservation.Status != HotelReservationStatus.CONFIRMED)
            {
                throw new InvalidOperationException(
                    $"Check-out possible uniquement sur une réservation CONFIRMED (statut actuel : {reservation.Status}).");
            }

            if (reservation.CheckedInAtUtc == null)
                throw new InvalidOperationException("Check-out impossible : la réservation n'est pas check-in.");

            if (reservation.CheckedOutAtUtc == null)
            {
                reservation.CheckedOutAtUtc = DateTime.UtcNow;
                reservation.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return HotelReservationMapper.ToResponse(
                await Query(true).FirstAsync(r => r.IdHotelReservation == idHotelReservation, cancellationToken));
        }

        public async Task<HotelReservationResponseDto> SetExtrasAsync(
            int idHotelReservation, int idSociete, HotelSetReservationExtrasRequestDto request,
            CancellationToken cancellationToken = default)
        {
            request.Items ??= new List<HotelSetReservationExtraItemDto>();
            var extraIds = request.Items.Select(i => i.IdHotelExtra).ToList();
            if (extraIds.Count != extraIds.Distinct().Count())
                throw new InvalidOperationException("Un même extra ne peut apparaître qu'une fois dans la liste.");

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                var reservation = await Query(false).FirstOrDefaultAsync(
                    r => r.IdHotelReservation == idHotelReservation && r.IdSociete == idSociete,
                    cancellationToken)
                    ?? throw new KeyNotFoundException($"Réservation hôtel {idHotelReservation} introuvable.");

                if (reservation.Status != HotelReservationStatus.CONFIRMED)
                {
                    throw new InvalidOperationException(
                        $"Extras possibles uniquement sur une réservation CONFIRMED (statut actuel : {reservation.Status}).");
                }

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        throw new InvalidOperationException("Quantity doit être strictement positive.");
                }

                var existing = await _context.HotelReservationExtras
                    .Where(e => e.IdHotelReservation == reservation.IdHotelReservation)
                    .ToListAsync(cancellationToken);
                if (existing.Count > 0)
                    _context.HotelReservationExtras.RemoveRange(existing);

                if (request.Items.Count > 0)
                {
                    var extras = await _context.HotelExtras
                        .Where(e => extraIds.Contains(e.IdHotelExtra) && e.IdSociete == idSociete)
                        .ToListAsync(cancellationToken);
                    if (extras.Count != extraIds.Count)
                        throw new KeyNotFoundException("Un ou plusieurs extras sont introuvables.");

                    var extraById = extras.ToDictionary(e => e.IdHotelExtra);
                    foreach (var item in request.Items)
                    {
                        var extra = extraById[item.IdHotelExtra];
                        if (extra.IdHotel != reservation.IdHotel)
                        {
                            throw new InvalidOperationException(
                                $"L'extra {extra.Code} n'appartient pas à l'hôtel de la réservation.");
                        }

                        if (!extra.IsActif)
                            throw new InvalidOperationException($"L'extra {extra.Code} est inactif.");

                        var montantLigne = extra.PricingUnit == HotelExtraPricingUnit.PerNight
                            ? extra.PrixUnitaire * item.Quantity * reservation.NombreNuits
                            : extra.PrixUnitaire * item.Quantity;

                        _context.HotelReservationExtras.Add(new HotelReservationExtra
                        {
                            IdHotelReservation = reservation.IdHotelReservation,
                            IdHotelExtra = extra.IdHotelExtra,
                            Quantity = item.Quantity,
                            PrixUnitaireSnapshot = extra.PrixUnitaire,
                            MontantLigne = montantLigne,
                            CodeDevise = extra.CodeDevise
                        });
                    }
                }

                reservation.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);

                var refreshed = await Query(true).FirstAsync(
                    r => r.IdHotelReservation == reservation.IdHotelReservation, cancellationToken);
                return HotelReservationMapper.ToResponse(refreshed);
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<HotelCancelReservationResponseDto> CancelAsync(int id, int idSociete,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                var reservation = await Query(false).FirstOrDefaultAsync(
                    r => r.IdHotelReservation == id && r.IdSociete == idSociete, cancellationToken)
                    ?? throw new KeyNotFoundException($"Réservation hôtel {id} introuvable.");
                if (reservation.Status == HotelReservationStatus.CANCELLED)
                    return new() { Reservation = HotelReservationMapper.ToResponse(reservation), AlreadyCancelled = true };
                if (reservation.Status is not (HotelReservationStatus.HOLD or HotelReservationStatus.CONFIRMED))
                    throw new InvalidOperationException($"Impossible d'annuler une réservation au statut {reservation.Status}.");
                var confirmed = reservation.Status == HotelReservationStatus.CONFIRMED;
                var cancelStrategy = _cancelStrategyFactory.GetStrategy(reservation.InventoryMode);
                await cancelStrategy.ReleaseReservationAsync(reservation, confirmed, cancellationToken);
                foreach (var payment in reservation.Payments.Where(p => p.Status == HotelPaymentStatus.SUCCEEDED))
                {
                    payment.Status = HotelPaymentStatus.REFUNDED;
                    payment.DateModification = DateTime.UtcNow;
                }

                if (reservation.RoomAssignments.Count > 0)
                    _context.HotelRoomAssignments.RemoveRange(reservation.RoomAssignments);

                if (reservation.ReservationExtras.Count > 0)
                    _context.HotelReservationExtras.RemoveRange(reservation.ReservationExtras);

                reservation.CheckedInAtUtc = null;
                reservation.CheckedOutAtUtc = null;
                reservation.Status = HotelReservationStatus.CANCELLED;
                reservation.ExpiresAtUtc = null;
                reservation.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                return new() { Reservation = HotelReservationMapper.ToResponse(reservation), AlreadyCancelled = false };
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private IQueryable<HotelReservation> Query(bool noTracking)
        {
            IQueryable<HotelReservation> query = _context.HotelReservations;
            if (noTracking) query = query.AsNoTracking();
            return query
                .Include(r => r.Lines)
                .Include(r => r.Payments)
                .Include(r => r.RoomAssignments)
                    .ThenInclude(a => a.Room)
                .Include(r => r.ReservationExtras)
                    .ThenInclude(e => e.Extra);
        }
    }
}
