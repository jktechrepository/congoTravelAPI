using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Helpers.Hotel;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Hotel.Strategies;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelHoldService : IHotelHoldService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelInventoryHoldStrategyFactory _holdStrategyFactory;
        private readonly ICurrentUserService? _currentUser;
        private readonly ILogger<HotelHoldService> _logger;

        public HotelHoldService(CongoTravelDbContext context, IHotelInventoryHoldStrategyFactory holdStrategyFactory,
            ILogger<HotelHoldService> logger, ICurrentUserService? currentUser = null)
        {
            _context = context; _holdStrategyFactory = holdStrategyFactory; _logger = logger; _currentUser = currentUser;
        }

        public async Task<HotelHoldResponseDto> CreateHoldAsync(int idHotel, int idSociete,
            HotelHoldRequestDto request, CancellationToken cancellationToken = default)
        {
            var checkIn = request.CheckInDate.Date;
            var checkOut = request.CheckOutDate.Date;
            if (checkOut <= checkIn)
                throw new InvalidOperationException("CheckOutDate doit être postérieur à CheckInDate.");
            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Au moins un item est requis.");
            var inventoryMode = HotelInventoryModeResolver.FromHoldItems(request.Items);
            var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
            if (key != null)
            {
                var existing = await _context.HotelReservations.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.IdSociete == idSociete && r.IdempotencyKey == key, cancellationToken);
                if (existing != null) return HotelReservationMapper.ToHold(existing);
            }

            var hotel = await _context.Hotels.FirstOrDefaultAsync(
                h => h.IdHotel == idHotel && h.IdSociete == idSociete, cancellationToken)
                ?? throw new KeyNotFoundException($"Hôtel {idHotel} introuvable pour la société {idSociete}.");
            if (hotel.Status != HotelStatus.Published)
                throw new InvalidOperationException("L'hôtel doit être publié pour créer un hold.");

            await using var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken) : null;
            try
            {
                var strategy = _holdStrategyFactory.GetStrategy(inventoryMode);
                var result = await strategy.ReserveHoldAsync(
                    idHotel, idSociete, checkIn, checkOut, request.Items, cancellationToken);
                var config = await _context.ConfigSocietes.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IdSociete == idSociete, cancellationToken);
                var holdMinutes = Math.Clamp(
                    config?.DureeHoldHotelMinutes ?? ConfigSocieteDefaults.DureeHoldHotelMinutes, 1, 120);
                var now = DateTime.UtcNow;
                var percent = Math.Clamp(hotel.AcomptePourcentDefaut, 0m, 100m);
                var reference = $"HTL-{idSociete}-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
                var reservation = new HotelReservation
                {
                    IdSociete = idSociete,
                    IdHotel = idHotel,
                    IdSite = request.IdSite ?? hotel.IdSite,
                    IdUtilisateur = _currentUser?.UserId > 0 ? _currentUser.UserId : null,
                    IdClient = request.IdClient,
                    ReferenceReservation = reference.Length <= 64 ? reference : reference.Substring(0, 64),
                    CustomerRef = string.IsNullOrWhiteSpace(request.CustomerRef) ? null : request.CustomerRef.Trim(),
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    NombreNuits = (checkOut - checkIn).Days,
                    Status = HotelReservationStatus.HOLD,
                    ExpiresAtUtc = now.AddMinutes(holdMinutes),
                    MontantSejour = result.MontantSejour,
                    MontantSousTotal = decimal.Round(result.MontantSejour * percent / 100m, 2),
                    CodeDevise = result.CodeDevise,
                    InventoryMode = inventoryMode,
                    IdempotencyKey = key,
                    DateCreation = now
                };
                if (reservation.IdClient is null && reservation.IdUtilisateur is > 0)
                    reservation.IdClient = await _context.Utilisateurs.AsNoTracking()
                        .Where(u => u.IdUtilisateur == reservation.IdUtilisateur)
                        .Select(u => u.IdClient).FirstOrDefaultAsync(cancellationToken);
                foreach (var line in result.Lines)
                    reservation.Lines.Add(new HotelReservationLine
                    {
                        LineType = line.LineType,
                        IdHotelRoomType = line.IdHotelRoomType,
                        IdHotelNight = line.IdHotelNight,
                        Quantity = line.Quantity,
                        PrixSejourUnitaire = line.PrixSejourUnitaire,
                        MontantLigne = line.MontantLigne,
                        CodeDevise = line.CodeDevise
                    });
                _context.HotelReservations.Add(reservation);
                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null) await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Hold hôtel créé {ReservationId} pour {Nights} nuit(s).",
                    reservation.IdHotelReservation, reservation.NombreNuits);
                return HotelReservationMapper.ToHold(reservation);
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
