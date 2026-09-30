using snap_test.Helpers;

namespace snap_test.Models
{
    public class Payment : IEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Method { get; set; } = string.Empty;   // card | upi | paypal | bank_transfer
        public string Status { get; set; } = string.Empty;   // succeeded | pending | failed | refunded
        public string Description { get; set; } = string.Empty;
        public string? CardLast4 { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? RefundedAt { get; set; }
    }

    public class CreatePaymentRequest
    {
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CardLast4 { get; set; }
    }
}
