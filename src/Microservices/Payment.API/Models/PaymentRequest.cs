namespace Payment.API.Models
{
    public class PaymentRequest
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public PaymentMethod Method { get; set; }
        public string? CustomerEmail { get; set; }
        public string? Description { get; set; }
        public PaymentCard? CardDetails { get; set; }
    }
}