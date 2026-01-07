using Microsoft.EntityFrameworkCore;
using Coupon.API.Models;
namespace Coupon.API.Data
{
    public class CouponContext : DbContext
    {
        public CouponContext(DbContextOptions<CouponContext> options) : base(options)
        {
        }

        public DbSet<Coupons> Coupons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Coupons>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.MinimumAmount).HasPrecision(18, 2);
                entity.Property(e => e.MaximumDiscount).HasPrecision(18, 2);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}