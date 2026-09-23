using InventoryManagementApi.Dtos;
using InventoryManagementApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InventoryManagementApi.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Auth")
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/register", async (RegisterDto dto, IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RegisterAsync(dto);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                } 
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(ex.Message);
                }
            })
                .WithName("Regsiter")
                .WithSummary("Register a new user")
                .AllowAnonymous();

            group.MapPost("/login", async (LoginDto dto, IAuthService authService) =>
            {
                try
                {
                    var result = await authService.LoginAsync(dto);
                    return Results.Ok(result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            })
                .WithName("Login")
                .WithSummary("Login to an existing account and receive JWT token")
                .AllowAnonymous();

            // Password Recovery Below
            group.MapPost("/forgot-password", async (ForgotPasswordDto dto, IAuthService authService) =>
            {
                await authService.ForgotPasswordAsync(dto.Email);
                return Results.Ok("If this email is registered, you will receive a reset code.");
            })
                .WithName("ForgotPassword")
                .WithSummary("Request a password reset token")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/reset-password", async (ResetPasswordDto dto, IAuthService authService) =>
            {
                try
                {
                    await authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
                    return Results.Ok("Password reset successfully.");
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("ResetPassword")
                .WithSummary("Reset password using a valid token")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/refresh", async (RefreshTokenDto dto, IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RefreshTokenAsync(dto.RefreshToken);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("RefreshToken")
                .WithSummary("Refresh an access token using a valid refresh token")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/logout", async (RevokeTokenDto dto, IAuthService authService) =>
            {
                try
                {
                    await authService.RevokeTokenAsync(dto.RefreshToken);
                    return Results.Ok("Logged out successfully.");
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("Logout")
                .WithSummary("Logout by revoking a refresh token.")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");
        }
    }
}
