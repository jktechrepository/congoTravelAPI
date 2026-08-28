using CongoTravel.Models;
using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.Reservation;

namespace CongoTravel.Services
{
    public interface IAllerRetourReservationService
    {
        Task<ReservationAllerRetourWithPaiementResponseDto> CreateCashAsync(
            CreateReservationAllerRetourWithPaiementDto dto);

        Task<ReservationAllerRetourWithPaiementResponseDto> InitiateFlexPayAsync(
            InitiateFlexPayReservationAllerRetourDto dto,
            CancellationToken cancellationToken = default);

        Task<ReservationAllerRetourResponseDto?> GetByIdAsync(
            int idReservationAllerRetour,
            CancellationToken cancellationToken = default);

        Task<ReservationAllerRetourResponseDto> CancelAsync(
            int idReservationAllerRetour,
            CancellationToken cancellationToken = default);

        /// <summary>Finalisation callback FlexPay pour TypeCommande=AllerRetour.</summary>
        Task<(int IdReservationAller, int IdPaiement, ReservationAllerRetour Agregat)> FinalizeFlexPaySuccessAsync(
            CommandeReservationEnAttente commande,
            Paiement paiement,
            TransactionFlexPay? transaction,
            FlexPayCallbackDto callback,
            CancellationToken cancellationToken);
    }
}
