
using Microsoft.EntityFrameworkCore;
using Payment.API.Data;
using Payment.API.Models;

namespace Payment.API.Services
{
    public interface IPaymentService
    {
        Task<PaymentResponse> ProcessPaymentAsync(Guid userId, PaymentRequest request);
        Task<Payments?> GetPaymentByIdAsync(Guid id, Guid userId);
        Task<Payments?> GetPaymentByOrderIdAsync(Guid orderId, Guid userId);
        Task<PaymentResponse> RefundPaymentAsync(Guid paymentId, decimal amount, string reason);
        Task<bool> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string transactionId = null);
        Task ProcessStripeWebhookAsync(string webhookJson);
    }

    public class PaymentService : IPaymentService
    {
        private readonly PaymentContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PaymentService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentService(
            PaymentContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<PaymentService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(Guid userId, PaymentRequest request)
        {
            try
            {
                // Validate order exists and amount matches
                var order = await GetOrderDetailsAsync(request.OrderId, userId);
                if (order == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Order not found",
                        Status = PaymentStatus.Failed
                    };
                }

                if (order.TotalAmount != request.Amount)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment amount does not match order total",
                        Status = PaymentStatus.Failed
                    };
                }

                PaymentResponse paymentResult = request.Method switch
                {
                    PaymentMethod.CreditCard => await ProcessCreditCardPaymentAsync(request),
                    PaymentMethod.PayPal => await ProcessPayPalPaymentAsync(request),
                    PaymentMethod.BankTransfer => await ProcessBankTransferAsync(request),
                    PaymentMethod.CashOnDelivery => await ProcessCashOnDeliveryAsync(request),
                    _ => new PaymentResponse
                    {
                        Success = false,
                        Message = "Unsupported payment method",
                        Status = PaymentStatus.Failed
                    }
                };

                if (paymentResult.Success)
                {
                    // Save payment record
                    var payment = new Payments
                    {
                        Id = Guid.NewGuid(),
                        OrderId = request.OrderId,
                        Amount = request.Amount,
                        Currency = request.Currency,
                        Method = request.Method,
                        Status = paymentResult.Status,
                        TransactionId = paymentResult.TransactionId,
                        CustomerEmail = request.CustomerEmail,
                        Description = request.Description,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Payments.Add(payment);
                    await _context.SaveChangesAsync();

                    // Update order payment status
                    await UpdateOrderPaymentStatusAsync(request.OrderId, payment.Status);
                }

                return paymentResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);

                return new PaymentResponse
                {
                    Success = false,
                    Message = "Payment processing failed",
                    Status = PaymentStatus.Failed
                };
            }
        }

        public async Task<Payments?> GetPaymentByIdAsync(Guid id, Guid userId)
        {
            try
            {
                return await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment {PaymentId}", id);
                throw;
            }
        }

        public async Task<Payments?> GetPaymentByOrderIdAsync(Guid orderId, Guid userId)
        {
            try
            {
                return await _context.Payments
                    .FirstOrDefaultAsync(p => p.OrderId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<PaymentResponse> RefundPaymentAsync(Guid paymentId, decimal amount, string reason)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Payment not found",
                        Status = PaymentStatus.Failed
                    };
                }

                if (payment.Status != PaymentStatus.Completed)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Only completed payments can be refunded",
                        Status = PaymentStatus.Failed
                    };
                }

                if (amount > payment.Amount)
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Refund amount cannot exceed original payment amount",
                        Status = PaymentStatus.Failed
                    };
                }

                // Process refund with payment gateway
                bool refundSuccess = await ProcessRefundWithGatewayAsync(payment, amount, reason);

                if (refundSuccess)
                {
                    payment.Status = amount == payment.Amount ? PaymentStatus.Refunded : PaymentStatus.Refunded;
                    payment.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await UpdateOrderRefundStatusAsync(payment.OrderId, amount);

                    return new PaymentResponse
                    {
                        Success = true,
                        Message = $"Payment refunded successfully: {amount:C}",
                        Status = payment.Status,
                        TransactionId = payment.TransactionId
                    };
                }
                else
                {
                    return new PaymentResponse
                    {
                        Success = false,
                        Message = "Refund processing failed",
                        Status = PaymentStatus.Failed
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);

                return new PaymentResponse
                {
                    Success = false,
                    Message = "Refund processing failed",
                    Status = PaymentStatus.Failed
                };
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(Guid paymentId, PaymentStatus status, string transactionId = null)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    return false;
                }

                payment.Status = status;
                if (!string.IsNullOrEmpty(transactionId))
                {
                    payment.TransactionId = transactionId;
                }
                payment.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update order payment status
                await UpdateOrderPaymentStatusAsync(payment.OrderId, status);

                _logger.LogInformation("Payment {PaymentId} status updated to {Status}", paymentId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for {PaymentId}", paymentId);
                throw;
            }
        }

        public async Task ProcessStripeWebhookAsync(string webhookJson)
        {
            try
            {

                _logger.LogInformation("Processing Stripe webhook: {WebhookJson}", webhookJson);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                throw;
            }
        }

        private async Task<OrderDetails?> GetOrderDetailsAsync(Guid orderId, Guid userId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["OrderApi:BaseUrl"]);

                var response = await httpClient.GetAsync($"api/orders/{orderId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<OrderDetails>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order details for {OrderId}", orderId);
                return null;
            }
        }

        private async Task UpdateOrderPaymentStatusAsync(Guid orderId, PaymentStatus paymentStatus)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["OrderApi:BaseUrl"]);

                var orderStatus = paymentStatus switch
                {
                    PaymentStatus.Completed => "Confirmed",
                    PaymentStatus.Failed => "Pending",
                    PaymentStatus.Refunded => "Refunded",
                    _ => "Pending"
                };

                await httpClient.PutAsJsonAsync($"api/orders/{orderId}/payment-status", new { Status = orderStatus });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order payment status for {OrderId}", orderId);
                // Don't throw - this is not critical for payment processing
            }
        }

        private async Task UpdateOrderRefundStatusAsync(Guid orderId, decimal refundAmount)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(_configuration["OrderApi:BaseUrl"]);

                await httpClient.PutAsJsonAsync($"api/orders/{orderId}/refund", new { Amount = refundAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order refund status for {OrderId}", orderId);
                // Don't throw - this is not critical for refund processing
            }
        }

        private async Task<PaymentResponse> ProcessCreditCardPaymentAsync(PaymentRequest request)
        {

            await Task.Delay(100); 
            return new PaymentResponse
            {
                Success = true,
                TransactionId = $"txn_{Guid.NewGuid():N}",
                Message = "Payment processed successfully",
                Status = PaymentStatus.Completed
            };
        }

        private async Task<PaymentResponse> ProcessPayPalPaymentAsync(PaymentRequest request)
        {
            // Mock PayPal payment processing
            await Task.Delay(100);

            return new PaymentResponse
            {
                Success = true,
                TransactionId = $"paypal_{Guid.NewGuid():N}",
                Message = "PayPal payment processed",
                Status = PaymentStatus.Completed
            };
        }

        private async Task<PaymentResponse> ProcessBankTransferAsync(PaymentRequest request)
        {
            // Bank transfer usually requires manual verification
            await Task.Delay(100);

            return new PaymentResponse
            {
                Success = true,
                TransactionId = $"bank_{Guid.NewGuid():N}",
                Message = "Bank transfer initiated",
                Status = PaymentStatus.Pending
            };
        }

        private async Task<PaymentResponse> ProcessCashOnDeliveryAsync(PaymentRequest request)
        {
            await Task.Delay(100);

            return new PaymentResponse
            {
                Success = true,
                TransactionId = $"cod_{Guid.NewGuid():N}",
                Message = "Cash on delivery payment will be collected upon delivery",
                Status = PaymentStatus.Pending
            };
        }

        private async Task<bool> ProcessRefundWithGatewayAsync(Payments payment, decimal amount, string reason)
        {
            // Mock refund processing
            await Task.Delay(100);
            return true;
        }

        // Helper classes
        public class OrderDetails
        {
            public Guid Id { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }
}