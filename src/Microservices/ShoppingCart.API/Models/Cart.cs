namespace ShoppingCart.API.Models
{
    public class Cart
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public decimal Price { get; set; }
        public decimal TotalPrice => Items.Sum(i => i.UnitPrice * i.Quantity);
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
