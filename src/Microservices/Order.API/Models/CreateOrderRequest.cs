namespace Order.API.Models
{
    public class CreateOrderRequest
    {
        public ShippingAddress ShippingAddress { get; set; } = new();
        public string PaymentMethod { get; set; } = "CreditCard";
        public string? CouponCode { get; set; }
    }
}