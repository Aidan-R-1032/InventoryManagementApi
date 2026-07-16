using Microsoft.EntityFrameworkCore;
using InventoryManagementApi.Data;
using InventoryManagementApi.Models;
using InventoryManagementApi.Services;

namespace InventoryManagementApi.Tests.Unit
{
    public class OrderServiceTests
    {
        private InventoryDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())       // gives each test its own isolated in-memory database 
                .Options;

            return new InventoryDbContext(options);
        }

        private Product CreateTestProduct(int id, string name, decimal price, int stock)
        {
            return new Product
            {
                Id = id,
                Name = name,
                Sku = $"SKU-{id}",
                Price = price,
                StockQuantity = stock
            };
        }

        // Test Structure: Arrange, Act, Assert
        // Arrange: Set up the context and service, and seed the database with test data
        // Act: Call the method under test
        // Assert: Verify the results are what you would expect

        [Fact] // xUnit's attribute for a single test case with no parameters
        public async Task PlaceOrderAsync_ValidOrder_CreatesOrderAndDecrementsStock()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 10);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 3) };

            // Act
            var order = await service.PlaceOrderAsync("John Doe", items);

            // Assert
            Assert.NotNull(order);
            Assert.Equal(OrderStatus.Confirmed, order.Status);
            Assert.Single(order.OrderItems);
            Assert.Equal(7, context.Products.Find(1)!.StockQuantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_InsufficientStock_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 2);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 5) };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.PlaceOrderAsync("John Doe", items));

            Assert.Contains("Insufficient stock", ex.Message);
        }

        [Fact]
        public async Task PlaceOrderAsync_ExactStockQuantity_Succeeds()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 5);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 5) };

            // Act
            var order = await service.PlaceOrderAsync("John Doe", items);

            // Assert
            Assert.NotNull(order);
            Assert.Equal(0, context.Products.Find(1)!.StockQuantity);
        }

        [Fact]
        public async Task PlaceOrderAsync_EmptyCustomerName_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 1) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.PlaceOrderAsync("", items));
        }

        [Fact]
        public async Task PlaceOrderAsync_EmptyItemsList_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new OrderService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.PlaceOrderAsync("John Doe", new List<OrderItemRequest>()));
        }

        [Fact]
        public async Task PlaceOrderAsync_ZeroQuantity_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 10);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 0) };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.PlaceOrderAsync("John Doe", items));
        }

        [Fact]
        public async Task PlaceOrderAsync_NonExistentProduct_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(999, 1) };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.PlaceOrderAsync("John Doe", items));
        }

        [Fact]
        public async Task PlaceOrderAsync_SnapshotsPriceAtTimeOfOrder()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 10);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemRequest> { new(1, 1) };

            // Act
            var order = await service.PlaceOrderAsync("John Doe", items);

            // Change the price after ordering
            product.Price = 99.99m;
            await context.SaveChangesAsync();

            // Assert — order item should still reflect original price
            Assert.Equal(9.99m, order.OrderItems.First().UnitPriceAtOrderTime);
        }

        [Fact]
        public async Task CancelOrderAsync_ValidOrder_RestoresStock()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 10);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var order = await service.PlaceOrderAsync("John Doe", new List<OrderItemRequest> { new(1, 3) });

            // Act
            var result = await service.CancelOrderAsync(order.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(OrderStatus.Cancelled, context.Orders.Find(order.Id)!.Status);
            Assert.Equal(10, context.Products.Find(1)!.StockQuantity);
        }

        [Fact]
        public async Task CancelOrderAsync_AlreadyCancelled_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var product = CreateTestProduct(1, "Widget", 9.99m, 10);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var order = await service.PlaceOrderAsync("John Doe", new List<OrderItemRequest> { new(1, 1) });
            await service.CancelOrderAsync(order.Id);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CancelOrderAsync(order.Id));
        }
    }
}