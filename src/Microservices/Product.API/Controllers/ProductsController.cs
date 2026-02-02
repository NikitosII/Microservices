using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Product.API.Data;
using Product.API.Models;

namespace Product.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly ProductContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Products>>> GetProducts()
        {
            try
            {
                var products = await _context.Entities
                    .Where(p => p.IsActive)
                    .ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Products>> GetProduct(Guid id)
        {
            try
            {
                var product = await _context.Entities.FindAsync(id);
                if (product == null || !product.IsActive)
                {
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
                return StatusCode(500, "An error occurred while retrieving the product");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Products>> CreateProduct(ProductCreateDto dto)
        {
            try
            {
                var product = new Products
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    Stock = dto.Stock,
                    Category = dto.Category,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Entities.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created with ID {ProductId}", product.Id);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, "An error occurred while creating the product");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(Guid id, ProductUpdateDto dto)
        {
            try
            {
                var product = await _context.Entities.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.Stock = dto.Stock;
                product.Category = dto.Category;
                product.ImageUrl = dto.ImageUrl;
                product.UpdatedAt = DateTime.UtcNow;
                product.IsActive = dto.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product updated with ID {ProductId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                return StatusCode(500, "An error occurred while updating the product");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var product = await _context.Entities.FindAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Product soft-deleted with ID {ProductId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                return StatusCode(500, "An error occurred while deleting the product");
            }
        }

        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Products>>> GetByCategory(string category)
        {
            try
            {
                var products = await _context.Entities
                    .Where(p => p.Category == category && p.IsActive)
                    .ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for category {Category}", category);
                return StatusCode(500, "An error occurred while retrieving products");
            }
        }
    }
}
