using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ShoppingCart.API.Controllers;
using ShoppingCart.API.Models;
using ShoppingCart.API.Services;
using System.Security.Claims;

namespace ShoppingCart.API.Tests
{
    [TestClass]
    public class CartControllerTests
    {
        private Mock<ICartService> _cartServiceMock = null!;
        private Mock<ILogger<CartController>> _loggerMock = null!;
        private CartController _controller = null!;
        private Guid _testUserId;

        [TestInitialize]
        public void Setup()
        {
            _cartServiceMock = new Mock<ICartService>();
            _loggerMock = new Mock<ILogger<CartController>>();
            _controller = new CartController(_cartServiceMock.Object, _loggerMock.Object);
            _testUserId = Guid.NewGuid();

            // Setup authenticated user
            var claims = new List<Claim>
            {
                new Claim("sub", _testUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        #region GetCart Tests

        [TestMethod]
        public async Task GetCart_ReturnsOkResult_WithCart()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test Product", Quantity = 2, UnitPrice = 10.00m }
                },
                Price = 20.00m
            };

            _cartServiceMock.Setup(s => s.GetCartAsync(_testUserId))
                .ReturnsAsync(cart);

            // Act
            var result = await _controller.GetCart();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedCart = okResult.Value as Cart;
            Assert.IsNotNull(returnedCart);
            Assert.AreEqual(_testUserId, returnedCart.UserId);
            Assert.AreEqual(1, returnedCart.Items.Count);
        }

        [TestMethod]
        public async Task GetCart_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetCart();

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
        }

        #endregion

        #region AddToCart Tests

        [TestMethod]
        public async Task AddToCart_ReturnsOkResult_WithUpdatedCart()
        {
            // Arrange
            var request = new AddToCartRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 2
            };

            var updatedCart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = request.ProductId, ProductName = "New Product", Quantity = 2, UnitPrice = 15.00m }
                },
                Price = 30.00m
            };

            _cartServiceMock.Setup(s => s.AddToCartAsync(_testUserId, request))
                .ReturnsAsync(updatedCart);

            // Act
            var result = await _controller.AddToCart(request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedCart = okResult.Value as Cart;
            Assert.IsNotNull(returnedCart);
            Assert.AreEqual(1, returnedCart.Items.Count);
        }

        [TestMethod]
        public async Task AddToCart_ReturnsBadRequest_WhenProductNotFound()
        {
            // Arrange
            var request = new AddToCartRequest
            {
                ProductId = Guid.NewGuid(),
                Quantity = 1
            };

            _cartServiceMock.Setup(s => s.AddToCartAsync(_testUserId, request))
                .ThrowsAsync(new ArgumentException("Product not found"));

            // Act
            var result = await _controller.AddToCart(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual("Product not found", badRequestResult.Value);
        }

        #endregion

        #region UpdateCartItem Tests

        [TestMethod]
        public async Task UpdateCartItem_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new UpdateCartRequest { Quantity = 5 };

            var updatedCart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = itemId, ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 5, UnitPrice = 10.00m }
                },
                Price = 50.00m
            };

            _cartServiceMock.Setup(s => s.UpdateCartItemAsync(_testUserId, itemId, request))
                .ReturnsAsync(updatedCart);

            // Act
            var result = await _controller.UpdateCartItem(itemId, request);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedCart = okResult.Value as Cart;
            Assert.IsNotNull(returnedCart);
            Assert.AreEqual(5, returnedCart.Items.First().Quantity);
        }

        [TestMethod]
        public async Task UpdateCartItem_ReturnsBadRequest_WhenItemNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var request = new UpdateCartRequest { Quantity = 5 };

            _cartServiceMock.Setup(s => s.UpdateCartItemAsync(_testUserId, itemId, request))
                .ThrowsAsync(new ArgumentException("Item not found in cart"));

            // Act
            var result = await _controller.UpdateCartItem(itemId, request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
        }

        #endregion

        #region RemoveFromCart Tests

        [TestMethod]
        public async Task RemoveFromCart_ReturnsOkResult_WithUpdatedCart()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updatedCart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>(),
                Price = 0m
            };

            _cartServiceMock.Setup(s => s.RemoveFromCartAsync(_testUserId, itemId))
                .ReturnsAsync(updatedCart);

            // Act
            var result = await _controller.RemoveFromCart(itemId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedCart = okResult.Value as Cart;
            Assert.IsNotNull(returnedCart);
            Assert.AreEqual(0, returnedCart.Items.Count);
        }

        #endregion

        #region ClearCart Tests

        [TestMethod]
        public async Task ClearCart_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _cartServiceMock.Setup(s => s.ClearCartAsync(_testUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ClearCart();

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        #endregion

        #region GetCartItemCount Tests

        [TestMethod]
        public async Task GetCartItemCount_ReturnsCorrectCount()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>
                {
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 1", Quantity = 3, UnitPrice = 10.00m },
                    new CartItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Product 2", Quantity = 2, UnitPrice = 20.00m }
                },
                Price = 70.00m
            };

            _cartServiceMock.Setup(s => s.GetCartAsync(_testUserId))
                .ReturnsAsync(cart);

            // Act
            var result = await _controller.GetCartItemCount();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(5, okResult.Value); // 3 + 2 = 5 items total
        }

        [TestMethod]
        public async Task GetCartItemCount_ReturnsZero_WhenCartIsEmpty()
        {
            // Arrange
            var cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                Items = new List<CartItem>(),
                Price = 0m
            };

            _cartServiceMock.Setup(s => s.GetCartAsync(_testUserId))
                .ReturnsAsync(cart);

            // Act
            var result = await _controller.GetCartItemCount();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(0, okResult.Value);
        }

        #endregion
    }
}
