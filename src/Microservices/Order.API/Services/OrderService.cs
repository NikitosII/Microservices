using EventBus.Events;
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
        Task<Orders> GetByIdAsync(Guid id, Guid userId);
        Task<Orders?> GetByNumberAsync(string orderNumber, Guid userId);
        Task<IEnumerable<Orders>> GetOrdersByUserIdAsync(Guid userId);
        Task<Orders> CreateOrderAsync(Guid userId, CreateOrderRequest request);
        Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task<bool> CancelOrderAsync(Guid orderId, Guid userId);
    }
    public class OrderService : IOrderService
    {
        private readonly OrderContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IConfiguration _configuration; 
        private readonly IHttpClientFactory _httpClient;
        private readonly IPublishEndpoint _publishEndpoint;


        public OrderService(
            OrderContext context, ILogger<OrderService> logger,
            IConfiguration configuration, IHttpClientFactory httpClient, IPublishEndpoint publishEndpoint)      
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
            _publishEndpoint = publishEndpoint;
        }

        private async Task<Cart?> GetUserCartAsync(Guid userId)
        {
            try
            {
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["ShoppingCartApi:BaseUrl"]);

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
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["ShoppingCartApi:BaseUrl"]);

                await httpClient.DeleteAsync($"api/cart?userId={userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user cart for {UserId}", userId);
                // Don't throw - cart clearing is not critical for order creation
            }
        }

        private async Task ValidateAndUpdateProductStockAsync(List<ShoppingCart.API.Models.CartItem> cartItems)
        {
            foreach (var item in cartItems)
            {
                // Call Product API to check stock and update
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["ProductApi:BaseUrl"]);

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
            foreach (var item in orderItems)
            {
                // Call Product API to return stock
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["ProductApi:BaseUrl"]);

                await httpClient.PutAsJsonAsync(
                    $"api/products/{item.ProductId}/stock",
                    new { Quantity = item.Quantity }); // Positive to return stock
            }
        }

        private async Task<CouponValidationResponse> ValidateCouponAsync(string couponCode, decimal orderAmount)
        {
            try
            {
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["CouponApi:BaseUrl"]);

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
                var httpClient = _httpClient.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["CouponApi:BaseUrl"]);

                await httpClient.PostAsync($"api/coupons/{couponId}/use", null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using coupon {CouponId}", couponId);
                // Don't throw - coupon usage is not critical for order creation
            }
        }

        private async Task PublishOrderCreatedEvent(Orders order)
        {
            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.TotalAmount,
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

        private async Task PublishOrderStatusUpdatedEvent(Orders order)
        {
            var orderStatusUpdatedEvent = new OrderStatusUpdatedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                OldStatus = order.Status.ToString(),
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
            // Simple shipping cost calculation based on country
            return address.Country.ToUpper() switch
            {
                "USA" => 10.00m,
                _ => 25.00m // International shipping
            };
        }

        private bool IsValidStatusTransition(Orders.OrderStatus currentStatus, Orders.OrderStatus newStatus)
        {
            var allowedTransitions = new Dictionary<Orders.OrderStatus, List<Orders.OrderStatus>>
            {
                [Orders.OrderStatus.Pending] = new() { Orders.OrderStatus.Confirmed, Orders.OrderStatus.Cancelled },
                [Orders.OrderStatus.Confirmed] = new() { Orders.OrderStatus.Processing, Orders.OrderStatus.Cancelled },
                [Orders.OrderStatus.Processing] = new() { Orders.OrderStatus.Shipped, Orders.OrderStatus.Cancelled },
                [Orders.OrderStatus.Shipped] = new() { Orders.OrderStatus.Delivered },
                [Orders.OrderStatus.Delivered] = new() { Orders.OrderStatus.Refunded },
                [Orders.OrderStatus.Cancelled] = new(),
                [Orders.OrderStatus.Refunded] = new()
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

        public async Task<Orders> CreateOrderAsync(Guid userId, CreateOrderRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get user's cart
                var cart = await GetUserCartAsync(userId);
                if (cart == null || cart.Items.Count == 0)
                {
                    throw new InvalidOperationException("Cart is empty");
                }

                // Validate and update product stock
                await ValidateAndUpdateProductStockAsync(cart.Items);

                // Validate coupon if provided
                decimal discount = 0;
                Guid? couponId = null;

                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var couponValidation = await ValidateCouponAsync(request.CouponCode, cart.TotalPrice);
                    if (!couponValidation.IsValid)
                    {
                        throw new ArgumentException(couponValidation.Message);
                    }

                    discount = couponValidation.DiscountAmount;
                    couponId = couponValidation.Coupon?.Id;
                }

                // Calculate totals
                var subtotal = cart.TotalPrice;
                var tax = subtotal * 0.1m; 
                var shippingCost = CalculateShippingCost(request.ShippingAddress);
                var totalAmount = subtotal + tax + shippingCost - discount;

                // Create order
                var order = new Orders
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OrderNumber = GenerateOrderNumber(),
                    Status = Orders.OrderStatus.Pending,
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
                await _context.SaveChangesAsync();

                // Use coupon if applicable
                if (couponId.HasValue)
                {
                    await UseCouponAsync(couponId.Value);
                }

                // Clear user's cart
                await ClearUserCartAsync(userId);

                // Publish order created event
                await PublishOrderCreatedEvent(order);

                await transaction.CommitAsync();

                _logger.LogInformation("Order created: {OrderId} - {OrderNumber}", order.Id, order.OrderNumber);
                return order;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(Guid orderId, Orders.OrderStatus status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return false;
                }

                // Validate status transition
                if (!IsValidStatusTransition(order.Status, status))
                {
                    return false;
                }

                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Publish order status updated event
                await PublishOrderStatusUpdatedEvent(order);

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
                if (order.Status != Orders.OrderStatus.Pending && order.Status != Orders.OrderStatus.Confirmed)
                {
                    throw new ArgumentException("Order cannot be cancelled in its current state");
                }

                order.Status = Orders.OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;

                // Return product stock
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


