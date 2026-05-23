using EventBus.Events;

namespace Order.API.Events
{
    /// <summary>
    /// Published by FulfillOrderConsumer (via MassTransit outbox) when an order is successfully created.
    /// Consumed by Payment.API to create an initial Pending payment record.
    /// OrderItemEvent carries per-line-item details for downstream processing.
    /// </summary>
    public class OrderCreatedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<OrderItemEvent> Items { get; set; } = new();
    }

    public class OrderItemEvent
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
