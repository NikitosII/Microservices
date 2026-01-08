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
                var products = await _context.Products
                    .Where(x => x.IsActive)
                    .ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Products>> GetProduct(Guid id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null || !product.IsActive)
                {
                    return NotFound();
                }
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Products>> CreateProduct(ProductCreateDto product)
        {
            try
            {
                var thing = new Products
                {
                    Id = Guid.NewGuid(),
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                _context.Products.Add(thing);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = thing.Id }, thing);

            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpPut("id")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(Guid id, ProductUpdateDto product)
        {
            try
            {
                var thing = await _context.Products.FindAsync(id);
                if (thing == null)
                {
                    return NotFound();
                }

                thing.Name = product.Name;
                thing.Description = product.Description;
                thing.Price = product.Price;
                thing.Stock = product.Stock;
                thing.Category = product.Category;
                thing.ImageUrl = product.ImageUrl;
                thing.CreatedAt = DateTime.Now;
                thing.IsActive = product.IsActive;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var thing = await _context.Products.FindAsync(id);
                if (thing == null)
                {
                    return NotFound();
                }
                thing.IsActive = false;
                thing.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500);
            }

        }

        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Products>>> GetByCategory(string category)
        {
            try
            {
                var thing = await _context.Products
                    .Where(x => x.Category == category && x.IsActive)
                    .ToListAsync();
                return Ok(thing);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                return StatusCode(500);
            }
        }
    }

}


