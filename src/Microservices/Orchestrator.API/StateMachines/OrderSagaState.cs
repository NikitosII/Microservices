using MassTransit;

namespace Orchestrator.API.StateMachines;

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
