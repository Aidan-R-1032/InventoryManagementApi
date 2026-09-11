namespace InventoryManagementApi.Dtos
{
    public record RegisterDto(
        string Username,
        string Email,
        string Password // plaintext will go straight into bycrypt
    );

    public record LoginDto(
        string Email,
        string Password
    );

    public record TokenResponseDto(
        string AccessToken,
        string TokenType,   // will be set to Bearer
        int ExpiresIn,      // seconds until the token will expire
        string Username,
        string Role
    );
}