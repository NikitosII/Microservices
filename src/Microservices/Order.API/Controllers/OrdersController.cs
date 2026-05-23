using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.API.Models;
using Order.API.Services;

namespace Order.API.Controllers
{
    /// <summary>
    /// HTTP controller for order operations.
    /// Route: /api/v1/orders  (JWT required for all endpoints)
    /// <list type="bullet">
    ///   <item>GET /                      — list caller's orders.</item>
    ///   <item>GET /{id}                  — single order by id (scoped to caller).</item>
    ///   <item>GET /by-number/{number}    — order lookup by order number.</item>
    ///   <item>POST /                     — starts the order saga; returns 202 Accepted with correlationId for polling.</item>
    ///   <item>PUT /{id}/status           — admin-only status update.</item>
    ///   <item>POST /{id}/cancel          — user-initiated cancellation (Pending/Confirmed only).</item>
    /// </list>
    /// GetUserId / GetUserEmail — extract the 'sub' and 'email' claims from the JWT.
    /// </summary>
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User not authenticated");

            return Guid.Parse(userIdClaim);
        }

        private string GetUserEmail() =>
            User.FindFirst("email")?.Value ?? string.Empty;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Orders>>> GetOrders()
        {
            try
            {
                var userId = GetUserId();
                var orders = await _orderService.GetOrdersByUserIdAsync(userId);
                return Ok(orders);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to get orders");
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return StatusCode(500, "An error occurred while retrieving orders");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Orders>> GetOrder(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var order = await _orderService.GetByIdAsync(id, userId);

                if (order == null)
                {
                    return NotFound();
                }

                return Ok(order);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to get order {OrderId}", id);
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order with ID {OrderId}", id);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = GetUserId();
                var email = GetUserEmail();
                var correlationId = await _orderService.StartOrderSagaAsync(userId, email, request);

                _logger.LogInformation("Order saga started: {CorrelationId} for user {UserId}", correlationId, userId);
                return Accepted(new { correlationId });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to create order");
                return Unauthorized();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating order");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting order saga");
                return StatusCode(500, "An error occurred while placing the order");
            }
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(id, request.Status);

                if (!result)
                {
                    return NotFound();
                }

                _logger.LogInformation("Order {OrderId} status updated to {Status}", id, request.Status);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }

        [HttpGet("by-number/{orderNumber}")]
        public async Task<ActionResult<Orders>> GetOrderByNumber(string orderNumber)
        {
            try
            {
                var userId = GetUserId();
                var order = await _orderService.GetByNumberAsync(orderNumber, userId);

                if (order == null)
                {
                    return NotFound();
                }

                return Ok(order);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to get order by number {OrderNumber}", orderNumber);
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order by number {OrderNumber}", orderNumber);
                return StatusCode(500, "An error occurred while retrieving the order");
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var result = await _orderService.CancelOrderAsync(id, userId);

                if (!result)
                {
                    return BadRequest("Cannot cancel order in its current state");
                }

                _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", id, userId);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to cancel order {OrderId}", id);
                return Unauthorized();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when cancelling order {OrderId}", id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, "An error occurred while cancelling the order");
            }
        }
    }

    public class UpdateOrderStatusRequest
    {
        public Orders.OrderStatus Status { get; set; }
    }
}
