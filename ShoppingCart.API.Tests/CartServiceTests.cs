using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using ShoppingCart.API.Data;
using ShoppingCart.API.Models;
using ShoppingCart.API.Services;
using System.Net;
using System.Text.Json;

namespace ShoppingCart.API.Tests
{
    [TestClass]
    public class CartServiceTests
    {
        private CartContext _context = null!;
        private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
        private Mock<ILogger<CartService>> _loggerMock = null!;
        private CartService _cartService = null!;
        private Guid _testUserId;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<CartContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new CartContext(options);
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<CartService>>();
            _testUserId = Guid.NewGuid();

            _cartService = new CartService(_context, _httpClientFactoryMock.Object, _loggerMock.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetCartAsync Tests

        [TestMethod]
        public async Task GetCartAsync_ReturnsExistingCart_WhenCartExists()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 10m }
                },
                Price = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cartService.GetCartAsync(_testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testUserId, result.UserId);
            Assert.AreEqual(1, result.Items.Count);
        }

        [TestMethod]
        public async Task GetCartAsync_CreatesNewCart_WhenCartDoesNotExist()
        {
            // Act
            var result = await _cartService.GetCartAsync(_testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(_testUserId, result.UserId);
            Assert.AreEqual(0, result.Items.Count);

            // Verify cart was saved to database
            var savedCart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == _testUserId);
            Assert.IsNotNull(savedCart);
        }

        #endregion

        #region UpdateCartItemAsync Tests

        [TestMethod]
        public async Task UpdateCartItemAsync_UpdatesQuantity_WhenItemExists()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = itemId, ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 10m }
                },
                Price = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            var request = new UpdateCartRequest { Quantity = 5 };

            // Act
            var result = await _cartService.UpdateCartItemAsync(_testUserId, itemId, request);

            // Assert
            Assert.IsNotNull(result);
            var updatedItem = result.Items.First(i => i.Id == itemId);
            Assert.AreEqual(5, updatedItem.Quantity);
            Assert.AreEqual(50m, result.Price);
        }

        [TestMethod]
        public async Task UpdateCartItemAsync_RemovesItem_WhenQuantityIsZero()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = itemId, ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 10m }
                },
                Price = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            var request = new UpdateCartRequest { Quantity = 0 };

            // Act
            var result = await _cartService.UpdateCartItemAsync(_testUserId, itemId, request);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Items.Count);
        }

        [TestMethod]
        public async Task UpdateCartItemAsync_ThrowsException_WhenItemNotFound()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>(),
                Price = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            var request = new UpdateCartRequest { Quantity = 5 };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _cartService.UpdateCartItemAsync(_testUserId, Guid.NewGuid(), request));
        }

        #endregion

        #region RemoveFromCartAsync Tests

        [TestMethod]
        public async Task RemoveFromCartAsync_RemovesItem_WhenItemExists()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = itemId, ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 10m },
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test 2", Quantity = 2, UnitPrice = 20m }
                },
                Price = 50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cartService.RemoveFromCartAsync(_testUserId, itemId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.IsFalse(result.Items.Any(i => i.Id == itemId));
        }

        [TestMethod]
        public async Task RemoveFromCartAsync_DoesNothing_WhenItemDoesNotExist()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 10m }
                },
                Price = 10m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            // Act
            var result = await _cartService.RemoveFromCartAsync(_testUserId, Guid.NewGuid());

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
        }

        #endregion

        #region ClearCartAsync Tests

        [TestMethod]
        public async Task ClearCartAsync_RemovesAllItems()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test 1", Quantity = 1, UnitPrice = 10m },
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test 2", Quantity = 2, UnitPrice = 20m }
                },
                Price = 50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();

            // Act
            await _cartService.ClearCartAsync(_testUserId);

            // Assert
            var clearedCart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == _testUserId);
            Assert.IsNotNull(clearedCart);
            Assert.AreEqual(0, clearedCart.Items.Count);
            Assert.AreEqual(0m, clearedCart.Price);
        }

        #endregion
    }
}
