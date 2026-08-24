using Microsoft.EntityFrameworkCore;
using InventoryManagementApi.Data;
using InventoryManagementApi.Models;
using InventoryManagementApi.Services;

namespace InventoryManagementApi.Tests.Unit
{
    public class ProductServiceTests
    {
        private InventoryDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<InventoryDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new InventoryDbContext(options);
        }

        [Fact]
        public async Task CreateProductAsync_ValidProduct_ReturnsCreatedProduct()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var product = await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Assert
            Assert.NotNull(product);
            Assert.Equal("Widget", product.Name);
            Assert.Equal("WGT-001", product.Sku);
            Assert.Equal(9.99m, product.Price);
            Assert.Equal(100, product.StockQuantity);
        }

        [Fact]
        public async Task CreateProductAsync_DuplicateSku_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.CreateProductAsync("Another Widget", "WGT-001", 14.99m, 50));

            Assert.Contains("WGT-001", ex.Message);
        }

        [Fact]
        public async Task CreateProductAsync_EmptyName_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateProductAsync("", "WGT-001", 9.99m, 100));
        }

        [Fact]
        public async Task CreateProductAsync_EmptySku_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateProductAsync("Widget", "", 9.99m, 100));
        }

        [Fact]
        public async Task CreateProductAsync_NegativePrice_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateProductAsync("Widget", "WGT-001", -1.00m, 100));
        }

        [Fact]
        public async Task CreateProductAsync_NegativeStock_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.CreateProductAsync("Widget", "WGT-001", 9.99m, -1));
        }

        [Fact]
        public async Task CreateProductAsync_SkuStoredAsUpperCase()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var product = await service.CreateProductAsync("Widget", "wgt-001", 9.99m, 100);

            // Assert
            Assert.Equal("WGT-001", product.Sku);
        }

        [Fact]
        public async Task CreateProductAsync_ZeroPriceAllowed()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var product = await service.CreateProductAsync("Free Sample", "FREE-001", 0m, 50);

            // Assert
            Assert.Equal(0m, product.Price);
        }

        [Fact]
        public async Task GetProductByIdAsync_ExistingProduct_ReturnsProduct()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            var created = await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act
            var product = await service.GetProductByIdAsync(created.Id);

            // Assert
            Assert.NotNull(product);
            Assert.Equal(created.Id, product.Id);
        }

        [Fact]
        public async Task GetProductByIdAsync_NonExistentProduct_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var product = await service.GetProductByIdAsync(999);

            // Assert
            Assert.Null(product);
        }

        [Fact]
        public async Task GetProductBySkuAsync_ExistingProduct_ReturnsProduct()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act
            var product = await service.GetProductBySkuAsync("WGT-001");

            // Assert
            Assert.NotNull(product);
            Assert.Equal("WGT-001", product.Sku);
        }

        [Fact]
        public async Task GetAllProductsAsync_ReturnsProductsOrderedByName()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            await service.CreateProductAsync("Zebra", "ZBR-001", 9.99m, 10);
            await service.CreateProductAsync("Apple", "APL-001", 4.99m, 20);
            await service.CreateProductAsync("Mango", "MNG-001", 6.99m, 15);

            // Act
            var products = (await service.GetAllProductsAsync()).ToList();

            // Assert
            Assert.Equal(3, products.Count);
            Assert.Equal("Apple", products[0].Name);
            Assert.Equal("Mango", products[1].Name);
            Assert.Equal("Zebra", products[2].Name);
        }

        [Fact]
        public async Task UpdateStockAsync_ValidQuantity_UpdatesStock()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            var product = await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act
            var updated = await service.UpdateStockAsync(product.Id, 50);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal(50, updated.StockQuantity);
        }

        [Fact]
        public async Task UpdateStockAsync_NegativeQuantity_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            var product = await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.UpdateStockAsync(product.Id, -1));
        }

        [Fact]
        public async Task UpdateStockAsync_NonExistentProduct_ReturnsNull()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var result = await service.UpdateStockAsync(999, 50);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteProductAsync_ExistingProduct_ReturnsTrue()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);
            var product = await service.CreateProductAsync("Widget", "WGT-001", 9.99m, 100);

            // Act
            var result = await service.DeleteProductAsync(product.Id);

            // Assert
            Assert.True(result);
            Assert.Null(await service.GetProductByIdAsync(product.Id));
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistentProduct_ReturnsFalse()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var service = new ProductService(context);

            // Act
            var result = await service.DeleteProductAsync(999);

            // Assert
            Assert.False(result);
        }
    }
}