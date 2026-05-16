namespace Product.API.Models
{
    public class StockUpdateRequest
    {
        // Negative value decrements stock; positive increments it.
        public int Quantity { get; set; }
    }
}