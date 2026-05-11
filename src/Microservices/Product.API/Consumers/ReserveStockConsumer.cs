using EventBus.Messages;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Product.API.Data;

namespace Product.API.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStockCommand>
{
    private readonly ProductContext _context;
    private readonly ILogger<ReserveStockConsumer> _logger;

    public ReserveStockConsumer(ProductContext context, ILogger<ReserveStockConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var msg = context.Message;

        // This transaction ensures all-or-nothing stock decrement across all items.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in msg.Items)
            {
                var product = await _context.Entities.FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product is null || product.Stock < item.Quantity)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning("Insufficient stock for product {ProductId} (saga {CorrelationId})",
                        item.ProductId, msg.CorrelationId);

                    await context.Publish(new StockReservationFailedEvent
                    {
                        CorrelationId = msg.CorrelationId,
                        Reason = $"Product {item.ProductId}: available {product?.Stock ?? 0}, requested {item.Quantity}"
                    });
                    return;
                }

                product.Stock -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stock reserved for saga {CorrelationId}", msg.CorrelationId);
            await context.Publish(new StockReservedEvent { CorrelationId = msg.CorrelationId });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}