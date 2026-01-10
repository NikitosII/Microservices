namespace Order.API.Models
{
    public class Orders
    {
        public enum OrderStatus
        {
            Pending,
            Confirmed,
            Processing,
            Shipped,
            Delivered,
            Cancelled,
            Refunded
        }

        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public ShippingAddress ShippingAddress { get; set; } = new();
        public PaymentInfo PaymentInfo { get; set; } = new();
        public string? CouponCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}