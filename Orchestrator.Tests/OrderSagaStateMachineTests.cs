using EventBus.Messages;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.API.StateMachines;

namespace Orchestrator.Tests;

[TestClass]
public class OrderSagaStateMachineTests
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    [TestInitialize]
    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    [TestCleanup]
    public async Task CleanupAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private static OrderPlacedCommand MakeCommand(Guid correlationId, string? couponCode = null) => new()
    {
        CorrelationId = correlationId,
        UserId = Guid.NewGuid(),
        Email = "test@example.com",
        PaymentMethod = "CreditCard",
        CouponCode = couponCode,
        Items = new List<SagaItem>
        {
            new() { ProductId = Guid.NewGuid(), ProductName = "Widget", UnitPrice = 10m, Quantity = 2 }
        },
        SubTotal = 20m,
        ShippingAddress = new SagaAddress
        {
            FullName = "Test User", Street = "1 Main St", City = "Springfield",
            State = "IL", Country = "USA", ZipCode = "12345", Phone = "555-0100"
        }
    };

    /// <summary>
    /// Happy path without a coupon: saga publishes ReserveStockCommand, then after stock is
    /// reserved it skips coupon validation and immediately publishes FulfillOrderCommand.
    /// </summary>
    [TestMethod]
    public async Task HappyPath_NoCoupon_PublishesReserveAndFulfillCommands()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(MakeCommand(correlationId));

        Assert.IsTrue(await _harness.Published.Any<ReserveStockCommand>(),
            "Saga should request stock reservation");

        await _harness.Bus.Publish(new StockReservedEvent { CorrelationId = correlationId });

        Assert.IsTrue(await _harness.Published.Any<FulfillOrderCommand>(),
            "Saga should request order fulfillment once stock is reserved and no coupon is needed");
    }

    /// <summary>
    /// Happy path with a coupon: ReserveStockCommand and ValidateCouponCommand are both
    /// published in parallel; after both succeed the saga publishes FulfillOrderCommand.
    /// </summary>
    [TestMethod]
    public async Task HappyPath_WithCoupon_PublishesBothParallelCommandsThenFulfills()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(MakeCommand(correlationId, couponCode: "SAVE10"));

        Assert.IsTrue(await _harness.Published.Any<ReserveStockCommand>(),
            "Saga should request stock reservation");
        Assert.IsTrue(await _harness.Published.Any<ValidateCouponCommand>(),
            "Saga should request coupon validation in parallel");

        await _harness.Bus.Publish(new StockReservedEvent { CorrelationId = correlationId });
        await _harness.Bus.Publish(new CouponValidatedEvent
        {
            CorrelationId = correlationId,
            CouponId = Guid.NewGuid(),
            Discount = 5m
        });

        Assert.IsTrue(await _harness.Published.Any<FulfillOrderCommand>(),
            "Saga should request fulfillment once both stock and coupon are confirmed");
    }

    /// <summary>
    /// When coupon validation fails after stock was already reserved, the saga must
    /// publish ReleaseStockCommand to compensate the reserved stock.
    /// </summary>
    [TestMethod]
    public async Task CouponValidationFailed_AfterStockReserved_PublishesReleaseStock()
    {
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(MakeCommand(correlationId, couponCode: "EXPIRED"));
        Assert.IsTrue(await _harness.Published.Any<ReserveStockCommand>());

        await _harness.Bus.Publish(new StockReservedEvent { CorrelationId = correlationId });
        await _harness.Bus.Publish(new CouponValidationFailedEvent
        {
            CorrelationId = correlationId,
            Reason = "Coupon has expired"
        });

        Assert.IsTrue(await _harness.Published.Any<ReleaseStockCommand>(),
            "Saga must release already-reserved stock when coupon validation fails");
    }

    /// <summary>
    /// When order fulfillment fails after both stock and coupon were reserved, the saga
    /// must publish both ReleaseStockCommand and ReleaseCouponCommand as compensation.
    /// </summary>
    [TestMethod]
    public async Task FulfillmentFailed_PublishesReleaseStockAndReleaseCoupon()
    {
        var correlationId = Guid.NewGuid();
        var couponId = Guid.NewGuid();

        await _harness.Bus.Publish(MakeCommand(correlationId, couponCode: "DISC5"));
        await _harness.Bus.Publish(new StockReservedEvent { CorrelationId = correlationId });
        await _harness.Bus.Publish(new CouponValidatedEvent
        {
            CorrelationId = correlationId,
            CouponId = couponId,
            Discount = 5m
        });

        Assert.IsTrue(await _harness.Published.Any<FulfillOrderCommand>());

        await _harness.Bus.Publish(new OrderFulfillmentFailedEvent
        {
            CorrelationId = correlationId,
            Reason = "Database connection lost"
        });

        Assert.IsTrue(await _harness.Published.Any<ReleaseStockCommand>(),
            "Saga must release stock when fulfillment fails");
        Assert.IsTrue(await _harness.Published.Any<ReleaseCouponCommand>(),
            "Saga must release coupon usage when fulfillment fails");
    }
}
