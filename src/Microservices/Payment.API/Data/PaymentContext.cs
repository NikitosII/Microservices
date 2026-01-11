

using Microsoft.EntityFrameworkCore;
using Payment.API.Models;

namespace Payment.API.Data
{
    public class PaymentContext : DbContext
    {
        public PaymentContext(DbContextOptions<PaymentContext> options) : base(options)
        {
        }

        public DbSet<Payments> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payments>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(e => e.Amount)
                    .HasPrecision(18, 2);
                entity.HasIndex(e => e.TransactionId)
                    .IsUnique();
                entity.Property(e => e.Method)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Currency)
                    .HasMaxLength(3)
                    .IsRequired()
                    .HasDefaultValue("USD");

                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Description)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }

}
