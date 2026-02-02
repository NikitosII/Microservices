using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Order.API.Data;
using Order.API.Models;
using Order.API.Services;
using MassTransit;

namespace Order.API.Tests
{
    [TestClass]
    public class OrderServiceTests
    {
        private OrderContext _context = null!;
        private Mock<ILogger<OrderService>> _loggerMock = null!;
        private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
        private Mock<IPublishEndpoint> _publishEndpointMock = null!;
        private OrderService _orderService = null!;
        private Guid _testUserId;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new OrderContext(options);
            _loggerMock = new Mock<ILogger<OrderService>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _testUserId = Guid.NewGuid();

            _orderService = new OrderService(
                _context,
                _loggerMock.Object,
                _httpClientFactoryMock.Object,
                _publishEndpointMock.Object
            );
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region GetByIdAsync Tests

        [TestMethod]
        public async Task GetByIdAsync_ReturnsOrder_WhenOrderExistsForUser()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100m,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 100m }
                }
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.GetByIdAsync(orderId, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(orderId, result.Id);
            Assert.AreEqual(_testUserId, result.UserId);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenOrderDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _orderService.GetByIdAsync(nonExistentId, _testUserId);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetByIdAsync_ReturnsNull_WhenOrderBelongsToDifferentUser()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = differentUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100m
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.GetByIdAsync(orderId, _testUserId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetByNumberAsync Tests

        [TestMethod]
        public async Task GetByNumberAsync_ReturnsOrder_WhenOrderExists()
        {
            // Arrange
            var orderNumber = "ORD-TEST-123";
            var order = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrderNumber = orderNumber,
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 150m
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.GetByNumberAsync(orderNumber, _testUserId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(orderNumber, result.OrderNumber);
        }

        [TestMethod]
        public async Task GetByNumberAsync_ReturnsNull_WhenOrderDoesNotExist()
        {
            // Act
            var result = await _orderService.GetByNumberAsync("NON-EXISTENT", _testUserId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetOrdersByUserIdAsync Tests

        [TestMethod]
        public async Task GetOrdersByUserIdAsync_ReturnsOrdersForUser()
        {
            // Arrange
            var orders = new List<Orders>
            {
                new Orders { Id = Guid.NewGuid(), UserId = _testUserId, OrderNumber = "ORD-001", Status = Orders.OrderStatus.Pending, TotalAmount = 100m, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new Orders { Id = Guid.NewGuid(), UserId = _testUserId, OrderNumber = "ORD-002", Status = Orders.OrderStatus.Confirmed, TotalAmount = 200m, CreatedAt = DateTime.UtcNow },
                new Orders { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), OrderNumber = "ORD-003", Status = Orders.OrderStatus.Pending, TotalAmount = 50m, CreatedAt = DateTime.UtcNow }
            };

            await _context.Orders.AddRangeAsync(orders);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.GetOrdersByUserIdAsync(_testUserId);

            // Assert
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(o => o.UserId == _testUserId));
        }

        [TestMethod]
        public async Task GetOrdersByUserIdAsync_ReturnsOrderedByCreatedAtDescending()
        {
            // Arrange
            var olderOrder = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrderNumber = "ORD-OLD",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100m,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var newerOrder = new Orders
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrderNumber = "ORD-NEW",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 200m,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Orders.AddRangeAsync(olderOrder, newerOrder);
            await _context.SaveChangesAsync();

            // Act
            var result = (await _orderService.GetOrdersByUserIdAsync(_testUserId)).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("ORD-NEW", result[0].OrderNumber);
            Assert.AreEqual("ORD-OLD", result[1].OrderNumber);
        }

        #endregion

        #region UpdateOrderStatusAsync Tests

        [TestMethod]
        public async Task UpdateOrderStatusAsync_ReturnsTrue_WhenValidTransition()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100m
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, Orders.OrderStatus.Confirmed);

            // Assert
            Assert.IsTrue(result);
            var updatedOrder = await _context.Orders.FindAsync(orderId);
            Assert.IsNotNull(updatedOrder);
            Assert.AreEqual(Orders.OrderStatus.Confirmed, updatedOrder.Status);
        }

        [TestMethod]
        public async Task UpdateOrderStatusAsync_ReturnsFalse_WhenOrderDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(nonExistentId, Orders.OrderStatus.Confirmed);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateOrderStatusAsync_ReturnsFalse_WhenInvalidTransition()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Delivered, // Cannot transition from Delivered to Pending
                TotalAmount = 100m
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, Orders.OrderStatus.Pending);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region CancelOrderAsync Tests

        [TestMethod]
        public async Task CancelOrderAsync_ReturnsTrue_WhenOrderIsPending()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Pending,
                TotalAmount = 100m,
                Items = new List<OrderItem>()
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, _testUserId);

            // Assert
            Assert.IsTrue(result);
            var cancelledOrder = await _context.Orders.FindAsync(orderId);
            Assert.IsNotNull(cancelledOrder);
            Assert.AreEqual(Orders.OrderStatus.Cancelled, cancelledOrder.Status);
        }

        [TestMethod]
        public async Task CancelOrderAsync_ReturnsTrue_WhenOrderIsConfirmed()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Confirmed,
                TotalAmount = 100m,
                Items = new List<OrderItem>()
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act
            var result = await _orderService.CancelOrderAsync(orderId, _testUserId);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task CancelOrderAsync_ThrowsException_WhenOrderIsShipped()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var order = new Orders
            {
                Id = orderId,
                UserId = _testUserId,
                OrderNumber = "ORD-001",
                Status = Orders.OrderStatus.Shipped,
                TotalAmount = 100m,
                Items = new List<OrderItem>()
            };

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _orderService.CancelOrderAsync(orderId, _testUserId));
        }

        [TestMethod]
        public async Task CancelOrderAsync_ReturnsFalse_WhenOrderDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _orderService.CancelOrderAsync(nonExistentId, _testUserId);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion
    }
}
