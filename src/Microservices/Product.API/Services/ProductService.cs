using Microsoft.EntityFrameworkCore;
using Product.API.Data;
using Product.API.Models;

namespace Product.API.Services
{

    public class ProductService
    {
        private readonly ProductContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ProductContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Products>> GetAllProductsAsync()
        {
            try
            {
                return await _context.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all products");
                throw;
            }
        }

        public async Task<Products?> GetProductByIdAsync(Guid id)
        {
            try
            {
                return await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by id {ProductId}", id);
                throw;
            }
        }

        public async Task<Products> CreateProductAsync(ProductCreateDto productDto)
        {
            try
            {
                var product = new Products
                {
                    Id = Guid.NewGuid(),
                    Name = productDto.Name,
                    Description = productDto.Description,
                    Price = productDto.Price,
                    Stock = productDto.Stock,
                    Category = productDto.Category,
                    ImageUrl = productDto.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created: {ProductId}", product.Id);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<Products?> UpdateProductAsync(Guid id, ProductUpdateDto productDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return null;
                }

                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.Price = productDto.Price;
                product.Stock = productDto.Stock;
                product.Category = productDto.Category;
                product.ImageUrl = productDto.ImageUrl;
                product.IsActive = productDto.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductId}", id);
                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return false;
                }

                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product deleted: {ProductId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> UpdateProductStockAsync(Guid productId, int quantity)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return false;
                }

                product.Stock += quantity;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated: {ProductId}, new quantity: {Quantity}",
                    productId, product.Stock);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {ProductId}", productId);
                throw;
            }
        }

        public async Task<IEnumerable<Products>> GetProductsByCategoryAsync(string category)
        {
            try
            {
                return await _context.Products
                    .Where(p => p.Category == category && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category {Category}", category);
                throw;
            }
        }

        public async Task<bool> ProductExistsAsync(Guid id)
        {
            return await _context.Products.AnyAsync(p => p.Id == id && p.IsActive);
        }
    }
}