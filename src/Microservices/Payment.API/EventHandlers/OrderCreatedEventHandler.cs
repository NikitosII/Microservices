using MassTransit;
using Payment.API.Data;
using Payment.API.Events;
using Payment.API.Models;

namespace Payment.API.EventHandlers
{
    public class OrderCreatedEventHandler : IConsumer<OrderCreatedEvent>
    {
        private readonly PaymentContext _context;
        private readonly ILogger<OrderCreatedEventHandler> _logger;

        public OrderCreatedEventHandler(PaymentContext context, ILogger<OrderCreatedEventHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("OrderCreatedEvent received: OrderId={OrderId}, Amount={Amount}", msg.OrderId, msg.TotalAmount);

            var alreadyExists = _context.Payments.Any(p => p.OrderId == msg.OrderId);
            if (alreadyExists)
            {
                _logger.LogWarning("Payment record for OrderId={OrderId} already exists, skipping", msg.OrderId);
                return;
            }

            var payment = new Payments
            {
                Id = Guid.NewGuid(),
                OrderId = msg.OrderId,
                Amount = msg.TotalAmount,
                Currency = "USD",
                Method = PaymentMethod.CreditCard,
                Status = PaymentStatus.Pending,
                CustomerEmail = msg.Email,
                Description = $"Payment for order {msg.OrderNumber}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pending payment record created: PaymentId={PaymentId} for OrderId={OrderId}", payment.Id, msg.OrderId);
        }
    }
}
