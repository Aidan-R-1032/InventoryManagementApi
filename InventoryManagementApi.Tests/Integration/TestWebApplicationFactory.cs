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
}
