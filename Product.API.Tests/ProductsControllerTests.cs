using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Product.API.Controllers;
using Product.API.Data;
using Product.API.Models;

namespace Product.API.Tests
{
    [TestClass]
    public class ProductsControllerTests
    {
        private ProductContext _context = null!;
        private ProductsController _controller = null!;
        private Mock<ILogger<ProductsController>> _loggerMock = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ProductContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ProductContext(options);
            _loggerMock = new Mock<ILogger<ProductsController>>();
            _controller = new ProductsController(_context, _loggerMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetProducts Tests

        [TestMethod]
        public async Task GetProducts_ReturnsOkResult_WithActiveProducts()
        {
            // Arrange
            var products = new List<Models.Products>
            {
                new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10.99m, IsActive = true, Category = "Electronics" },
                new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 20.99m, IsActive = true, Category = "Clothing" },
                new() { Id = Guid.NewGuid(), Name = "Inactive Product", Price = 5.99m, IsActive = false, Category = "Electronics" }
            };

            await _context.Entities.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProducts = okResult.Value as IEnumerable<Models.Products>;
            Assert.IsNotNull(returnedProducts);
            Assert.AreEqual(2, returnedProducts.Count());
        }

        [TestMethod]
        public async Task GetProducts_ReturnsEmptyList_WhenNoActiveProducts()
        {
            // Arrange - no products added

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProducts = okResult.Value as IEnumerable<Models.Products>;
            Assert.IsNotNull(returnedProducts);
            Assert.AreEqual(0, returnedProducts.Count());
        }

        #endregion

        #region GetProduct Tests

        [TestMethod]
        public async Task GetProduct_ReturnsOkResult_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Models.Products
            {
                Id = productId,
                Name = "Test Product",
                Price = 15.99m,
                IsActive = true,
                Category = "Test"
            };

            await _context.Entities.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProduct = okResult.Value as Models.Products;
            Assert.IsNotNull(returnedProduct);
            Assert.AreEqual(productId, returnedProduct.Id);
            Assert.AreEqual("Test Product", returnedProduct.Name);
        }

        [TestMethod]
        public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _controller.GetProduct(nonExistentId);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task GetProduct_ReturnsNotFound_WhenProductIsInactive()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Models.Products
            {
                Id = productId,
                Name = "Inactive Product",
                Price = 15.99m,
                IsActive = false,
                Category = "Test"
            };

            await _context.Entities.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        #endregion

        #region CreateProduct Tests

        [TestMethod]
        public async Task CreateProduct_ReturnsCreatedResult_WithValidData()
        {
            // Arrange
            var createDto = new ProductCreateDto
            {
                Name = "New Product",
                Description = "Test Description",
                Price = 25.99m,
                Stock = 100,
                Category = "Electronics",
                ImageUrl = "http://example.com/image.jpg"
            };

            // Act
            var result = await _controller.CreateProduct(createDto);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(nameof(ProductsController.GetProduct), createdResult.ActionName);

            var createdProduct = createdResult.Value as Models.Products;
            Assert.IsNotNull(createdProduct);
            Assert.AreEqual("New Product", createdProduct.Name);
            Assert.AreEqual(25.99m, createdProduct.Price);
            Assert.IsTrue(createdProduct.IsActive);
        }

        [TestMethod]
        public async Task CreateProduct_SavesToDatabase()
        {
            // Arrange
            var createDto = new ProductCreateDto
            {
                Name = "Database Test Product",
                Description = "Test",
                Price = 10.00m,
                Stock = 50,
                Category = "Test",
                ImageUrl = ""
            };

            // Act
            await _controller.CreateProduct(createDto);

            // Assert
            var savedProduct = await _context.Entities.FirstOrDefaultAsync(p => p.Name == "Database Test Product");
            Assert.IsNotNull(savedProduct);
            Assert.AreEqual(50, savedProduct.Stock);
        }

        #endregion

        #region UpdateProduct Tests

        [TestMethod]
        public async Task UpdateProduct_ReturnsNoContent_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Models.Products
            {
                Id = productId,
                Name = "Original Name",
                Price = 10.00m,
                IsActive = true,
                Category = "Test"
            };

            await _context.Entities.AddAsync(product);
            await _context.SaveChangesAsync();

            var updateDto = new ProductUpdateDto
            {
                Name = "Updated Name",
                Description = "Updated Description",
                Price = 20.00m,
                Stock = 200,
                Category = "Updated Category",
                ImageUrl = "",
                IsActive = true
            };

            // Act
            var result = await _controller.UpdateProduct(productId, updateDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));

            // Verify update was saved
            var updatedProduct = await _context.Entities.FindAsync(productId);
            Assert.IsNotNull(updatedProduct);
            Assert.AreEqual("Updated Name", updatedProduct.Name);
            Assert.AreEqual(20.00m, updatedProduct.Price);
        }

        [TestMethod]
        public async Task UpdateProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateDto = new ProductUpdateDto
            {
                Name = "Test",
                Description = "",
                Price = 10.00m,
                Stock = 10,
                Category = "Test",
                ImageUrl = "",
                IsActive = true
            };

            // Act
            var result = await _controller.UpdateProduct(nonExistentId, updateDto);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        #endregion

        #region DeleteProduct Tests

        [TestMethod]
        public async Task DeleteProduct_SoftDeletesProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Models.Products
            {
                Id = productId,
                Name = "Product to Delete",
                Price = 10.00m,
                IsActive = true,
                Category = "Test"
            };

            await _context.Entities.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));

            // Verify product is soft-deleted (IsActive = false)
            var deletedProduct = await _context.Entities.FindAsync(productId);
            Assert.IsNotNull(deletedProduct);
            Assert.IsFalse(deletedProduct.IsActive);
            Assert.IsNotNull(deletedProduct.UpdatedAt);
        }

        [TestMethod]
        public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _controller.DeleteProduct(nonExistentId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        #endregion

        #region GetByCategory Tests

        [TestMethod]
        public async Task GetByCategory_ReturnsProductsInCategory()
        {
            // Arrange
            var products = new List<Models.Products>
            {
                new() { Id = Guid.NewGuid(), Name = "Electronics 1", Price = 100m, IsActive = true, Category = "Electronics" },
                new() { Id = Guid.NewGuid(), Name = "Electronics 2", Price = 200m, IsActive = true, Category = "Electronics" },
                new() { Id = Guid.NewGuid(), Name = "Clothing 1", Price = 50m, IsActive = true, Category = "Clothing" },
                new() { Id = Guid.NewGuid(), Name = "Inactive Electronics", Price = 150m, IsActive = false, Category = "Electronics" }
            };

            await _context.Entities.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetByCategory("Electronics");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProducts = okResult.Value as IEnumerable<Models.Products>;
            Assert.IsNotNull(returnedProducts);
            Assert.AreEqual(2, returnedProducts.Count());
            Assert.IsTrue(returnedProducts.All(p => p.Category == "Electronics"));
        }

        [TestMethod]
        public async Task GetByCategory_ReturnsEmptyList_WhenNoCategoryMatch()
        {
            // Arrange
            var product = new Models.Products
            {
                Id = Guid.NewGuid(),
                Name = "Test",
                Price = 10m,
                IsActive = true,
                Category = "Electronics"
            };

            await _context.Entities.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetByCategory("NonExistentCategory");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedProducts = okResult.Value as IEnumerable<Models.Products>;
            Assert.IsNotNull(returnedProducts);
            Assert.AreEqual(0, returnedProducts.Count());
        }

        #endregion
    }
}
