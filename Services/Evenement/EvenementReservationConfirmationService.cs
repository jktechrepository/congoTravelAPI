using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Services.Evenement.Strategies;

namespace CongoTravel.Services.Evenement
{
    public class EvenementReservationConfirmationService : IEvenementReservationConfirmationService
    {
        private const int MaxTicketCodeAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly IEvenementInventoryConfirmStrategyFactory _confirmStrategyFactory;
        private readonly ILogger<EvenementReservationConfirmationService> _logger;

        public EvenementReservationConfirmationService(
            CongoTravelDbContext context,
            IEvenementInventoryConfirmStrategyFactory confirmStrategyFactory,
            ILogger<EvenementReservationConfirmationService> logger)
        {
            _context = context;
            _confirmStrategyFactory = confirmStrategyFactory;
            _logger = logger;
        }

        public void EnsureHoldConfirmable(EvenementReservation reservation)
        {
            if (reservation.Status != EvenementReservationStatus.HOLD)
            {
                throw new InvalidOperationException(
                    $"Impossible de confirmer une réservation au statut {reservation.Status}.");
            }

            if (reservation.ExpiresAtUtc.HasValue && reservation.ExpiresAtUtc.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException(
                    "Le hold a expiré. Créez une nouvelle réservation.");
            }

            if (reservation.Lines.Count == 0)
                throw new InvalidOperationException("La réservation ne contient aucune ligne.");
        }

        public async Task ConfirmHoldAndEmitTicketsAsync(
            EvenementReservation reservation,
            EvenementPayment payment,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            EnsureHoldConfirmable(reservation);

            var session = await _context.EvenementSessions
                .Include(s => s.GlobalQuota)
                .FirstOrDefaultAsync(
                    s => s.IdEvenementSession == reservation.IdEvenementSession
                         && s.IdSociete == idSociete,
                    cancellationToken);

            if (session == null)
            {
                throw new InvalidOperationException(
                    "Session associée à la réservation introuvable.");
            }

            var confirmStrategy = _confirmStrategyFactory.GetStrategy(session.InventoryMode);
            await confirmStrategy.ConfirmHoldAsync(
                new EvenementInventoryConfirmRequest
                {
                    Reservation = reservation,
                    Session = session
                },
                cancellationToken);

            var utcNow = DateTime.UtcNow;
            reservation.Status = EvenementReservationStatus.CONFIRMED;
            reservation.ExpiresAtUtc = null;
            reservation.DateModification = utcNow;

            payment.Status = EvenementPaymentStatus.SUCCEEDED;
            payment.DateModification = utcNow;

            if (payment.IdEvenementPayment == 0)
            {
                payment.IdEvenementReservation = reservation.IdEvenementReservation;
                payment.DateCreation = utcNow;
                _context.EvenementPayments.Add(payment);
            }

            await EmitTicketsAsync(reservation, idSociete, utcNow, cancellationToken);

            _logger.LogInformation(
                "Réservation événement confirmée — IdReservation={Id}, Provider={Provider}, Tickets={TicketCount}",
                reservation.IdEvenementReservation,
                payment.Provider,
                reservation.Lines.Sum(l => l.Quantite));
        }

        private async Task EmitTicketsAsync(
            EvenementReservation reservation,
            int idSociete,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            foreach (var line in reservation.Lines)
            {
                for (var i = 0; i < line.Quantite; i++)
                {
                    var ticketCode = await GenerateUniqueTicketCodeAsync(idSociete, cancellationToken);
                    var ticket = new EvenementTicket
                    {
                        IdEvenementReservationLine = line.IdEvenementReservationLine,
                        TicketCode = ticketCode,
                        Status = EvenementTicketStatus.ISSUED,
                        IssuedAtUtc = utcNow
                    };

                    line.Tickets.Add(ticket);
                    _context.EvenementTickets.Add(ticket);
                }
            }
        }

        private async Task<string> GenerateUniqueTicketCodeAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxTicketCodeAttempts; attempt++)
            {
                var candidate = EvenementTicketCodeGenerator.GenerateTicketCodeCandidate(idSociete);
                var exists = await _context.EvenementTickets
                    .AsNoTracking()
                    .AnyAsync(t => t.TicketCode == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer un code ticket événement unique.");
        }
    }
}
