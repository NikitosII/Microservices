using Coupon.API.Services;
using EventBus.Messages;
using MassTransit;

namespace Coupon.API.Consumers;

/// <summary>
/// Consumes <see cref="ValidateCouponCommand"/> from the orchestrator saga (parallel with stock reservation).
/// Calls <see cref="ICouponService.ValidateCouponAsync"/> then <see cref="ICouponService.UseCouponAsync"/>
/// to atomically reserve the usage slot.
/// Publishes <see cref="CouponValidatedEvent"/> on success, or <see cref="CouponValidationFailedEvent"/> on failure.
/// </summary>
public class ValidateCouponConsumer : IConsumer<ValidateCouponCommand>
{
    private readonly ICouponService _couponService;
    private readonly ILogger<ValidateCouponConsumer> _logger;

    public ValidateCouponConsumer(ICouponService couponService, ILogger<ValidateCouponConsumer> logger)
    {
        _couponService = couponService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ValidateCouponCommand> context)
    {
        var msg = context.Message;

        var result = await _couponService.ValidateCouponAsync(msg.CouponCode, msg.OrderAmount);

        if (!result.IsValid || result.Coupon is null)
        {
            _logger.LogWarning("Coupon validation failed for saga {CorrelationId}: {Reason}", msg.CorrelationId, result.Message);
            await context.Publish(new CouponValidationFailedEvent
            {
                CorrelationId = msg.CorrelationId,
                Reason = result.Message
            });
            return;
        }

        // Reserve the coupon usage atomically with validation.
        await _couponService.UseCouponAsync(result.Coupon.Id);

        _logger.LogInformation("Coupon validated for saga {CorrelationId}, discount {Discount}", msg.CorrelationId, result.DiscountAmount);
        await context.Publish(new CouponValidatedEvent
        {
            CorrelationId = msg.CorrelationId,
            CouponId = result.Coupon.Id,
            Discount = result.DiscountAmount
        });
    }
}
