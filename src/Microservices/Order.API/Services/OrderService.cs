using EventBus.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.Models;
using static Order.API.Models.Orders;

namespace Order.API.Services
{
    public interface IOrderService
    {
        Task<Orders?> GetByIdAsync(Guid id, Guid userId);
        Task<Orders?> GetByNumberAsync(string orderNumber, Guid userId);
        Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(Guid userId);
        Task<Guid> StartOrderSagaAsync(Guid userId, string email, CreateOrderRequest request);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task<bool> CancelOrderAsync(Guid orderId, Guid userId);
    }

    public class OrderService : IOrderService
    {
        private readonly OrderContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IPublishEndpoint _publishEndpoint;

        public OrderService(
            OrderContext context,
            ILogger<OrderService> logger,
            IHttpClientFactory httpClient,
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _publishEndpoint = publishEndpoint;
        }

        private async Task<Cart?> GetUserCartAsync(Guid userId)
        {
            try
            {
                var httpClient = _httpClient.CreateClient("ShoppingCartApi");
                var response = await httpClient.GetAsync($"api/v1/cart?userId={userId}");
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<Cart>();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user cart for {UserId}", userId);
                throw;
            }
        }

        private async Task ReturnProductStockAsync(List<OrderItem> orderItems)
        {
            var httpClient = _httpClient.CreateClient("ProductApi");
            foreach (var item in orderItems)
            {
                await httpClient.PutAsJsonAsync(
                    $"api/v1/products/{item.ProductId}/stock",
                    new { Quantity = item.Quantity });
            }
        }

        private async Task PublishOrderStatusUpdatedEvent(Orders order, OrderStatus oldStatus)
        {
            await _publishEndpoint.Publish(new OrderStatusUpdatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OldStatus = oldStatus.ToString(),
                NewStatus = order.Status.ToString(),
                UpdatedAt = DateTime.UtcNow
            });
        }

        private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            var allowedTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                [OrderStatus.Pending]    = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
                [OrderStatus.Confirmed]  = new() { OrderStatus.Processing, OrderStatus.Cancelled },
                [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
                [OrderStatus.Shipped]    = new() { OrderStatus.Delivered },
                [OrderStatus.Delivered]  = new() { OrderStatus.Refunded },
                [OrderStatus.Cancelled]  = new(),
                [OrderStatus.Refunded]   = new()
            };
            return allowedTransitions[currentStatus].Contains(newStatus);
        }

        public async Task<Orders?> GetByIdAsync(Guid id, Guid userId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting by id {OrderId}", id);
                throw;
            }
        }

        public async Task<Orders?> GetByNumberAsync(string number, Guid userId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(x => x.OrderNumber == number && x.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting by number {OrderNumber}", number);
                throw;
            }
        }

        public async Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(Guid userId)
        {
            try
            {
                return await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Guid> StartOrderSagaAsync(Guid userId, string email, CreateOrderRequest request)
        {
            var cart = await GetUserCartAsync(userId);
            if (cart == null || cart.Items.Count == 0)
                throw new InvalidOperationException("Cart is empty");

            var correlationId = Guid.NewGuid();

            await _publishEndpoint.Publish(new OrderPlacedCommand
            {
                CorrelationId = correlationId,
                UserId = userId,
                Email = email,
                PaymentMethod = request.PaymentMethod,
                CouponCode = request.CouponCode,
                ShippingAddress = new SagaAddress
                {
                    FullName = request.ShippingAddress.FullName,
                    Street = request.ShippingAddress.Street,
                    City = request.ShippingAddress.City,
                    State = request.ShippingAddress.State,
                    Country = request.ShippingAddress.Country,
                    ZipCode = request.ShippingAddress.ZipCode,
                    Phone = request.ShippingAddress.Phone
                },
                Items = cart.Items.Select(i => new SagaItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity
                }).ToList(),
                SubTotal = cart.TotalPrice
            });

            _logger.LogInformation("Order saga started: {CorrelationId} for user {UserId}", correlationId, userId);
            return correlationId;
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return false;

                if (!IsValidStatusTransition(order.Status, status))
                    return false;

                var oldStatus = order.Status;
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                await PublishOrderStatusUpdatedEvent(order, oldStatus);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order status updated: {OrderId} -> {Status}", orderId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, Guid userId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

                if (order == null)
                    return false;

                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                    throw new ArgumentException("Order cannot be cancelled in its current state");

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;

                await ReturnProductStockAsync(order.Items);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order cancelled: {OrderId}", orderId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                throw;
            }
        }
    }
}
