using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.Email;
using PlatformFlower.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace PlatformFlower.Services.User.Auth
{
    public class AuthServiceSimple : IAuthService
    {
        private readonly FlowershopContext _context;
        private readonly IJwtConfiguration _jwtConfig;
        private readonly IValidationService _validationService;
        private readonly IEmailService _emailService;
        private readonly IAppLogger _logger;

        public AuthServiceSimple(
            FlowershopContext context,
            IJwtConfiguration jwtConfig,
            IValidationService validationService,
            IEmailService emailService,
            IAppLogger logger)
        {
            _context = context;
            _jwtConfig = jwtConfig;
            _validationService = validationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto)
        {
            try
            {
                _logger.LogInformation($"Starting user registration for username: {registerDto.Username}");

                await AuthValidation.ValidateRegistrationAsync(registerDto, _context, _validationService);
                var user = new Entities.User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    Password = HashPassword(registerDto.Password),
                    Type = "user",
                    Status = "active",
                    CreatedDate = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User registered successfully: {user.Username}");

                var token = GenerateJwtToken(user);

                return new AuthResponseDto
                {
                    User = MapToUserResponseDto(user, null),
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                    ExpiresInMinutes = _jwtConfig.ExpirationMinutes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during user registration: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<AuthResponseDto> LoginUserAsync(LoginUserDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for username: {loginDto.Username}");

                AuthValidation.ValidateLogin(loginDto);
                var user = await _context.Users
                    .Include(u => u.UserInfos)
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null || !VerifyPassword(loginDto.Password, user.Password))
                {
                    throw new UnauthorizedAccessException("Invalid username or password");
                }

                if (user.Status != "active")
                {
                    throw new UnauthorizedAccessException("Account is not active");
                }

                await UpgradePasswordIfNeeded(user, loginDto.Password);

                _logger.LogInformation($"User logged in successfully: {user.Username}");

                var token = GenerateJwtToken(user);
                var userInfo = user.UserInfos.FirstOrDefault();

                return new AuthResponseDto
                {
                    User = MapToUserResponseDto(user, userInfo),
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                    ExpiresInMinutes = _jwtConfig.ExpirationMinutes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during user login: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Password reset request for email: {email}");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return new ForgotPasswordResponseDto
                    {
                        Success = true,
                        Message = "If email exists, you will receive reset instructions."
                    };
                }

                var resetToken = GenerateResetToken();
                user.ResetPasswordToken = resetToken;
                user.ResetPasswordTokenExpiry = DateTime.UtcNow.AddHours(1);

                await _context.SaveChangesAsync();

                var emailSubject = "Password Reset - Flower Shop";
                var emailBody = $@"
                    <h2>Password Reset Request</h2>
                    <p>Your reset token is: <strong>{resetToken}</strong></p>
                    <p>This token expires in 1 hour.</p>
                ";

                await _emailService.SendEmailAsync(email, emailSubject, emailBody);

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Reset instructions sent to your email."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in forgot password: {ex.Message}", ex);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                };
            }
        }

        public async Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            try
            {
                AuthValidation.ValidateResetPassword(resetDto);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == resetDto.Token);
                if (user == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                {
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Invalid or expired reset token."
                    };
                }

                user.Password = HashPassword(resetDto.NewPassword);
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Password reset successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in reset password: {ex.Message}", ex);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                };
            }
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == token);
                return user != null && user.ResetPasswordTokenExpiry > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }



        private string GenerateJwtToken(Entities.User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("user_id", user.UserId.ToString()),
                    new Claim("username", user.Username),
                    new Claim("email", user.Email),
                    new Claim("type", user.Type)
                }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtConfig.ExpirationMinutes),
                Issuer = _jwtConfig.Issuer,
                Audience = _jwtConfig.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return VerifyLegacyPassword(password, hashedPassword);
            }
        }

        private static bool VerifyLegacyPassword(string password, string hashedPassword)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var legacyHash = Convert.ToBase64String(hashedBytes);
                return legacyHash == hashedPassword;
            }
            catch
            {
                return false;
            }
        }

        private async Task UpgradePasswordIfNeeded(Entities.User user, string plainPassword)
        {
            try
            {
                if (user.Password.StartsWith("$2")) return;

                user.Password = HashPassword(plainPassword);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Password upgraded for user: {user.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to upgrade password: {ex.Message}", ex);
            }
        }

        private static string GenerateResetToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var tokenBytes = new byte[32];
            rng.GetBytes(tokenBytes);
            return Convert.ToBase64String(tokenBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..8].ToUpper();
        }

        private static UserResponseDto MapToUserResponseDto(Entities.User user, UserInfo? userInfo)
        {
            return new UserResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Type = user.Type,
                CreatedDate = user.CreatedDate,
                Status = user.Status,
                UserInfo = userInfo != null ? new UserInfoDto
                {
                    UserInfoId = userInfo.UserInfoId,
                    FullName = userInfo.FullName,
                    Address = userInfo.Address,
                    BirthDate = userInfo.BirthDate,
                    Sex = userInfo.Sex,
                    IsSeller = userInfo.IsSeller,
                    Avatar = userInfo.Avatar,
                    Points = userInfo.Points,
                    CreatedDate = userInfo.CreatedDate,
                    UpdatedDate = userInfo.UpdatedDate
                } : null
            };
        }
    }
}
