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
                .WithTags("Auth");

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
        }
    }
}
