using EventBus.Messages;
using MassTransit;
using System.Text.Json;

namespace Orchestrator.API.StateMachines;

/// <summary>
/// MassTransit saga that orchestrates the order placement workflow.
///
/// States: Initial → Submitted → FulfillingOrder → Completed | Failed.
///
/// Flow:
/// 1. OrderPlacedCommand received — persists routing data, publishes ReserveStockCommand
///    and (if coupon present) ValidateCouponCommand in parallel, transitions to Submitted.
/// 2. In Submitted — waits for both StockReservedEvent and CouponValidatedEvent.
///    Once both flags are true, publishes FulfillOrderCommand → FulfillingOrder.
///    On any failure, publishes compensating release commands and transitions to Failed.
/// 3. In FulfillingOrder — waits for OrderFulfilledEvent → Completed,
///    or OrderFulfillmentFailedEvent → releases stock + coupon → Failed.
///
/// SetCompletedWhenFinalized() removes the saga row from the repository after finalization.
///
/// Helpers:
///   BuildFulfillCommand — assembles FulfillOrderCommand from saga state.
///   DeserializeItems    — deserializes the JSON item payload for compensation commands.
/// </summary>
public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    // ----- States ----- 

    public State Submitted { get; private set; } = null!;
    public State FulfillingOrder { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    // ----- Events -----

    public Event<OrderPlacedCommand> OrderPlaced { get; private set; } = null!;
    public Event<StockReservedEvent> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailedEvent> StockReservationFailed { get; private set; } = null!;
    public Event<CouponValidatedEvent> CouponValidated { get; private set; } = null!;
    public Event<CouponValidationFailedEvent> CouponValidationFailed { get; private set; } = null!;
    public Event<OrderFulfilledEvent> OrderFulfilled { get; private set; } = null!;
    public Event<OrderFulfillmentFailedEvent> OrderFulfillmentFailed { get; private set; } = null!;

    private static FulfillOrderCommand BuildFulfillCommand(OrderSagaState s) =>
        new()
        {
            CorrelationId = s.CorrelationId,
            UserId = s.UserId,
            Email = s.Email,
            PaymentMethod = s.PaymentMethod,
            CouponCode = s.CouponCode,
            Discount = s.Discount,
            ShippingAddress = JsonSerializer.Deserialize<SagaAddress>(s.ShippingAddressJson)!,
            Items = DeserializeItems(s.ItemsJson),
            SubTotal = s.SubTotal
        };

    private static List<SagaItem> DeserializeItems(string json) =>
        JsonSerializer.Deserialize<List<SagaItem>>(json) ?? new();
        
    public OrderSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // All messages are correlated by CorrelationId
        Event(() => OrderPlaced, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReservationFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CouponValidated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => CouponValidationFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => OrderFulfilled, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => OrderFulfillmentFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // ----- Initial -----

        Initially(
            When(OrderPlaced)
                .Then(ctx =>
                {
                    var s = ctx.Saga;
                    var m = ctx.Message;
                    s.UserId = m.UserId;
                    s.Email = m.Email;
                    s.PaymentMethod = m.PaymentMethod;
                    s.CouponCode = m.CouponCode;
                    s.SubTotal = m.SubTotal;
                    s.ItemsJson = JsonSerializer.Serialize(m.Items);
                    s.ShippingAddressJson = JsonSerializer.Serialize(m.ShippingAddress);
                    s.StockReserved = false;
                    s.CouponValidated = string.IsNullOrEmpty(m.CouponCode);
                    s.CreatedAt = DateTime.UtcNow;
                })
                // Always send stock reservation
                .PublishAsync(ctx => ctx.Init<ReserveStockCommand>(new ReserveStockCommand
                {
                    CorrelationId = ctx.Message.CorrelationId,
                    Items = ctx.Message.Items
                }))
                // Send coupon validation in parallel only when a coupon is provided
                .If(ctx => !string.IsNullOrEmpty(ctx.Message.CouponCode),
                    b => b.PublishAsync(ctx => ctx.Init<ValidateCouponCommand>(new ValidateCouponCommand
                    {
                        CorrelationId = ctx.Message.CorrelationId,
                        CouponCode = ctx.Message.CouponCode!,
                        OrderAmount = ctx.Message.SubTotal
                    })))
                .TransitionTo(Submitted)
        );

        // ----- Submitted: waiting for stock + coupon (parallel) -----

        During(Submitted,
            When(StockReserved)
                .Then(ctx => { ctx.Saga.StockReserved = true; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                .IfElse(
                    ctx => ctx.Saga.CouponValidated,
                    // Both steps done → create order
                    ifBnd => ifBnd
                        .PublishAsync(ctx => ctx.Init<FulfillOrderCommand>(BuildFulfillCommand(ctx.Saga)))
                        .TransitionTo(FulfillingOrder),
                    elseBnd => elseBnd.TransitionTo(Submitted)
                ),

            // Insufficient stock — saga ends immediately (nothing to compensate yet)
            When(StockReservationFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = $"Stock: {ctx.Message.Reason}";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(Failed)
                .Finalize(),

            When(CouponValidated)
                .Then(ctx =>
                {
                    ctx.Saga.CouponValidated = true;
                    ctx.Saga.CouponId = ctx.Message.CouponId;
                    ctx.Saga.Discount = ctx.Message.Discount;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .IfElse(
                    ctx => ctx.Saga.StockReserved,
                    // Both steps done → create order
                    ifBnd => ifBnd
                        .PublishAsync(ctx => ctx.Init<FulfillOrderCommand>(BuildFulfillCommand(ctx.Saga)))
                        .TransitionTo(FulfillingOrder),
                    // Still waiting for stock reservation
                    elseBnd => elseBnd.TransitionTo(Submitted)
                ),

            // Coupon invalid — compensate already-reserved stock (if any)
            When(CouponValidationFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = $"Coupon: {ctx.Message.Reason}";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .If(ctx => ctx.Saga.StockReserved,
                    b => b.PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new ReleaseStockCommand
                    {
                        CorrelationId = ctx.Saga.CorrelationId,
                        Items = DeserializeItems(ctx.Saga.ItemsJson)
                    })))
                .TransitionTo(Failed)
                .Finalize()
        );

        // ----- FulfillingOrder: waiting for order creation -----

        During(FulfillingOrder,

            When(OrderFulfilled)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .TransitionTo(Completed)
                .Finalize(),

            // Order creation failed — compensate stock and coupon
            When(OrderFulfillmentFailed)
                .Then(ctx =>
                {
                    ctx.Saga.FailureReason = $"Fulfillment: {ctx.Message.Reason}";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new ReleaseStockCommand
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    Items = DeserializeItems(ctx.Saga.ItemsJson)
                }))
                .If(ctx => ctx.Saga.CouponId.HasValue,
                    b => b.PublishAsync(ctx => ctx.Init<ReleaseCouponCommand>(new ReleaseCouponCommand
                    {
                        CorrelationId = ctx.Saga.CorrelationId,
                        CouponId = ctx.Saga.CouponId!.Value
                    })))
                .TransitionTo(Failed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }

}
