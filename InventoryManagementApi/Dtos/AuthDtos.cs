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
        string TokenType,       // will be set to Bearer
        string RefreshToken,    
        int ExpiresIn,          // seconds until the token will expire
        string Username,
        string Role
    );

    public record ForgotPasswordDto(
        string Email
    );
    
    public record ResetPasswordDto(
        string Token, 
        string NewPassword
    );

    public record RefreshTokenDto(string RefreshToken);
    public record RevokeTokenDto(string RefreshToken);
}