using InventoryManagementApi.Dtos;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto> RegisterAsync(RegisterDto dto);
        Task<TokenResponseDto> LoginAsync(LoginDto dto);
        string GenerateJwtToken(User user);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(string token, string newPassword);
        Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken);

        Task StartDeleteProcessAsync(string email);
        Task ConfirmDeleteAccountAsync(string token);

        Task AlterPermissionsAsync(
            int adminId, 
            int userId, 
            UserRole newRole, 
            string reason, 
            string ipAddress
        );
        Task<IEnumerable<RoleChangeLogViewerDto>> GetRoleChangeLogsAsync(
            int amount = 20,
            int page = 1,
            UserRole? newRole = null,
            UserRole? oldRole = null,
            DateTime? begin = null,
            DateTime? end = null,
            int? adminId = null,
            int? targetId = null);
    }
}