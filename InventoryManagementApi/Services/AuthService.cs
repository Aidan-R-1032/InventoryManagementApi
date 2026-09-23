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
        private readonly IEmailService _emailService;
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
        
        private string GenerateRefreshToken()
        {
            var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
        
        private async Task<RefreshToken> CreateRefreshTokenAsync(int userId)
        {
            var token = new RefreshToken
            {
                Token = GenerateRefreshToken(),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                isRevoked = false,
                CreatedAt = DateTime.UtcNow
            };


            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public AuthService(InventoryDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);
            return new TokenResponseDto(
                    AccessToken: token, 
                    RefreshToken: refreshToken.Token,
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
            var refreshToken = await CreateRefreshTokenAsync(user.Id);
            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);

            return new TokenResponseDto(
                    AccessToken: token,
                    RefreshToken: refreshToken.Token,
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

        public async Task ForgotPasswordAsync(string email)
        {
            if(string.IsNullOrEmpty(email))
            {
                return;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());

            if(user is null)
            {
                return;
            }

            // Invalidate any existing unused tokens for this user
            var existingTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
            foreach(var existingToken in existingTokens)
            {
                existingToken.IsUsed = true;
            }

            // generates a cryptographically secure random token
            var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            var resetToken = new PasswordResetToken
            {
                Token = token,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, token);
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            if(string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Reset token is required.");
            }

            ValidatePassword(newPassword);

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token);
            
            if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt <= DateTime.UtcNow)
            {
                throw new ArgumentException("This reset token is invalid or has expired.");
            }

            // update the password
            resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            resetToken.User.FailedLoginAttempts = 0;
            resetToken.User.LockoutUntil = null;

            // Mark token as used
            resetToken.IsUsed = true;

            await _context.SaveChangesAsync();
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if(string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("Refresh token is required.");
            }

            var existingToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            // Detect reuse FIRST
            if (existingToken is not null && existingToken.ReplacedByToken is not null)
            {
                await RevokeTokenFamilyAsync(existingToken.UserId);
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // THEN check if revoked
            if (existingToken is null || existingToken.isRevoked)
            {
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }


            // Detect token reuse attacks - token already replaced means potential misuse
            if (existingToken.ReplacedByToken is not null)
            {
                // Revoke the entire token family
                await RevokeTokenFamilyAsync(existingToken.UserId);
                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // Rotate - create new refresh token and invalidate old ones
            var newRefreshToken = await CreateRefreshTokenAsync(existingToken.UserId);
            existingToken.isRevoked = true;
            existingToken.ReplacedByToken = newRefreshToken.Token;
            await _context.SaveChangesAsync();

            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"]!);
            return new TokenResponseDto(
                AccessToken: GenerateJwtToken(existingToken.User),
                RefreshToken: newRefreshToken.Token,
                TokenType: "Bearer",
                ExpiresIn: expiryMinutes * 60,
                Username: existingToken.User.Username,
                Role: existingToken.User.Role.ToString()
                );
        }

        public async Task RevokeTokenAsync(string refreshToken)
        {
            if(string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("Refresh token is required.");
            }
            var existingToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);
            if(existingToken is null || existingToken.isRevoked)
            {
                throw new ArgumentException("Invalid refresh token.");
            }
            existingToken.isRevoked = true;
            await _context.SaveChangesAsync();
        }

        private async Task RevokeTokenFamilyAsync(int userId)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.isRevoked)
                .ToListAsync();
            foreach (var token in activeTokens)
            {
                token.isRevoked = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}