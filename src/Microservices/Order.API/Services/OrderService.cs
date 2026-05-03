using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Data;
using Order.API.Events;
using Order.API.Models;
using static Order.API.Models.Orders;

namespace Order.API.Services
{
    public interface IOrderService
    {
        Task<Orders?> GetByIdAsync(Guid id, Guid userId);
        Task<Orders?> GetByNumberAsync(string orderNumber, Guid userId);
        Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(Guid userId);
        Task<Orders> CreateOrderAsync(Guid userId, string email, CreateOrderRequest request);
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

                var response = await httpClient.GetAsync($"api/cart?userId={userId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Cart>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user cart for {UserId}", userId);
                throw;
            }
        }

        private async Task ClearUserCartAsync(Guid userId)
        {
            try
            {
                var httpClient = _httpClient.CreateClient("ShoppingCartApi");

                await httpClient.DeleteAsync($"api/cart?userId={userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user cart for {UserId}", userId);
            }
        }

        private async Task ValidateAndUpdateProductStockAsync(List<CartItem> cartItems)
        {
            var httpClient = _httpClient.CreateClient("ProductApi");

            foreach (var item in cartItems)
            {
                var response = await httpClient.PutAsJsonAsync(
                    $"api/products/{item.ProductId}/stock",
                    new { Quantity = -item.Quantity }); // Negative to reduce stock

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");
                }
            }
        }

        private async Task ReturnProductStockAsync(List<OrderItem> orderItems)
        {
            var httpClient = _httpClient.CreateClient("ProductApi");

            foreach (var item in orderItems)
            {
                await httpClient.PutAsJsonAsync(
                    $"api/products/{item.ProductId}/stock",
                    new { Quantity = item.Quantity }); // Positive to return stock
            }
        }

        private async Task<CouponValidationResponse> ValidateCouponAsync(string couponCode, decimal orderAmount)
        {
            try
            {
                var httpClient = _httpClient.CreateClient("CouponApi");

                var response = await httpClient.PostAsJsonAsync(
                    "api/coupons/validate",
                    new { Code = couponCode, OrderAmount = orderAmount });

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<CouponValidationResponse>();
                }

                return new CouponValidationResponse { IsValid = false, Message = "Coupon validation failed" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon {CouponCode}", couponCode);
                return new CouponValidationResponse { IsValid = false, Message = "Error validating coupon" };
            }
        }

        private async Task UseCouponAsync(Guid couponId)
        {
            try
            {
                var httpClient = _httpClient.CreateClient("CouponApi");

                await httpClient.PostAsync($"api/coupons/{couponId}/use", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using coupon {CouponId}", couponId);
            }
        }

        private async Task PublishOrderCreatedEvent(Orders order, string email)
        {
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
                Email = email,
                Items = order.Items.Select(item => new OrderItemEvent
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.UnitPrice
                }).ToList()
            };

            await _publishEndpoint.Publish(orderCreatedEvent);
        }

        private async Task PublishOrderStatusUpdatedEvent(Orders order, OrderStatus oldStatus)
        {
            var orderStatusUpdatedEvent = new OrderStatusUpdatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OldStatus = oldStatus.ToString(),
                NewStatus = order.Status.ToString(),
                UpdatedAt = DateTime.UtcNow
            };

            await _publishEndpoint.Publish(orderStatusUpdatedEvent);
        }

        private string GenerateOrderNumber()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"ORD-{timestamp}-{random}";
        }

        private decimal CalculateShippingCost(ShippingAddress address)
        {
            return address.Country.ToUpper() switch
            {
                "USA" => 10.00m,
                _ => 25.00m // International shipping
            };
        }

        private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            var allowedTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
                [OrderStatus.Confirmed] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
                [OrderStatus.Processing] = new() { OrderStatus.Shipped, OrderStatus.Cancelled },
                [OrderStatus.Shipped] = new() { OrderStatus.Delivered },
                [OrderStatus.Delivered] = new() { OrderStatus.Refunded },
                [OrderStatus.Cancelled] = new(),
                [OrderStatus.Refunded] = new()
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
            catch(Exception ex) 
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

        public async Task<Orders> CreateOrderAsync(Guid userId, string email, CreateOrderRequest request)
        {
            // If anything here fails the whole thing rolls back; no orphaned events.
            Orders order;
            Guid? couponId = null;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var cart = await GetUserCartAsync(userId);
                    if (cart == null || cart.Items.Count == 0)
                        throw new InvalidOperationException("Cart is empty");

                    await ValidateAndUpdateProductStockAsync(cart.Items);

                    decimal discount = 0;
                    if (!string.IsNullOrEmpty(request.CouponCode))
                    {
                        var couponValidation = await ValidateCouponAsync(request.CouponCode, cart.TotalPrice);
                        if (!couponValidation.IsValid)
                            throw new ArgumentException(couponValidation.Message);

                        discount = couponValidation.DiscountAmount;
                        couponId = couponValidation.Coupon?.Id;
                    }

                    var subtotal = cart.TotalPrice;
                    var tax = subtotal * 0.1m;
                    var shippingCost = CalculateShippingCost(request.ShippingAddress);
                    var totalAmount = subtotal + tax + shippingCost - discount;

                    order = new Orders
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        OrderNumber = GenerateOrderNumber(),
                        Status = OrderStatus.Pending,
                        Items = cart.Items.Select(item => new OrderItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = item.ProductId,
                            ProductName = item.ProductName,
                            UnitPrice = item.UnitPrice,
                            Quantity = item.Quantity,
                            TotalPrice = item.UnitPrice * item.Quantity
                        }).ToList(),
                        Subtotal = subtotal,
                        Tax = tax,
                        ShippingCost = shippingCost,
                        Discount = discount,
                        TotalAmount = totalAmount,
                        ShippingAddress = request.ShippingAddress,
                        PaymentInfo = new PaymentInfo
                        {
                            Method = request.PaymentMethod,
                            AmountPaid = totalAmount
                        },
                        CouponCode = request.CouponCode,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Orders.Add(order);

                    await PublishOrderCreatedEvent(order, email);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            if (couponId.HasValue)
                await UseCouponAsync(couponId.Value);

            await ClearUserCartAsync(userId);

            _logger.LogInformation("Order created: {OrderId} - {OrderNumber}", order.Id, order.OrderNumber);
            return order;
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
                {
                    return false;
                }

                // Check if order can be cancelled
                if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                {
                    throw new ArgumentException("Order cannot be cancelled in its current state");
                }

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


