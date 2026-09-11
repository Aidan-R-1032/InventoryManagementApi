using InventoryManagementApi.Dtos;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public interface IAuthService
    {
        Task<TokenResponseDto> RegisterAsync(RegisterDto dto);
        Task<TokenResponseDto> LoginAsync(LoginDto dto);
        string GenerateJwtToken(User user);
    }
}