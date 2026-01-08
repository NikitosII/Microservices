namespace Payment.API.Models
{
    public class PaymentCard
    {
        public string Number { get; set; } = string.Empty;
        public string ExpiryMonth { get; set; } = string.Empty;
        public string ExpiryYear { get; set; } = string.Empty;
        public string Cvc { get; set; } = string.Empty;
        public string CardholderName { get; set; } = string.Empty;
    }
}
