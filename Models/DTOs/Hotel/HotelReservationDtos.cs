using System.ComponentModel.DataAnnotations;
using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelHoldItemRequestDto
    {
        /// <summary>Requis pour ClassQuota ; null/0 pour GlobalQuota (quantity seule).</summary>
        public int? RoomTypeId { get; set; }
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class HotelHoldRequestDto
    {
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        [MaxLength(100)]
        public string? CustomerRef { get; set; }
        public int? IdSite { get; set; }
        public int? IdClient { get; set; }
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
        [MinLength(1)]
        public List<HotelHoldItemRequestDto> Items { get; set; } = new();
    }

    public class HotelReservationPaiementBlockDto
    {
        [Required, MaxLength(50)]
        public string MethodePaiement { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }
        [MaxLength(30)]
        public string? Phone { get; set; }
        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }
        public int? IdSite { get; set; }
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
    }

    public class HotelReservationWithPaiementRequestDto : HotelHoldRequestDto
    {
        [Range(1, int.MaxValue)]
        public int IdHotel { get; set; }
        [Required]
        public HotelReservationPaiementBlockDto Paiement { get; set; } = new();
    }

    public class HotelReservationLineResponseDto
    {
        public int IdHotelReservationLine { get; set; }
        public string LineType { get; set; } = nameof(HotelReservationLineType.ClassQuota);
        public int? IdHotelRoomType { get; set; }
        public int? IdHotelNight { get; set; }
        public int Quantity { get; set; }
        public decimal PrixSejourUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }

    public class HotelPaymentResponseDto
    {
        public int IdHotelPayment { get; set; }
        public string ReferencePaiement { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderTxRef { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public DateTime DateCreation { get; set; }
    }

    public class HotelReservationResponseDto
    {
        public int IdHotelReservation { get; set; }
        public int IdSociete { get; set; }
        public int IdHotel { get; set; }
        public int? IdSite { get; set; }
        public int? IdUtilisateur { get; set; }
        public int? IdClient { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public string? CustomerRef { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NombreNuits { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? CheckedInAtUtc { get; set; }
        public DateTime? CheckedOutAtUtc { get; set; }
        public decimal MontantSejour { get; set; }
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string InventoryMode { get; set; } = nameof(HotelInventoryMode.ClassQuota);
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
        public List<HotelReservationLineResponseDto> Lines { get; set; } = new();
        public List<HotelPaymentResponseDto> Payments { get; set; } = new();
        public List<HotelRoomAssignmentResponseDto> RoomAssignments { get; set; } = new();
        public List<HotelReservationExtraResponseDto> Extras { get; set; } = new();
        public decimal MontantExtras { get; set; }
    }

    public class HotelReservationListItemDto : HotelReservationResponseDto { }

    public class HotelReservationListFilter
    {
        public HotelReservationStatus? Status { get; set; }
        public int? IdHotel { get; set; }
        public int? IdUtilisateur { get; set; }
        public int? IdClient { get; set; }
    }

    public class HotelHoldResponseDto
    {
        public int IdHotelReservation { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpiresAtUtc { get; set; }
        public decimal MontantSejour { get; set; }
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }

    public class HotelConfirmPaymentRequestDto
    {
        public string MethodePaiement { get; set; } = "CASH";
        public string? ReferenceTransaction { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class HotelConfirmPaymentResponseDto
    {
        public HotelReservationResponseDto Reservation { get; set; } = new();
        public HotelPaymentResponseDto Payment { get; set; } = new();
        public bool AlreadyConfirmed { get; set; }
    }

    public class HotelReservationWithPaiementResponseDto : HotelConfirmPaymentResponseDto
    {
        public string TransactionStatut { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public string? PaymentUrl { get; set; }
        public DateTime? ReservationExpiresAtUtc { get; set; }
        public decimal? MontantFlexPay { get; set; }
        public string? CodeDevisePaiement { get; set; }
        public decimal? MontantTarif { get; set; }
        public string? CodeDeviseTarif { get; set; }
        public decimal? TauxApplique { get; set; }
        public bool? FlexPayAccepted { get; set; }
        public bool AlreadyInitiated { get; set; }
    }

    public class HotelCommandeSnapshotLineDto
    {
        public string LineType { get; set; } = nameof(HotelReservationLineType.ClassQuota);
        public int? IdHotelRoomType { get; set; }
        public int? IdHotelNight { get; set; }
        public int Quantity { get; set; }
        public decimal PrixSejourUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
        public string CodeDevise { get; set; } = "CDF";
    }

    public class HotelCommandeSnapshotDto
    {
        public HotelReservationWithPaiementRequestDto Request { get; set; } = new();
        public List<HotelCommandeSnapshotLineDto> Lines { get; set; } = new();
        public string ReferenceReservation { get; set; } = string.Empty;
        public decimal MontantSejour { get; set; }
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public HotelInventoryMode InventoryMode { get; set; } = HotelInventoryMode.ClassQuota;
    }

    public class HotelFlexPayCallbackProcessResultDto
    {
        public bool Success { get; set; }
        public bool AlreadyProcessed { get; set; }
        public bool PaymentPending { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdHotelReservation { get; set; }
        public int? IdHotelPayment { get; set; }
    }

    public class HotelFlexPayVerifierResultDto
    {
        public HotelConfirmPaymentResponseDto? ConfirmPayment { get; set; }
        public HotelFlexPayCallbackProcessResultDto? StatusOnly { get; set; }
        public bool IsConfirmSuccess => ConfirmPayment != null;
    }

    public class HotelCancelReservationResponseDto
    {
        public HotelReservationResponseDto Reservation { get; set; } = new();
        public bool AlreadyCancelled { get; set; }
    }
}
