using Microsoft.EntityFrameworkCore;
using Order.API.Models;

namespace Order.API.Data
{
    public class OrderContext : DbContext 
    {
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {
        }

        public DbSet<Orders> Orders { get; set; }
        public DbSet<OrderItem> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Orders>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                entity.Property(e => e.Subtotal)
                    .HasPrecision(18, 2);
                entity.Property(e => e.Tax)
                    .HasPrecision(18, 2);
                entity.Property(e => e.ShippingCost)
                    .HasPrecision(18, 2);
                entity.Property(e => e.Discount)
                    .HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.OwnsOne(e => e.ShippingAddress, sa =>
                {
                    sa.Property(p => p.FullName).HasMaxLength(200);
                    sa.Property(p => p.Street).HasMaxLength(200);
                    sa.Property(p => p.City).HasMaxLength(100);
                    sa.Property(p => p.State).HasMaxLength(100);
                    sa.Property(p => p.Country).HasMaxLength(100);
                    sa.Property(p => p.ZipCode).HasMaxLength(20);
                    sa.Property(p => p.Phone).HasMaxLength(20);
                });

                entity.OwnsOne(e => e.PaymentInfo, pi =>
                {
                    pi.Property(p => p.Method).HasMaxLength(50);
                    pi.Property(p => p.TransactionId).HasMaxLength(100);
                    pi.Property(p => p.AmountPaid).HasPrecision(18, 2);
                });

                entity.HasMany(e => e.Items)
                    .WithOne()
                    .HasForeignKey("OrderId")
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice)
                    .HasPrecision(18, 2);
                entity.Property(e => e.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

            });
        }
    }

}