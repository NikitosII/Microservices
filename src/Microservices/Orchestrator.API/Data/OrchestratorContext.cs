using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orchestrator.API.StateMachines;

namespace Orchestrator.API.Data;

public class OrchestratorContext : DbContext
{
    public OrchestratorContext(DbContextOptions<OrchestratorContext> options) : base(options) { }

    public DbSet<OrderSagaState> OrderSagas { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddInboxStateEntity();

        modelBuilder.Entity<OrderSagaState>(entity =>
        {
            entity.HasKey(e => e.CorrelationId);
            entity.Property(e => e.CurrentState).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.CouponCode).HasMaxLength(50);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            // Compact JSON columns — not full domain objects
            entity.Property(e => e.ItemsJson).HasColumnType("text");
            entity.Property(e => e.ShippingAddressJson).HasColumnType("text");
        });
    }
}
