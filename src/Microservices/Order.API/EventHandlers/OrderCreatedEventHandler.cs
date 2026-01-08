using EventBus.Events;
using MassTransit;
using Order.API.Models;

namespace Order.API.EventHandlers
{
    public class OrderCreatedEvent : IntegrationEvent
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Email { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class OrderCreatedEventHandler : IConsumer<OrderCreatedEvent>
    {
        private readonly ILogger<OrderCreatedEventHandler> _logger;

        public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            _logger.LogInformation("OrderCreatedEvent received: {OrderId}", context.Message.OrderId);
            await Task.CompletedTask;
        }
    }
}
