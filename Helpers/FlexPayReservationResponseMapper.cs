using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Reservation;
using CommonBilletResponseDto = CongoTravel.Models.DTOs.BilletResponseDto;

namespace CongoTravel.Helpers
{
    public static class FlexPayReservationResponseMapper
    {
        public static ReservationWithPaiementResponseDto MapInitiation(
            InitiateFlexPayReservationDto request,
            Paiement paiement,
            CommandeReservationEnAttente commande,
            string orderNumber,
            string? paymentUrl,
            bool flexPayAccepted,
            string message)
        {
            var now = DateTime.UtcNow;
            var idUtilisateur = request.Reservation.IdUtilisateur > 0
                ? request.Reservation.IdUtilisateur
                : commande.IdUtilisateur;

            return new ReservationWithPaiementResponseDto
            {
                TransactionId = orderNumber,
                Statut = TransactionStatut.EnAttente,
                Message = message,
                DateCreation = now,
                Reservation = new ReservationResponseDto
                {
                    IdReservation = 0,
                    IdVoyage = request.Reservation.IdVoyage,
                    IdClient = request.Reservation.IdClient,
                    IdUtilisateur = idUtilisateur,
                    IdSociete = request.Reservation.IdSociete > 0
                        ? request.Reservation.IdSociete
                        : commande.IdSociete,
                    IdSite = request.Reservation.IdSite ?? commande.IdSite,
                    StatutReservation = "EN_ATTENTE_PAIEMENT",
                    Statut = false,
                    DateReservation = now,
                    DateCreation = now,
                    Origine = commande.Origine,
                    Passagers = MapPassagersPreview(request)
                },
                Paiement = PaiementResponseMapper.Map(paiement),
                Billets = new List<CommonBilletResponseDto>(),
                Billet = null,
                IdCommandeReservationEnAttente = commande.IdCommandeReservationEnAttente,
                OrderNumberFlexPay = orderNumber,
                ReferenceFlexPay = commande.ReferenceFlexPay,
                MontantVoyage = commande.MontantVoyage,
                CodeDeviseVoyage = commande.CodeDeviseVoyage,
                MontantFlexPay = commande.MontantFlexPay,
                CodeDevisePaiement = commande.CodeDevisePaiement,
                TauxApplique = commande.TauxVersDevisePaiement,
                HoldExpireAt = commande.DateExpiration,
                PaymentUrl = paymentUrl,
                FlexPayAccepted = flexPayAccepted
            };
        }

        private static List<ReservationPassengerReadDto> MapPassagersPreview(InitiateFlexPayReservationDto request)
        {
            if (request.Reservation.Passagers == null || request.Reservation.Passagers.Count == 0)
                return new List<ReservationPassengerReadDto>();

            return request.Reservation.Passagers.Select(p => new ReservationPassengerReadDto
            {
                IdClient = p.IdClient ?? request.Reservation.IdClient,
                NomComplet = p.NomComplet ?? string.Empty,
                Telephone = p.Telephone,
                Email = p.Email,
                DocumentType = p.DocumentType,
                DocumentNumero = p.DocumentNumero,
                Genre = p.Genre,
                IdSociete = request.Reservation.IdSociete > 0 ? request.Reservation.IdSociete : 0,
                Statut = true
            }).ToList();
        }
    }
}
