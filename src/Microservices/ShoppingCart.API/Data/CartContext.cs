

using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Models;

namespace ShoppingCart.API.Data
{
    public class CartContext : DbContext 
    {
        public CartContext(DbContextOptions<CartContext> options) : base(options)
        {
        }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Cart>(
                entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.HasIndex(e => e.UserId).IsUnique();
                    entity.Property(e => e.Price).HasPrecision(18, 2);
                    entity.HasMany(e => e.Items).WithOne().
                        HasForeignKey("CartId").OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity<CartItem>(
                entity =>
                {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ProductName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(e => new { e.Id, e.ProductId })
                    .IsUnique();
                });

            modelBuilder.Entity<CartItem>()
                .Property<Guid>("CartId");
        }
    }


}
