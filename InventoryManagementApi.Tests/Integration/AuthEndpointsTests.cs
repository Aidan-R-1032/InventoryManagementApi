using System.Net;
using System.Net.Http.Json;
using InventoryManagementApi.Dtos;
using InventoryManagementApi.Data;
using InventoryManagementApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagementApi.Tests.Integration
{
    public class AuthEndpointsTests
    {
        private TestWebApplicationFactory _factory;
        private HttpClient CreateClient()
        {
            var factory = new TestWebApplicationFactory();
            _factory = factory;
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
        
        [Fact]
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

        [Fact]
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

        // Passwords and Reset Tokens
        [Fact]
        public async Task ForgotPassword_RegisteredEmail_Returns200()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("resetuser", "reset@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var dto = new ForgotPasswordDto("reset@example.com");
            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_UnregisteredEmail_StillReturns200()
        {
            var client = CreateClient();
            var dto = new ForgotPasswordDto("nobody@example.com");

            var response = await client.PostAsJsonAsync("/api/auth/forgot-password", dto);

            // Must return 200 regardless — prevents email enumeration
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_ValidToken_Returns200()
        {
            var client = CreateClient();

            // Register user
            var registerDto = new RegisterDto("pwreset", "pwreset@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Create a scope from the SAME factory used by CreateClient()
            using var scope = _factory.Services.CreateScope();

            // Request reset token via service directly
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.ForgotPasswordAsync("pwreset@example.com");

            // Retrieve the token from the SAME database
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var token = await db.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync();

            // Reset the password using the SAME client
            var resetDto = new ResetPasswordDto(token.Token, "NewPassword123!");
            var response = await client.PostAsJsonAsync("/api/auth/reset-password", resetDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_Returns400()
        {
            var client = CreateClient();
            var dto = new ResetPasswordDto("invalid-token-that-does-not-exist", "NewPassword123!");

            var response = await client.PostAsJsonAsync("/api/auth/reset-password", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_WeakPassword_Returns400()
        {
            var client = CreateClient();
            var dto = new ResetPasswordDto("sometoken", "weak");

            var response = await client.PostAsJsonAsync("/api/auth/reset-password", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_NewPasswordWorks()
        {
            var factory = new TestWebApplicationFactory();
            var client = factory.CreateClient();

            // Register
            var registerDto = new RegisterDto("loginafter", "loginafter@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Get token via service
            var scope = factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.ForgotPasswordAsync("loginafter@example.com");

            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var token = await db.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync();

            // Reset password
            var resetDto = new ResetPasswordDto(token.Token, "NewPassword123!");
            await client.PostAsJsonAsync("/api/auth/reset-password", resetDto);

            // Login with new password
            var loginDto = new LoginDto("loginafter@example.com", "NewPassword123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_TokenCannotBeReused()
        {
            var factory = new TestWebApplicationFactory();
            var client = factory.CreateClient();

            // Register
            var registerDto = new RegisterDto("reusetest", "reuse@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Get token
            var scope = factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await authService.ForgotPasswordAsync("reuse@example.com");

            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var token = await db.PasswordResetTokens
                .OrderByDescending(t => t.CreatedAt)
                .FirstAsync();

            // Use token once
            var resetDto = new ResetPasswordDto(token.Token, "NewPassword123!");
            await client.PostAsJsonAsync("/api/auth/reset-password", resetDto);

            // Try to use the same token again
            var resetDto2 = new ResetPasswordDto(token.Token, "AnotherPassword123!");
            var response = await client.PostAsJsonAsync("/api/auth/reset-password", resetDto2);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsRefreshToken()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("refreshuser", "refresh@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("refresh@example.com", "Password123!");
            var response = await client.PostAsJsonAsync("/api/auth/login", loginDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.RefreshToken);
            Assert.NotEmpty(result.AccessToken);
        }

        [Fact]
        public async Task RefreshToken_ValidToken_ReturnsNewTokenPair()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("refreshuser2", "refresh2@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("refresh2@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            var refreshDto = new RefreshTokenDto(loginResult!.RefreshToken);
            var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            // New refresh token must be different from the old one
            Assert.NotEqual(loginResult.RefreshToken, result.RefreshToken);
        }

        [Fact]
        public async Task RefreshToken_OldTokenIsInvalidatedAfterRotation()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("refreshuser3", "refresh3@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("refresh3@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            // Use the refresh token once
            var refreshDto = new RefreshTokenDto(loginResult!.RefreshToken);
            await client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

            // Try to use the same refresh token again
            var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_InvalidToken_Returns401()
        {
            var client = CreateClient();
            var dto = new RefreshTokenDto("this-is-not-a-valid-token");

            var response = await client.PostAsJsonAsync("/api/auth/refresh", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_TokenReuseRevokesFamily()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("familytest", "family@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("family@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            // Rotate once to get a new token
            var firstRefreshDto = new RefreshTokenDto(loginResult!.RefreshToken);
            var firstRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", firstRefreshDto);
            var firstRefreshResult = await firstRefreshResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            // Simulate theft — reuse the original token (which is now replaced)
            var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", firstRefreshDto);
            Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

            // The new token should also be revoked since family was nuked
            var newTokenDto = new RefreshTokenDto(firstRefreshResult!.RefreshToken);
            var newTokenResponse = await client.PostAsJsonAsync("/api/auth/refresh", newTokenDto);
            Assert.Equal(HttpStatusCode.Unauthorized, newTokenResponse.StatusCode);
        }

        [Fact]
        public async Task Logout_ValidToken_Returns200()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("logoutuser", "logout@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("logout@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            var revokeDto = new RevokeTokenDto(loginResult!.RefreshToken);
            var response = await client.PostAsJsonAsync("/api/auth/logout", revokeDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Logout_RevokedTokenCannotBeRefreshed()
        {
            var client = CreateClient();
            var registerDto = new RegisterDto("logoutuser2", "logout2@example.com", "Password123!");
            await client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto("logout2@example.com", "Password123!");
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

            // Logout
            var revokeDto = new RevokeTokenDto(loginResult!.RefreshToken);
            await client.PostAsJsonAsync("/api/auth/logout", revokeDto);

            // Try to refresh with the revoked token
            var refreshDto = new RefreshTokenDto(loginResult.RefreshToken);
            var response = await client.PostAsJsonAsync("/api/auth/refresh", refreshDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Logout_InvalidToken_Returns400()
        {
            var client = CreateClient();
            var dto = new RevokeTokenDto("not-a-real-token");

            var response = await client.PostAsJsonAsync("/api/auth/logout", dto);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
