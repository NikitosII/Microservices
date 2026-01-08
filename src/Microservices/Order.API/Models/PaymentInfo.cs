namespace Order.API.Models
{
    public class PaymentInfo
    {
        public string Method { get; set; } = "CreditCard";
        public string? TransactionId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}