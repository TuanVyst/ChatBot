using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BusinessObject.Enums;

namespace BusinessObject.Entities
{
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [ForeignKey("AccountId")]
        public virtual Account? Account { get; set; }

        [Required]
        public int PlanId { get; set; }

        [ForeignKey("PlanId")]
        public virtual SubscriptionPlan? Plan { get; set; }

        [Required]
        public long OrderCode { get; set; }

        [Required]
        public int Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [MaxLength(100)]
        public string? PayOSTransactionId { get; set; }

        [MaxLength(500)]
        public string? CheckoutUrl { get; set; }

        [MaxLength(2000)]
        public string? QrCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
    }
}
