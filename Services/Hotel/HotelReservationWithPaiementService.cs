using CongoTravel.Data;
using CongoTravel.Models.DTOs.Hotel;
using CongoTravel.Models.Hotel.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services.Hotel
{
    public class HotelReservationWithPaiementService : IHotelReservationWithPaiementService
    {
        private readonly CongoTravelDbContext _context;
        private readonly IHotelHoldService _holds;
        private readonly IHotelPaymentService _payments;
        private readonly IHotelReservationService _reservations;
        private readonly IServiceProvider? _services;
        private readonly ICurrentUserService? _currentUser;

        public HotelReservationWithPaiementService(CongoTravelDbContext context,
            IHotelHoldService holds, IHotelPaymentService payments,
            IHotelReservationService reservations, ICurrentUserService? currentUser = null,
            IServiceProvider? services = null)
        {
            _context = context; _holds = holds; _payments = payments;
            _reservations = reservations; _currentUser = currentUser;
            _services = services;
        }

        public Task<HotelReservationWithPaiementResponseDto> InitiateElectronicAsync(
            HotelReservationWithPaiementRequestDto request, CancellationToken cancellationToken = default) =>
            (_services?.GetService<IHotelCommandeFlexPayService>()
             ?? throw new InvalidOperationException("Le service FlexPay hôtel n'est pas configuré."))
                .InitiateElectronicAsync(request, cancellationToken);

        public async Task<HotelReservationWithPaiementResponseDto> CreateCashAsync(
            HotelReservationWithPaiementRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var method = request.Paiement?.MethodePaiement?.Trim();
            if (!string.Equals(method, "CASH", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(method, "ESPECES", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cet endpoint hôtel accepte uniquement CASH (ou espèces).");
            var hotel = await _context.Hotels.AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHotel == request.IdHotel
                    && h.Status == HotelStatus.Published, cancellationToken)
                ?? throw new KeyNotFoundException($"Hôtel {request.IdHotel} introuvable ou non publié.");
            if (_currentUser?.IsStaff == true && !_currentUser.IsSuperAdmin
                && _currentUser.SocieteId != hotel.IdSociete)
                throw new UnauthorizedAccessException("Cet hôtel n'appartient pas à la société du JWT.");

            var hold = await _holds.CreateHoldAsync(request.IdHotel, hotel.IdSociete,
                new HotelHoldRequestDto
                {
                    CheckInDate = request.CheckInDate,
                    CheckOutDate = request.CheckOutDate,
                    CustomerRef = request.CustomerRef,
                    IdClient = request.IdClient,
                    IdSite = request.Paiement.IdSite ?? hotel.IdSite,
                    IdempotencyKey = request.IdempotencyKey,
                    Items = request.Items
                }, cancellationToken);
            try
            {
                var confirmed = await _payments.ConfirmPaymentAsync(
                    hold.IdHotelReservation, hotel.IdSociete,
                    new HotelConfirmPaymentRequestDto
                    {
                        MethodePaiement = "CASH",
                        ReferenceTransaction = request.Paiement.ReferenceTransaction,
                        IdempotencyKey = !string.IsNullOrWhiteSpace(request.Paiement.IdempotencyKey)
                            ? request.Paiement.IdempotencyKey
                            : string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey + ":pay"
                    }, cancellationToken);
                return new()
                {
                    Reservation = confirmed.Reservation,
                    Payment = confirmed.Payment,
                    AlreadyConfirmed = confirmed.AlreadyConfirmed,
                    TransactionStatut = "Succes",
                    Message = confirmed.AlreadyConfirmed
                        ? "Réservation déjà confirmée (idempotent)."
                        : "Réservation hôtel confirmée et acompte CASH encaissé."
                };
            }
            catch
            {
                await _reservations.CancelAsync(hold.IdHotelReservation, hotel.IdSociete, cancellationToken);
                throw;
            }
        }
    }
}
