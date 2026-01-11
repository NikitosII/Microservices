using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.API.Models;
using Payment.API.Services;

namespace Payment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }

            return Guid.Parse(userIdClaim);
        }

        [HttpPost]
        public async Task<ActionResult<PaymentResponse>> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = GetUserId();
                var response = await _paymentService.ProcessPaymentAsync(userId, request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payments>> GetPayment(Guid id)
        {
            try
            {
                var userId = GetUserId();
                var payment = await _paymentService.GetPaymentByIdAsync(id, userId);

                if (payment == null)
                {
                    return NotFound();
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<Payments>> GetPaymentByOrderId(Guid orderId)
        {
            try
            {
                var userId = GetUserId();
                var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId, userId);

                if (payment == null)
                {
                    return NotFound();
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentResponse>> RefundPayment(Guid id, [FromBody] RefundRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var response = await _paymentService.RefundPaymentAsync(id, request.Amount, request.Reason);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost("webhook/stripe")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                await _paymentService.ProcessStripeWebhookAsync(json);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

    }

    public class RefundRequest
    {
        public decimal Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}