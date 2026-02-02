using Order.API.Models;

namespace Order.API.Tests
{
    [TestClass]
    public class OrderModelTests
    {
        [TestMethod]
        public void Orders_DefaultValues_AreCorrect()
        {
            // Act
            var order = new Orders();

            // Assert
            Assert.AreEqual(Guid.Empty, order.Id);
            Assert.AreEqual(Guid.Empty, order.UserId);
            Assert.AreEqual(string.Empty, order.OrderNumber);
            Assert.AreEqual(Orders.OrderStatus.Pending, order.Status);
            Assert.IsNotNull(order.Items);
            Assert.AreEqual(0, order.Items.Count);
            Assert.AreEqual(0m, order.Subtotal);
            Assert.AreEqual(0m, order.Tax);
            Assert.AreEqual(0m, order.ShippingCost);
            Assert.AreEqual(0m, order.Discount);
            Assert.AreEqual(0m, order.TotalAmount);
        }

        [TestMethod]
        public void Orders_CanSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createdAt = DateTime.UtcNow;

            // Act
            var order = new Orders
            {
                Id = id,
                UserId = userId,
                OrderNumber = "ORD-123456",
                Status = Orders.OrderStatus.Confirmed,
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = Guid.NewGuid(), ProductName = "Test", Quantity = 1, UnitPrice = 100m }
                },
                Subtotal = 100m,
                Tax = 10m,
                ShippingCost = 5m,
                Discount = 15m,
                TotalAmount = 100m,
                CreatedAt = createdAt
            };

            // Assert
            Assert.AreEqual(id, order.Id);
            Assert.AreEqual(userId, order.UserId);
            Assert.AreEqual("ORD-123456", order.OrderNumber);
            Assert.AreEqual(Orders.OrderStatus.Confirmed, order.Status);
            Assert.AreEqual(1, order.Items.Count);
            Assert.AreEqual(100m, order.Subtotal);
            Assert.AreEqual(10m, order.Tax);
            Assert.AreEqual(5m, order.ShippingCost);
            Assert.AreEqual(15m, order.Discount);
            Assert.AreEqual(100m, order.TotalAmount);
            Assert.AreEqual(createdAt, order.CreatedAt);
        }

        [TestMethod]
        public void OrderStatus_HasCorrectValues()
        {
            // Assert
            Assert.AreEqual(0, (int)Orders.OrderStatus.Pending);
            Assert.AreEqual(1, (int)Orders.OrderStatus.Confirmed);
            Assert.AreEqual(2, (int)Orders.OrderStatus.Processing);
            Assert.AreEqual(3, (int)Orders.OrderStatus.Shipped);
            Assert.AreEqual(4, (int)Orders.OrderStatus.Delivered);
            Assert.AreEqual(5, (int)Orders.OrderStatus.Cancelled);
            Assert.AreEqual(6, (int)Orders.OrderStatus.Refunded);
        }

        [TestMethod]
        public void OrderItem_DefaultValues_AreCorrect()
        {
            // Act
            var item = new OrderItem();

            // Assert
            Assert.AreEqual(Guid.Empty, item.Id);
            Assert.AreEqual(Guid.Empty, item.ProductId);
            Assert.AreEqual(string.Empty, item.ProductName);
            Assert.AreEqual(0m, item.UnitPrice);
            Assert.AreEqual(0, item.Quantity);
            Assert.AreEqual(0m, item.TotalPrice);
        }

        [TestMethod]
        public void OrderItem_CanSetAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var productId = Guid.NewGuid();

            // Act
            var item = new OrderItem
            {
                Id = id,
                ProductId = productId,
                ProductName = "Test Product",
                UnitPrice = 25.00m,
                Quantity = 4,
                TotalPrice = 100.00m
            };

            // Assert
            Assert.AreEqual(id, item.Id);
            Assert.AreEqual(productId, item.ProductId);
            Assert.AreEqual("Test Product", item.ProductName);
            Assert.AreEqual(25.00m, item.UnitPrice);
            Assert.AreEqual(4, item.Quantity);
            Assert.AreEqual(100.00m, item.TotalPrice);
        }

        [TestMethod]
        public void ShippingAddress_DefaultValues_AreCorrect()
        {
            // Act
            var address = new ShippingAddress();

            // Assert
            Assert.AreEqual(string.Empty, address.Street);
            Assert.AreEqual(string.Empty, address.City);
            Assert.AreEqual(string.Empty, address.State);
            Assert.AreEqual(string.Empty, address.ZipCode);
            Assert.AreEqual(string.Empty, address.Country);
        }

        [TestMethod]
        public void ShippingAddress_CanSetAllProperties()
        {
            // Act
            var address = new ShippingAddress
            {
                Street = "123 Main St",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            };

            // Assert
            Assert.AreEqual("123 Main St", address.Street);
            Assert.AreEqual("New York", address.City);
            Assert.AreEqual("NY", address.State);
            Assert.AreEqual("10001", address.ZipCode);
            Assert.AreEqual("USA", address.Country);
        }

        [TestMethod]
        public void PaymentInfo_CanSetAllProperties()
        {
            // Act
            var paymentInfo = new PaymentInfo
            {
                Method = "CreditCard",
                AmountPaid = 150.00m
            };

            // Assert
            Assert.AreEqual("CreditCard", paymentInfo.Method);
            Assert.AreEqual(150.00m, paymentInfo.AmountPaid);
        }
    }
}
