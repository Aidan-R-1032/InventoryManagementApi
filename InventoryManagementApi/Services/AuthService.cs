using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using InventoryManagementApi.Data;
using InventoryManagementApi.Dtos;
using InventoryManagementApi.Models;

namespace InventoryManagementApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly InventoryDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(InventoryDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
        {
            // Input validation:
            if (string.IsNullOrWhiteSpace(dto.Username))
            {
                throw new ArgumentException("Username cannot be empty.", nameof(dto));
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(dto));
            }
            if (string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(dto));
            }
            if (dto.Password.Length < 8)
            {
                throw new ArgumentException("Password must be at least 8 characters.", nameof(dto));
            }

            // user validation
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (existingUser != null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }
            var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.ToLower());
            if (existingUsername != null)
            {
                throw new InvalidOperationException("This username is already in use.");
            }

            // new user creation
            var user = new User
            {
                Username = dto.Username.Trim().ToLower(),
                Email = dto.Email.Trim().ToLower(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Staff,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // token creation
            var token = GenerateJwtToken(user);
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);
            return new TokenResponseDto(
                    AccessToken: token, 
                    TokenType: "Bearer",
                    ExpiresIn: expiryMinutes * 60,
                    Username: user.Username,
                    Role: user.Role.ToString()
                );
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            // input validation
            if(string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            {
                throw new ArgumentException("Email and password are required.");
            }

            // user validation
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if(user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // token generation
            var token = GenerateJwtToken(user);
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);

            return new TokenResponseDto(
                    AccessToken: token,
                    TokenType: "Bearer",
                    ExpiresIn: expiryMinutes * 60,
                    Username: user.Username,
                    Role: user.Role.ToString()
                );
        }

        public string GenerateJwtToken(User user)
        {
            // token data creation
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            // tokens receive identifiers - useful for (later) token revocation
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Jwt Token Generation
            var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(
                            int.Parse(_configuration["Jwt:ExpiryMinutes"]!)
                        ),
                    signingCredentials: credentials
                );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}