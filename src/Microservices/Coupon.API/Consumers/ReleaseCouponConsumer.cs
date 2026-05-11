using Coupon.API.Services;
using EventBus.Messages;
using MassTransit;

namespace Coupon.API.Consumers;

public class ReleaseCouponConsumer : IConsumer<ReleaseCouponCommand>
{
    private readonly ICouponService _couponService;
    private readonly ILogger<ReleaseCouponConsumer> _logger;

    public ReleaseCouponConsumer(ICouponService couponService, ILogger<ReleaseCouponConsumer> logger)
    {
        _couponService = couponService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseCouponCommand> context)
    {
        var msg = context.Message;
        await _couponService.ReleaseCouponAsync(msg.CouponId);
        _logger.LogInformation("Coupon released for saga {CorrelationId}", msg.CorrelationId);
    }
}
