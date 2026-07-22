using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Audit de chaque notification callback FlexPay reçue.
    /// </summary>
    public class CallbackFlexPay
    {
        [Key]
        public Guid IdCallback { get; set; } = Guid.NewGuid();

        public Guid? IdTransaction { get; set; }

        [MaxLength(100)]
        public string? OrderNumber { get; set; }

        [MaxLength(10)]
        public string? Code { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(100)]
        public string? ProviderReference { get; set; }

        [MaxLength(50)]
        public string? Amount { get; set; }

        [MaxLength(50)]
        public string? AmountCustomer { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(50)]
        public string? Channel { get; set; }

        [MaxLength(50)]
        public string? CreatedAt { get; set; }

        public string? PayloadComplet { get; set; }

        public string? Headers { get; set; }

        [MaxLength(50)]
        public string? IpSource { get; set; }

        public DateTime DateReception { get; set; } = DateTime.UtcNow;

        public bool TraiteAvecSucces { get; set; }

        [MaxLength(1000)]
        public string? MessageErreur { get; set; }

        public string? DetailsTraitement { get; set; }

        [ForeignKey(nameof(IdTransaction))]
        public TransactionFlexPay? Transaction { get; set; }
    }
}
