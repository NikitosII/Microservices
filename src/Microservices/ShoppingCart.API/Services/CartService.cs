using Microsoft.EntityFrameworkCore;
using ShoppingCart.API.Data;
using ShoppingCart.API.Models;

namespace ShoppingCart.API.Services
{
    /// <summary>
    /// Shopping cart service backed by CartDb (PostgreSQL).
    /// <list type="bullet">
    ///   <item>GetCartAsync       — returns or creates the cart for a user.</item>
    ///   <item>AddToCartAsync     — looks up product info from Product.API, then adds or merges a line item.</item>
    ///   <item>UpdateCartItemAsync — sets quantity; removes item if quantity ≤ 0.</item>
    ///   <item>RemoveFromCartAsync — removes a single line item.</item>
    ///   <item>ClearCartAsync     — empties all items (called by FulfillOrderConsumer after order commit).</item>
    /// </list>
    /// Private helpers:
    ///   UpdateCartTotalAsync — recalculates Cart.Price from line items.
    ///   GetProductInfoAsync  — HTTP GET to Product.API for name/price/image.
    /// </summary>
    public interface ICartService
    {
        Task<Cart> GetCartAsync(Guid userId);
        Task<Cart> AddToCartAsync(Guid userId, AddToCartRequest request);
        Task<Cart> UpdateCartItemAsync(Guid userId, Guid itemId, UpdateCartRequest request);
        Task<Cart> RemoveFromCartAsync(Guid userId, Guid itemId);
        Task ClearCartAsync(Guid userId);
    }

    public class CartService : ICartService
    {
        private readonly CartContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CartService> _logger;

        public CartService(CartContext context, IHttpClientFactory httpClientFactory, ILogger<CartService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private class ProductInfo
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public string ImageUrl { get; set; } = string.Empty;
        }

        public async Task<Cart> GetCartAsync(Guid userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<Cart> AddToCartAsync(Guid userId, AddToCartRequest request)
        {
            var cart = await GetCartAsync(userId);

            // Get product info from Product service
            var product = await GetProductInfoAsync(request.ProductId);
            if (product == null)
            {
                throw new ArgumentException("Product not found");
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem != null)
            {
                existingItem.Quantity += request.Quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = request.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = request.Quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            await UpdateCartTotalAsync(cart);
            await _context.SaveChangesAsync();

            return cart;
        }

        public async Task<Cart> UpdateCartItemAsync(Guid userId, Guid itemId, UpdateCartRequest request)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item == null)
            {
                throw new ArgumentException("Item not found in cart");
            }

            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = request.Quantity;
            }

            await UpdateCartTotalAsync(cart);
            await _context.SaveChangesAsync();

            return cart;
        }

        public async Task<Cart> RemoveFromCartAsync(Guid userId, Guid itemId)
        {
            var cart = await GetCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == itemId);

            if (item != null)
            {
                cart.Items.Remove(item);
                await UpdateCartTotalAsync(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task ClearCartAsync(Guid userId)
        {
            var cart = await GetCartAsync(userId);
            cart.Items.Clear();
            cart.Price = 0;
            cart.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task UpdateCartTotalAsync(Cart cart)
        {
            cart.Price = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
            cart.UpdatedAt = DateTime.UtcNow;
        }

        private async Task<ProductInfo?> GetProductInfoAsync(Guid productId)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ProductApi");

                var response = await httpClient.GetAsync($"/api/products/{productId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProductInfo>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product info for product {ProductId}", productId);
            }

            return null;
        }

    }
}