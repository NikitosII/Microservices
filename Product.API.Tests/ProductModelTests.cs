using Product.API.Models;

namespace Product.API.Tests
{
    [TestClass]
    public class ProductModelTests
    {
        [TestMethod]
        public void Product_DefaultValues_AreCorrect()
        {
            // Act
            var product = new Products();

            // Assert
            Assert.AreEqual(Guid.Empty, product.Id);
            Assert.AreEqual(string.Empty, product.Name);
            Assert.AreEqual(string.Empty, product.Description);
            Assert.AreEqual(0m, product.Price);
            Assert.AreEqual(0, product.Stock);
            Assert.AreEqual(string.Empty, product.Category);
            Assert.AreEqual(string.Empty, product.ImageUrl);
            Assert.IsTrue(product.IsActive);
            Assert.IsNull(product.UpdatedAt);
        }

        [TestMethod]
        public void Product_CanSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddHours(1);

            // Act
            var product = new Products
            {
                Id = id,
                Name = "Test Product",
                Description = "Test Description",
                Price = 99.99m,
                Stock = 100,
                Category = "Electronics",
                ImageUrl = "http://example.com/image.jpg",
                IsActive = true,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.AreEqual(id, product.Id);
            Assert.AreEqual("Test Product", product.Name);
            Assert.AreEqual("Test Description", product.Description);
            Assert.AreEqual(99.99m, product.Price);
            Assert.AreEqual(100, product.Stock);
            Assert.AreEqual("Electronics", product.Category);
            Assert.AreEqual("http://example.com/image.jpg", product.ImageUrl);
            Assert.IsTrue(product.IsActive);
            Assert.AreEqual(createdAt, product.CreatedAt);
            Assert.AreEqual(updatedAt, product.UpdatedAt);
        }

        [TestMethod]
        public void Product_IsActive_DefaultsToTrue()
        {
            // Act
            var product = new Products();

            // Assert
            Assert.IsTrue(product.IsActive);
        }

        [TestMethod]
        public void Product_Price_CanBeDecimal()
        {
            // Arrange & Act
            var product = new Products
            {
                Price = 1234.56m
            };

            // Assert
            Assert.AreEqual(1234.56m, product.Price);
        }

        [TestMethod]
        public void Product_Price_CanBeZero()
        {
            // Arrange & Act
            var product = new Products
            {
                Price = 0m
            };

            // Assert
            Assert.AreEqual(0m, product.Price);
        }

        [TestMethod]
        public void Product_Stock_CanBeNegative()
        {
            // This test documents current behavior - stock can be negative
            // In a real scenario, you might want to add validation

            // Arrange & Act
            var product = new Products
            {
                Stock = -5
            };

            // Assert
            Assert.AreEqual(-5, product.Stock);
        }
    }
}
