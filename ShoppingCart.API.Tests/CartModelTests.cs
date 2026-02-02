using ShoppingCart.API.Models;

namespace ShoppingCart.API.Tests
{
    [TestClass]
    public class CartModelTests
    {
        [TestMethod]
        public void Cart_DefaultValues_AreCorrect()
        {
            // Act
            var cart = new Cart();

            // Assert
            Assert.AreEqual(Guid.Empty, cart.Id);
            Assert.AreEqual(Guid.Empty, cart.UserId);
            Assert.IsNotNull(cart.Items);
            Assert.AreEqual(0, cart.Items.Count);
            Assert.AreEqual(0m, cart.Price);
        }

        [TestMethod]
        public void Cart_CanSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddMinutes(30);

            // Act
            var cart = new Cart
            {
                Id = id,
                UserId = userId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductName = "Test", Quantity = 2, UnitPrice = 10m }
                },
                Price = 20m,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            };

            // Assert
            Assert.AreEqual(id, cart.Id);
            Assert.AreEqual(userId, cart.UserId);
            Assert.AreEqual(1, cart.Items.Count);
            Assert.AreEqual(20m, cart.Price);
            Assert.AreEqual(createdAt, cart.CreatedAt);
            Assert.AreEqual(updatedAt, cart.UpdatedAt);
        }

        [TestMethod]
        public void Cart_TotalPrice_CalculatesCorrectly()
        {
            // Arrange
            var cart = new Cart
            {
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductName = "Item 1", Quantity = 2, UnitPrice = 10m },
                    new CartItem { Id = Guid.NewGuid(), ProductName = "Item 2", Quantity = 3, UnitPrice = 20m }
                }
            };

            // Act
            var totalPrice = cart.TotalPrice;

            // Assert
            Assert.AreEqual(80m, totalPrice); // (2 * 10) + (3 * 20) = 20 + 60 = 80
        }

        [TestMethod]
        public void Cart_TotalPrice_ReturnsZero_WhenEmpty()
        {
            // Arrange
            var cart = new Cart
            {
                Items = new List<CartItem>()
            };

            // Act
            var totalPrice = cart.TotalPrice;

            // Assert
            Assert.AreEqual(0m, totalPrice);
        }

        [TestMethod]
        public void CartItem_DefaultValues_AreCorrect()
        {
            // Act
            var item = new CartItem();

            // Assert
            Assert.AreEqual(Guid.Empty, item.Id);
            Assert.AreEqual(Guid.Empty, item.ProductId);
            Assert.AreEqual(string.Empty, item.ProductName);
            Assert.AreEqual(0m, item.UnitPrice);
            Assert.AreEqual(0, item.Quantity);
            Assert.AreEqual(string.Empty, item.ImageUrl);
        }

        [TestMethod]
        public void CartItem_CanSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var productId = Guid.NewGuid();

            // Act
            var item = new CartItem
            {
                Id = id,
                ProductId = productId,
                ProductName = "Test Product",
                UnitPrice = 15.99m,
                Quantity = 3,
                ImageUrl = "http://example.com/image.jpg"
            };

            // Assert
            Assert.AreEqual(id, item.Id);
            Assert.AreEqual(productId, item.ProductId);
            Assert.AreEqual("Test Product", item.ProductName);
            Assert.AreEqual(15.99m, item.UnitPrice);
            Assert.AreEqual(3, item.Quantity);
            Assert.AreEqual("http://example.com/image.jpg", item.ImageUrl);
        }

        [TestMethod]
        public void AddToCartRequest_CanSetAllProperties()
        {
            // Arrange
            var productId = Guid.NewGuid();

            // Act
            var request = new AddToCartRequest
            {
                ProductId = productId,
                Quantity = 5
            };

            // Assert
            Assert.AreEqual(productId, request.ProductId);
            Assert.AreEqual(5, request.Quantity);
        }

        [TestMethod]
        public void UpdateCartRequest_DefaultValues_AreCorrect()
        {
            // Act
            var request = new UpdateCartRequest();

            // Assert
            Assert.AreEqual(0, request.Quantity);
        }

        [TestMethod]
        public void UpdateCartRequest_CanSetQuantity()
        {
            // Act
            var request = new UpdateCartRequest
            {
                Quantity = 10
            };

            // Assert
            Assert.AreEqual(10, request.Quantity);
        }
    }
}
