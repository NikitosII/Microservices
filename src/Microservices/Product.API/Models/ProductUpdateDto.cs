namespace Product.API.Models
{
    public class ProductUpdateDto
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public string Category { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

}


