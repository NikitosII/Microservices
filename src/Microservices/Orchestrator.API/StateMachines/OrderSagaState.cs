using MassTransit;

namespace Orchestrator.API.StateMachines;

/// <summary>
/// EF Core–persisted state for the order placement saga.
/// <para>Fields fall into four groups:</para>
/// <list type="bullet">
///   <item>Routing data — UserId, Email, PaymentMethod, CouponCode, SubTotal.</item>
///   <item>Intermediate results — CouponId and Discount (from CouponValidatedEvent), OrderId (from OrderFulfilledEvent).</item>
///   <item>Parallel-step flags — StockReserved, CouponValidated (pre-set to true when no coupon is used).</item>
///   <item>Compensation payloads — ItemsJson and ShippingAddressJson (JSON-serialized for reuse in release commands).</item>
/// </list>
/// <see cref="ISagaVersion"/> enables optimistic concurrency via a row-version column.
/// </summary>
public class OrderSagaState : SagaStateMachineInstance, ISagaVersion
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public int Version { get; set; }

    // ----- Routing / decision data -----

    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
    public decimal SubTotal { get; set; }

    // ----- Results from intermediate saga steps -----

    public Guid? CouponId { get; set; }   // filled by CouponValidatedEvent
    public decimal Discount { get; set; }
    public Guid? OrderId { get; set; }    // filled by OrderFulfilledEvent

    // ----- Parallel-step coordination flags -----

    public bool StockReserved { get; set; }
    public bool CouponValidated { get; set; }  // pre-set to true when no coupon

    // ----- Compensation payload -----

    public string ItemsJson { get; set; } = "[]";
    public string ShippingAddressJson { get; set; } = "{}";

    // ----- Diagnostics -----

    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
