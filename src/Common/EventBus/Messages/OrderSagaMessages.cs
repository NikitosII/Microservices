namespace EventBus.Messages;

// ----- Value objects carried in commands -----
public record SagaItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public record SagaAddress
{
    public string FullName { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}

// ----- Saga trigger (Order.API -> Orchestrator) ----- 

public record OrderPlacedCommand
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string? CouponCode { get; init; }
    public SagaAddress ShippingAddress { get; init; } = new();
    public List<SagaItem> Items { get; init; } = new();
    public decimal SubTotal { get; init; }
}

// ----- Stock commands / events -----
public record ReserveStockCommand
{
    public Guid CorrelationId { get; init; }
    public List<SagaItem> Items { get; init; } = new();
}

public record ReleaseStockCommand
{
    public Guid CorrelationId { get; init; }
    public List<SagaItem> Items { get; init; } = new();
}

public record StockReservedEvent
{
    public Guid CorrelationId { get; init; }
}

public record StockReservationFailedEvent
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

// ----- Coupon commands / events -----

public record ValidateCouponCommand
{
    public Guid CorrelationId { get; init; }
    public string CouponCode { get; init; } = string.Empty;
    public decimal OrderAmount { get; init; }
}

public record ReleaseCouponCommand
{
    public Guid CorrelationId { get; init; }
    public Guid CouponId { get; init; }
}

public record CouponValidatedEvent
{
    public Guid CorrelationId { get; init; }
    public Guid CouponId { get; init; }
    public decimal Discount { get; init; }
}

public record CouponValidationFailedEvent
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

// ----- Order fulfillment commands / events -----

public record FulfillOrderCommand
{
    public Guid CorrelationId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string? CouponCode { get; init; }
    public decimal Discount { get; init; }
    public SagaAddress ShippingAddress { get; init; } = new();
    public List<SagaItem> Items { get; init; } = new();
    public decimal SubTotal { get; init; }
}

public record OrderFulfilledEvent
{
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}

public record OrderFulfillmentFailedEvent
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record CancelOrderCommand
{
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
}
