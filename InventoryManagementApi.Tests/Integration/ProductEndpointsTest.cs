using InventoryManagementApi.Data;
using InventoryManagementApi.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace InventoryManagementApi.Tests.Integration
{
    public class ProductEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ProductEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptorsToRemove = services
                        .Where(d =>
                            d.ServiceType == typeof(DbContextOptions<InventoryDbContext>) ||
                            d.ServiceType == typeof(InventoryDbContext) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)))
                        .ToList();

                    foreach (var descriptor in descriptorsToRemove)
                        services.Remove(descriptor);

                    services.AddDbContext<InventoryDbContext>(options =>
                        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
                });
            });
        }

        private HttpClient CreateClient() => _factory.CreateClient();

        private async Task<ProductResponseDto> CreateTestProductAsync(
            HttpClient client,
            string name = "Widget",
            string sku = "WGT-001",
            decimal price = 9.99m,
            int stock = 100)
        {
            var dto = new CreateProductDto(name, sku, price, stock);
            var response = await client.PostAsJsonAsync("/api/products", dto);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<ProductResponseDto>())!;
        }

        [Fact]
        public async Task CreateProduct_ValidRequest_Returns201()
        {
            // Arrange
            var client = CreateClient();
            var dto = new CreateProductDto("Widget", "WGT-001", 9.99m, 100);

            // Act
            var response = await client.PostAsJsonAsync("/api/products", dto);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal("Widget", product.Name);
            Assert.Equal("WGT-001", product.Sku);
        }

        [Fact]
        public async Task CreateProduct_DuplicateSku_Returns409()
        {
            // Arrange
            var client = CreateClient();
            await CreateTestProductAsync(client);

            // Act — try to create another product with the same SKU
            var dto = new CreateProductDto("Another Widget", "WGT-001", 14.99m, 50);
            var response = await client.PostAsJsonAsync("/api/products", dto);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_EmptyName_Returns400()
        {
            // Arrange
            var client = CreateClient();
            var dto = new CreateProductDto("", "WGT-001", 9.99m, 100);

            // Act
            var response = await client.PostAsJsonAsync("/api/products", dto);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsProducts()
        {
            // Arrange
            var client = CreateClient();
            await CreateTestProductAsync(client, "Apple", "APL-001");
            await CreateTestProductAsync(client, "Banana", "BAN-001");

            // Act
            var response = await client.GetAsync("/api/products");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
            Assert.NotNull(products);
            Assert.True(products.Count >= 2);
        }

        [Fact]
        public async Task GetProductById_ExistingProduct_Returns200()
        {
            // Arrange
            var client = CreateClient();
            var created = await CreateTestProductAsync(client);

            // Act
            var response = await client.GetAsync($"/api/products/{created.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal(created.Id, product.Id);
        }

        [Fact]
        public async Task GetProductById_NonExistentProduct_Returns404()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var response = await client.GetAsync("/api/products/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStock_ValidRequest_Returns200WithUpdatedStock()
        {
            // Arrange
            var client = CreateClient();
            var created = await CreateTestProductAsync(client);
            var dto = new UpdateStockDto(50);

            // Act
            var response = await client.PatchAsJsonAsync(
                $"/api/products/{created.Id}/stock", dto);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal(50, product.StockQuantity);
        }

        [Fact]
        public async Task DeleteProduct_ExistingProduct_Returns204()
        {
            // Arrange
            var client = CreateClient();
            var created = await CreateTestProductAsync(client);

            // Act
            var response = await client.DeleteAsync($"/api/products/{created.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProduct_NonExistentProduct_Returns404()
        {
            // Arrange
            var client = CreateClient();

            // Act
            var response = await client.DeleteAsync("/api/products/999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}