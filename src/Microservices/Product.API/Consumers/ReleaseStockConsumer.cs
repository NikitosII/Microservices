using EventBus.Messages;
using MassTransit;
using Product.API.Data;

namespace Product.API.Consumers;

/// <summary>
/// Consumes <see cref="ReleaseStockCommand"/> sent by the saga as a compensating action.
/// Re-increments stock for each line item and saves in a single <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> call.
/// </summary>
public class ReleaseStockConsumer : IConsumer<ReleaseStockCommand>
{
    private readonly ProductContext _context;
    private readonly ILogger<ReleaseStockConsumer> _logger;

    public ReleaseStockConsumer(ProductContext context, ILogger<ReleaseStockConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
    {
        var msg = context.Message;

        foreach (var item in msg.Items)
        {
            var product = await _context.Entities.FindAsync(item.ProductId);
            if (product is not null)
            {
                product.Stock += item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Stock released for saga {CorrelationId}", msg.CorrelationId);
    }
}