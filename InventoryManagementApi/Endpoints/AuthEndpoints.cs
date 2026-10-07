using InventoryManagementApi.Dtos;
using InventoryManagementApi.Models;
using InventoryManagementApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using InventoryManagementApi.Extensions;

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

            group.MapPost("/delete-account", async (DeleteAccountDto dto, IAuthService authService) =>
            {
                await authService.StartDeleteProcessAsync(dto.Email);
                return Results.Ok("If this email is registered, you will receive a deletion token");
            })
                .WithName("DeleteAccount")
                .WithSummary("Request an account deletion token.")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/confirm-delete", async (ConfirmDeleteDto dto, IAuthService authService) =>
            {
                try
                {
                    await authService.ConfirmDeleteAccountAsync(dto.DeleteToken);
                    return Results.Ok("Your account has been successfully deleted.");
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("ConfirmDeleteAccount")
                .WithSummary("Delete an account using a valid token")
                .AllowAnonymous()
                .RequireRateLimiting("AuthRateLimit");

            group.MapPost("/set-as-admin", async (SetRoleDto dto, IAuthService authService, HttpContext ctx) =>
            {
                // Dont trust the client to say who the admin is - look at their JWT instead
                var promoterId = ctx.User.GetUserId(); 

                try
                {
                    await authService.AlterPermissionsAsync(promoterId, dto.UserId, UserRole.Admin);
                    return Results.Ok("User was given Admin privileges");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("SetAsAdmin")
                .WithSummary("Admins can users to 'Admin' role")
                .RequireAuthorization("AdminOnly")
                .RequireRateLimiting("AuthRateLimit");


            group.MapPost("/set-as-staff", async (SetRoleDto dto, IAuthService authService, HttpContext ctx) =>
            {
                var promoterId = ctx.User.GetUserId();

                try
                {
                    await authService.AlterPermissionsAsync(promoterId, dto.UserId, UserRole.Staff);
                    return Results.Ok("User was given staff privileges");
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            })
                .WithName("SetStaff")
                .WithSummary("Admins can set users to the 'Staff' role")
                .RequireAuthorization("AdminOnly")
                .RequireRateLimiting("AuthRateLimit");

        }
    }
}
