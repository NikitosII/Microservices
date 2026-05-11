using EventBus.Messages;
using MassTransit;
using Order.API.Data;
using Order.API.Events;
using Order.API.Models;

namespace Order.API.Consumers;

public class FulfillOrderConsumer : IConsumer<FulfillOrderCommand>
{
    private readonly OrderContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FulfillOrderConsumer> _logger;

    public FulfillOrderConsumer(
        OrderContext context,
        IPublishEndpoint publishEndpoint,
        IHttpClientFactory httpClientFactory,
        ILogger<FulfillOrderConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FulfillOrderCommand> context)
    {
        var msg = context.Message;

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var subtotal = msg.SubTotal;
            var tax = subtotal * 0.1m;
            var shippingCost = msg.ShippingAddress.Country.Equals("USA", StringComparison.OrdinalIgnoreCase)
                ? 10.00m : 25.00m;
            var totalAmount = subtotal + tax + shippingCost - msg.Discount;

            var order = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = msg.UserId,
                OrderNumber = GenerateOrderNumber(),
                Status = Orders.OrderStatus.Confirmed,
                Items = msg.Items.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    TotalPrice = i.UnitPrice * i.Quantity
                }).ToList(),
                Subtotal = subtotal,
                Tax = tax,
                ShippingCost = shippingCost,
                Discount = msg.Discount,
                TotalAmount = totalAmount,
                ShippingAddress = new ShippingAddress
                {
                    FullName = msg.ShippingAddress.FullName,
                    Street = msg.ShippingAddress.Street,
                    City = msg.ShippingAddress.City,
                    State = msg.ShippingAddress.State,
                    Country = msg.ShippingAddress.Country,
                    ZipCode = msg.ShippingAddress.ZipCode,
                    Phone = msg.ShippingAddress.Phone
                },
                PaymentInfo = new PaymentInfo { Method = msg.PaymentMethod, AmountPaid = totalAmount },
                CouponCode = msg.CouponCode,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            // Published through the outbox — atomic with the DB write.
            await _publishEndpoint.Publish(new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Email = msg.Email,
                Items = order.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice
                }).ToList()
            });

            await _publishEndpoint.Publish(new OrderFulfilledEvent
            {
                CorrelationId = msg.CorrelationId,
                OrderId = order.Id,
                TotalAmount = order.TotalAmount
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Order fulfilled for saga {CorrelationId}, order {OrderId}", msg.CorrelationId, order.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Order fulfillment failed for saga {CorrelationId}", msg.CorrelationId);
            await context.Publish(new OrderFulfillmentFailedEvent
            {
                CorrelationId = msg.CorrelationId,
                Reason = ex.Message
            });
            return;
        }

        try
        {
            var http = _httpClientFactory.CreateClient("ShoppingCartApi");
            await http.DeleteAsync($"api/v1/cart?userId={msg.UserId}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear cart for user {UserId} after order fulfillment", msg.UserId);
        }
    }

    private static string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{timestamp}-{random}";
    }
}
