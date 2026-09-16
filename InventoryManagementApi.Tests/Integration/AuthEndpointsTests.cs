using System.Net;
using System.Net.Http.Json;
using InventoryManagementApi.Dtos;

namespace InventoryManagementApi.Tests.Integration
{
    public class AuthEndpointsTests
    {
        private HttpClient CreateClient()
        {
            var factory = new TestWebApplicationFactory();
            return factory.CreateClient();
        }

        [Fact]
        public async Task Register_ValidRequest_Returns200WithToken()
        {
            var client = CreateClient();
            var dto = new RegisterDto("testuser", "test@example.com", "Password123!");

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.Equal("Bearer", result.TokenType);
            Assert.Equal("testuser", result.Username);
            Assert.Equal("Staff", result.Role);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns409()
        {
            var client = CreateClient();
            var dto = new RegisterDto("user1", "duplicate@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", dto);

            var dto2 = new RegisterDto("user2", "duplicate@example.com", "Password123!");
            var response = await client.PostAsJsonAsync("/api/auth/register", dto2);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateUsername_Returns409()
        {
            var client = CreateClient();
            var dto = new RegisterDto("duplicateuser", "user1@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", dto);

            var dto2 = new RegisterDto("duplicateuser", "user2@example.com", "Password123!");
            var response = await client.PostAsJsonAsync("/api/auth/register", dto2);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Register_ShortPassword_Returns400()
        {
            var client = CreateClient();
            var dto = new RegisterDto("testuser", "test@example.com", "short");

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_Empty_Username_Returns400()
        {
            var client = CreateClient();
            var dto = new RegisterDto("", "test@example.com", "Password123!");

            var response = await client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_ValidCredentials_Returns200WithToken()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("loginuser", "login@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("login@example.com", "Password123!");
            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
        }

        public async Task Login_WrongPassword_Returns401()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("loginuser", "login@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("login@example.com", "WrongPassword!");
            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_NonExistentEmail_Returns401()
        {
            var client = CreateClient();
            var loginDto = new LoginDto("nobody@noone.com", "Password123!");

            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        public async Task Login_TokenCanAccessProtectedEndpoint()
        {
            var client = CreateClient();

            var registerDto = new RegisterDto("apiuser", "api@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("api@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var tokenResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult!.AccessToken);

            var response = await client.GetAsync("/api/products");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
