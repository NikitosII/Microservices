using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Order.API.Controllers;
using Order.API.Models;
using Order.API.Services;
using System.Security.Claims;

namespace Order.API.Tests
{
    [TestClass]
    public class OrdersControllerTests
    {
        private Mock<IOrderService> _orderServiceMock = null!;
        private Mock<ILogger<OrdersController>> _loggerMock = null!;
        private OrdersController _controller = null!;
        private Guid _testUserId;

        [TestInitialize]
        public void Setup()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<OrdersController>>();
            _controller = new OrdersController(_orderServiceMock.Object, _loggerMock.Object);
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

        #region GetOrders Tests

        [TestMethod]
        public async Task GetOrders_ReturnsOkResult_WithUserOrders()
        {
            // Arrange
            var orders = new List<Orders>
            {
                new Orders { Id = Guid.NewGuid(), UserId = _testUserId, OrderNumber = "ORD-001", Status = Orders.OrderStatus.Pending },
                new Orders { Id = Guid.NewGuid(), UserId = _testUserId, OrderNumber = "ORD-002", Status = Orders.OrderStatus.Confirmed }
            };

            _orderServiceMock.Setup(s => s.GetOrdersByUserIdAsync(_testUserId))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.GetOrders();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedOrders = okResult.Value as IEnumerable<Orders>;
            Assert.IsNotNull(returnedOrders);
            Assert.AreEqual(2, returnedOrders.Count());
        }

        [TestMethod]
        public async Task GetOrders_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange - Setup controller without user claims
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.GetOrders();

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(UnauthorizedResult));
        }

        #endregion

        #region GetOrder Tests

        [TestMethod]
        public async Task GetOrder_ReturnsOkResult_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100.00m
            };

            _orderServiceMock.Setup(s => s.GetByIdAsync(orderId, _testUserId))
                .ReturnsAsync(order);

            // Act
            var result = await _controller.GetOrder(orderId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedOrder = okResult.Value as Orders;
            Assert.IsNotNull(returnedOrder);
            Assert.AreEqual(orderId, returnedOrder.Id);
        }

        [TestMethod]
        public async Task GetOrder_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            _orderServiceMock.Setup(s => s.GetByIdAsync(nonExistentId, _testUserId))
                .ReturnsAsync((Orders?)null);

            // Act
            var result = await _controller.GetOrder(nonExistentId);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        #endregion

        #region CreateOrder Tests

        [TestMethod]
        public async Task CreateOrder_ReturnsCreatedResult_WithValidRequest()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                ShippingAddress = new ShippingAddress
                {
                    Street = "123 Test St",
                    City = "Test City",
                    State = "TS",
                    ZipCode = "12345",
                    Country = "USA"
                },
                PaymentMethod = "CreditCard"
            };

            var createdOrder = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrderNumber = "ORD-NEW-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 150.00m
            };

            _orderServiceMock.Setup(s => s.CreateOrderAsync(_testUserId, request))
                .ReturnsAsync(createdOrder);

            // Act
            var result = await _controller.CreateOrder(request);

            // Assert
            var createdResult = result.Result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(nameof(OrdersController.GetOrder), createdResult.ActionName);
        }

        [TestMethod]
        public async Task CreateOrder_ReturnsBadRequest_WhenCartIsEmpty()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                ShippingAddress = new ShippingAddress(),
                PaymentMethod = "CreditCard"
            };

            _orderServiceMock.Setup(s => s.CreateOrderAsync(_testUserId, request))
                .ThrowsAsync(new InvalidOperationException("Cart is empty"));

            // Act
            var result = await _controller.CreateOrder(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual("Cart is empty", badRequestResult.Value);
        }

        [TestMethod]
        public async Task CreateOrder_ReturnsBadRequest_WhenInvalidCoupon()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                ShippingAddress = new ShippingAddress(),
                PaymentMethod = "CreditCard",
                CouponCode = "INVALID"
            };

            _orderServiceMock.Setup(s => s.CreateOrderAsync(_testUserId, request))
                .ThrowsAsync(new ArgumentException("Invalid coupon code"));

            // Act
            var result = await _controller.CreateOrder(request);

            // Assert
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
        }

        #endregion

        #region UpdateOrderStatus Tests

        [TestMethod]
        public async Task UpdateOrderStatus_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest { Status = Orders.OrderStatus.Confirmed };

            _orderServiceMock.Setup(s => s.UpdateOrderStatusAsync(orderId, Orders.OrderStatus.Confirmed))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateOrderStatus(orderId, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task UpdateOrderStatus_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var request = new UpdateOrderStatusRequest { Status = Orders.OrderStatus.Confirmed };

            _orderServiceMock.Setup(s => s.UpdateOrderStatusAsync(orderId, Orders.OrderStatus.Confirmed))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateOrderStatus(orderId, request);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        #endregion

        #region GetOrderByNumber Tests

        [TestMethod]
        public async Task GetOrderByNumber_ReturnsOkResult_WhenOrderExists()
        {
            // Arrange
            var orderNumber = "ORD-123456";
            var order = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrderNumber = orderNumber,
                Status = Orders.OrderStatus.Pending
            };

            _orderServiceMock.Setup(s => s.GetByNumberAsync(orderNumber, _testUserId))
                .ReturnsAsync(order);

            // Act
            var result = await _controller.GetOrderByNumber(orderNumber);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var returnedOrder = okResult.Value as Orders;
            Assert.IsNotNull(returnedOrder);
            Assert.AreEqual(orderNumber, returnedOrder.OrderNumber);
        }

        [TestMethod]
        public async Task GetOrderByNumber_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var orderNumber = "NON-EXISTENT";
            _orderServiceMock.Setup(s => s.GetByNumberAsync(orderNumber, _testUserId))
                .ReturnsAsync((Orders?)null);

            // Act
            var result = await _controller.GetOrderByNumber(orderNumber);

            // Assert
            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        #endregion

        #region CancelOrder Tests

        [TestMethod]
        public async Task CancelOrder_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _orderServiceMock.Setup(s => s.CancelOrderAsync(orderId, _testUserId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelOrder(orderId);

            // Assert
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task CancelOrder_ReturnsBadRequest_WhenCannotCancel()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _orderServiceMock.Setup(s => s.CancelOrderAsync(orderId, _testUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.CancelOrder(orderId);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
        }

        [TestMethod]
        public async Task CancelOrder_ReturnsBadRequest_WhenOrderInWrongState()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            _orderServiceMock.Setup(s => s.CancelOrderAsync(orderId, _testUserId))
                .ThrowsAsync(new ArgumentException("Order cannot be cancelled in its current state"));

            // Act
            var result = await _controller.CancelOrder(orderId);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
        }

        #endregion
    }
}
