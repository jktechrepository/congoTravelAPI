using Microsoft.EntityFrameworkCore;
using CongoTravel.Data;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;
using CongoTravel.Services.SiteTouristique.Strategies;

namespace CongoTravel.Services.SiteTouristique
{
    public class SiteTouristiqueReservationConfirmationService : ISiteTouristiqueReservationConfirmationService
    {
        private const int MaxTicketCodeAttempts = 10;

        private readonly CongoTravelDbContext _context;
        private readonly ISiteTouristiqueInventoryConfirmStrategyFactory _confirmStrategyFactory;
        private readonly ILogger<SiteTouristiqueReservationConfirmationService> _logger;

        public SiteTouristiqueReservationConfirmationService(
            CongoTravelDbContext context,
            ISiteTouristiqueInventoryConfirmStrategyFactory confirmStrategyFactory,
            ILogger<SiteTouristiqueReservationConfirmationService> logger)
        {
            _context = context;
            _confirmStrategyFactory = confirmStrategyFactory;
            _logger = logger;
        }

        public void EnsureHoldConfirmable(SiteTouristiqueReservation reservation)
        {
            if (reservation.Status != SiteTouristiqueReservationStatus.HOLD)
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
            SiteTouristiqueReservation reservation,
            SiteTouristiquePayment payment,
            int idSociete,
            CancellationToken cancellationToken = default)
        {
            EnsureHoldConfirmable(reservation);

            var journee = await _context.SiteTouristiqueJournees
                .Include(s => s.GlobalQuota)
                .FirstOrDefaultAsync(
                    s => s.IdSiteTouristiqueJournee == reservation.IdSiteTouristiqueJournee
                         && s.IdSociete == idSociete,
                    cancellationToken);

            if (journee == null)
            {
                throw new InvalidOperationException(
                    "Session associée à la réservation introuvable.");
            }

            SiteTouristiqueJourneeSalesEligibilityHelper.EnsureCanSell(journee, DateTime.UtcNow);

            var confirmStrategy = _confirmStrategyFactory.GetStrategy(journee.InventoryMode);
            await confirmStrategy.ConfirmHoldAsync(
                new SiteTouristiqueInventoryConfirmRequest
                {
                    Reservation = reservation,
                    Journee = journee
                },
                cancellationToken);

            var utcNow = DateTime.UtcNow;
            reservation.Status = SiteTouristiqueReservationStatus.CONFIRMED;
            reservation.ExpiresAtUtc = null;
            reservation.DateModification = utcNow;

            payment.Status = SiteTouristiquePaymentStatus.SUCCEEDED;
            payment.DateModification = utcNow;

            if (payment.IdSiteTouristiquePayment == 0)
            {
                payment.IdSiteTouristiqueReservation = reservation.IdSiteTouristiqueReservation;
                payment.DateCreation = utcNow;
                _context.SiteTouristiquePayments.Add(payment);
            }

            await EmitTicketsAsync(reservation, idSociete, utcNow, cancellationToken);

            _logger.LogInformation(
                "Réservation site touristique confirmée — IdReservation={Id}, Provider={Provider}, Tickets={TicketCount}",
                reservation.IdSiteTouristiqueReservation,
                payment.Provider,
                reservation.Lines.Sum(l => l.Quantite));
        }

        private async Task EmitTicketsAsync(
            SiteTouristiqueReservation reservation,
            int idSociete,
            DateTime utcNow,
            CancellationToken cancellationToken)
        {
            foreach (var line in reservation.Lines)
            {
                for (var i = 0; i < line.Quantite; i++)
                {
                    var ticketCode = await GenerateUniqueTicketCodeAsync(idSociete, cancellationToken);
                    var ticket = new SiteTouristiqueTicket
                    {
                        IdSiteTouristiqueReservationLine = line.IdSiteTouristiqueReservationLine,
                        TicketCode = ticketCode,
                        Status = SiteTouristiqueTicketStatus.ISSUED,
                        IssuedAtUtc = utcNow
                    };

                    line.Tickets.Add(ticket);
                    _context.SiteTouristiqueTickets.Add(ticket);
                }
            }
        }

        private async Task<string> GenerateUniqueTicketCodeAsync(
            int idSociete,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < MaxTicketCodeAttempts; attempt++)
            {
                var candidate = SiteTouristiqueTicketCodeGenerator.GenerateTicketCodeCandidate(idSociete);
                var exists = await _context.SiteTouristiqueTickets
                    .AsNoTracking()
                    .AnyAsync(t => t.TicketCode == candidate, cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new InvalidOperationException(
                "Impossible de générer un code ticket site touristique unique.");
        }
    }
}
