namespace ShoppingCart.API.Models
{
    public class AddToCartRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}