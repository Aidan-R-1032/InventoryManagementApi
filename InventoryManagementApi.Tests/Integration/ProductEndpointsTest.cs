using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using InventoryManagementApi.Data;
using InventoryManagementApi.Dtos;

namespace InventoryManagementApi.Tests.Integration
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection;

        public TestWebApplicationFactory()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove all EF Core registrations
                var toRemove = services.Where(d =>
                    d.ServiceType.Namespace != null &&
                    (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                     d.ServiceType == typeof(InventoryDbContext)))
                    .ToList();

                foreach (var d in toRemove)
                    services.Remove(d);

                // Register with in-memory SQLite using shared connection
                services.AddDbContext<InventoryDbContext>(options =>
                    options.UseSqlite(_connection));

                // Create schema
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection.Close();
        }

        public string GenerateTestToken(string role = "Admin")
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("aN0cacp98dvva136nO9m12r7Fx6dK68dbM2V")); // TEMPORARY TESTING KEY
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
                new Claim(JwtRegisteredClaimNames.UniqueName, "testUser"),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                    issuer: "InventoryManagementApi",
                    audience: "InventoryManagementApiUsers",
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class ProductEndpointsTests
    {
        private HttpClient CreateClient(string role = "Admin")
        {
            var factory = new TestWebApplicationFactory();
            var client = factory.CreateClient();
            var token = factory.GenerateTestToken(role);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }
            

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
            var client = CreateClient("Admin");
            var dto = new CreateProductDto("Widget", "WGT-001", 9.99m, 100);

            var response = await client.PostAsJsonAsync("/api/products", dto);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal("Widget", product.Name);
            Assert.Equal("WGT-001", product.Sku);
        }

        [Fact]
        public async Task CreateProduct_DuplicateSku_Returns409()
        {
            var client = CreateClient("Admin");
            await CreateTestProductAsync(client);

            var dto = new CreateProductDto("Another Widget", "WGT-001", 14.99m, 50);
            var response = await client.PostAsJsonAsync("/api/products", dto);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_EmptyName_Returns400()
        {
            var client = CreateClient("Admin");
            var dto = new CreateProductDto("", "WGT-001", 9.99m, 100);

            var response = await client.PostAsJsonAsync("/api/products", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsProducts()
        {
            var client = CreateClient("Admin");
            await CreateTestProductAsync(client, "Apple", "APL-001");
            await CreateTestProductAsync(client, "Banana", "BAN-001");

            var response = await client.GetAsync("/api/products");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var products = await response.Content.ReadFromJsonAsync<List<ProductResponseDto>>();
            Assert.NotNull(products);
            Assert.True(products.Count >= 2);
        }

        [Fact]
        public async Task GetProductById_ExistingProduct_Returns200()
        {
            var client = CreateClient("Admin");
            var created = await CreateTestProductAsync(client);

            var response = await client.GetAsync($"/api/products/{created.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal(created.Id, product.Id);
        }

        [Fact]
        public async Task GetProductById_NonExistentProduct_Returns404()
        {
            var client = CreateClient("Admin");

            var response = await client.GetAsync("/api/products/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateStock_ValidRequest_Returns200WithUpdatedStock()
        {
            var client = CreateClient("Admin");
            var created = await CreateTestProductAsync(client);
            var dto = new UpdateStockDto(50);

            var response = await client.PatchAsJsonAsync(
                $"/api/products/{created.Id}/stock", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var product = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            Assert.NotNull(product);
            Assert.Equal(50, product.StockQuantity);
        }

        [Fact]
        public async Task DeleteProduct_ExistingProduct_Returns204()
        {
            var client = CreateClient("Admin");
            var created = await CreateTestProductAsync(client);

            var response = await client.DeleteAsync($"/api/products/{created.Id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DeleteProduct_NonExistentProduct_Returns404()
        {
            var client = CreateClient("Admin");

            var response = await client.DeleteAsync("/api/products/999");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreateProduct_WithStaffRole_Returns403()
        {
            var staffClient = CreateClient("Staff");
            var dto = new CreateProductDto("Widget", "WGT-001", 9.99m, 100);

            var response = await staffClient.PostAsJsonAsync("/api/products", dto);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetAllProducts_WithNoToken_Returns401()
        {
            var factory = new TestWebApplicationFactory();
            var client = factory.CreateClient();

            var response = await client.GetAsync("/api/products");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}