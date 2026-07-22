using AutoMapper;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class ReservationWithPaiementReadService : IReservationWithPaiementReadService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IBilletRepository _billetRepository;
        private readonly IBilletPricingEnrichmentService _billetPricingEnrichment;
        private readonly CongoTravelDbContext _context;
        private readonly IMapper _mapper;

        public ReservationWithPaiementReadService(
            IReservationRepository reservationRepository,
            IBilletRepository billetRepository,
            IBilletPricingEnrichmentService billetPricingEnrichment,
            CongoTravelDbContext context,
            IMapper mapper)
        {
            _reservationRepository = reservationRepository;
            _billetRepository = billetRepository;
            _billetPricingEnrichment = billetPricingEnrichment;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ReservationWithPaiementResponseDto?> BuildByReservationIdAsync(
            int idReservation,
            string? transactionId = null,
            string? message = null,
            CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(idReservation);
            if (reservation == null)
                return null;

            var paiement = await _context.Paiements
                .AsNoTracking()
                .Where(p => p.IdReservation == idReservation)
                .OrderByDescending(p => p.DateCreation)
                .FirstOrDefaultAsync(cancellationToken);
            if (paiement == null)
                return null;

            var passagers = await _reservationRepository.GetPassagersByReservationAsync(idReservation);
            var billets = await _billetRepository.GetByReservationAsync(idReservation);
            var billetsList = billets.ToList();
            var billetsDto = _mapper.Map<List<BilletResponseDto>>(billetsList);
            await _billetPricingEnrichment.EnrichPrixVoyageAsync(billetsList, billetsDto);

            var reservationDto = _mapper.Map<ReservationResponseDto>(reservation);
            reservationDto.Passagers = passagers.Count > 0
                ? _mapper.Map<List<ReservationPassengerReadDto>>(passagers)
                : new List<ReservationPassengerReadDto>();

            var resolvedMessage = message
                ?? (billetsDto.Count == 0
                    ? "Réservation confirmée. Émission billet(s) en attente ou indisponible."
                    : "Réservation créée après confirmation FlexPay.");

            return new ReservationWithPaiementResponseDto
            {
                Reservation = reservationDto,
                Paiement = PaiementResponseMapper.Map(paiement),
                Billets = billetsDto,
                Billet = billetsDto.FirstOrDefault(),
                TransactionId = transactionId ?? paiement.ReferenceTransaction ?? $"RES-{idReservation}",
                Statut = TransactionStatut.Succes,
                Message = resolvedMessage,
                DateCreation = DateTime.UtcNow
            };
        }
    }
}
