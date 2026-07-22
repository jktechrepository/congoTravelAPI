using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class CashReservationWithPaiementService : ICashReservationWithPaiementService
    {
        private readonly ReservationWithPaiementService _inner;

        public CashReservationWithPaiementService(ReservationWithPaiementService inner)
        {
            _inner = inner;
        }

        public async Task<ReservationWithPaiementResponseDto> CreateAsync(CreateReservationWithPaiementDto dto)
        {
            MethodePaiementHelper.EnsureCashOnlyForGuichetEndpoint(dto.Paiement.MethodePaiement);
            dto.Paiement.MethodePaiement = MethodePaiementHelper.NormalizeForStorage(dto.Paiement.MethodePaiement);
            return await _inner.CreateReservationWithPaiementAsync(dto);
        }
    }
}
