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

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email.ToLower().Trim();
            }
            catch
            {
                return false;
            }
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be empty.", nameof(password));
            }
            if (password.Length < 8)
            {
                throw new ArgumentException("Password must be at least 8 characters.", nameof(password));
            }
            if (!password.Any(char.IsUpper))
            {
                throw new ArgumentException("Password must contain at least one uppercase character.", nameof(password));
            }
            if(!password.Any(char.IsDigit))
            {
                throw new ArgumentException("Password must contain at least one number.", nameof(password));
            }
            if(!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                throw new ArgumentException("Password must contain at least one special character.", nameof(password));
            }
        }
        
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
            if (string.IsNullOrWhiteSpace(dto.Email) || !IsValidEmail(dto.Email))
            {
                throw new ArgumentException("Email cannot be empty.", nameof(dto));
            }
            ValidatePassword(dto.Password);

            // user validation
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (existingUser is not null)
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }
            var existingUsername = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username.ToLower());
            if (existingUsername is not null)
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

            // lockout check before anything else
            if (user is not null && user.LockoutUntil.HasValue && user.LockoutUntil > DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");    // don't mention lockout duration
            }

            // check to see if user exists and they used the correct password 
            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                // Incrementing failed attempts for valid user
                if(user is not null)
                {
                    user.FailedLoginAttempts++;

                    if(user.FailedLoginAttempts >= 5)
                    {
                        user.LockoutUntil = DateTime.UtcNow.AddMinutes(5);
                        user.FailedLoginAttempts = 0;
                    }

                    await _context.SaveChangesAsync();
                }

                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            // Successful login - reset failed login attempts
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _context.SaveChangesAsync();

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