namespace Payment.API.Models
{
    public enum PaymentMethod
    {
        CreditCard,
        PayPal,
        BankTransfer,
        CashOnDelivery
    }

    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Refunded
    }

    public class Payments
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; }
        public string? TransactionId { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}