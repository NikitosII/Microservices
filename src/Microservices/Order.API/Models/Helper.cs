using EventBus.Events;
using ShoppingCart.API.Models;

namespace Order.API.Models
{
    // Helper classes for external API calls
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public decimal TotalPrice { get; set; }
    }

    public class CouponValidationResponse
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Message { get; set; } = string.Empty;
        public Coupon? Coupon { get; set; }
    }

    public class Coupon
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }

    public class OrderStatusUpdatedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
