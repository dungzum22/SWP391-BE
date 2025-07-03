using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
using PlatformFlower.Models.DTOs.Auth;
using PlatformFlower.Models.DTOs.User;

namespace PlatformFlower.Services.User.Auth
{
    public class AuthServiceSimple : IAuthService
    {
        private readonly FlowershopContext _context;
        private readonly IJwtConfiguration _jwtConfig;
        private readonly IEmailService _emailService;
        private readonly IAppLogger _logger;

        public AuthServiceSimple(
            FlowershopContext context,
            IJwtConfiguration jwtConfig,
            IEmailService emailService,
            IAppLogger logger)
        {
            _context = context;
            _jwtConfig = jwtConfig;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<LoginResponse> RegisterUserAsync(RegisterRequest registerDto)
        {
            try
            {
                _logger.LogInformation($"Starting user registration for username: {registerDto.Username}");

                await AuthValidation.ValidateRegistrationAsync(registerDto, _context);
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

                // Send welcome email to new user
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                    _logger.LogInformation($"Welcome email sent successfully to: {user.Email}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError($"Failed to send welcome email to {user.Email}: {emailEx.Message}", emailEx);
                    // Don't throw here - registration should still succeed even if email fails
                }

                var token = GenerateJwtToken(user);

                return new LoginResponse
                {
                    User = MapToUserResponse(user, null),
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

        public async Task<LoginResponse> LoginUserAsync(LoginRequest loginDto)
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

                return new LoginResponse
                {
                    User = MapToUserResponse(user, userInfo),
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

        public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Password reset request for email: {email}");

                // Validate email format
                var request = new ForgotPasswordRequest { Email = email };
                AuthValidation.ValidateForgotPassword(request);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "If email exists, you will receive reset instructions."
                    };
                }

                // Check if user account is active - SECURITY: Don't allow password reset for inactive users
                if (user.Status != "active")
                {
                    _logger.LogWarning($"Password reset denied - account is inactive: {user.Username}");
                    // Return same message for security - don't reveal account status
                    return new ForgotPasswordResponse
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

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Reset instructions sent to your email."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in forgot password: {ex.Message}", ex);
                return new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "An error occurred. Please try again."
                };
            }
        }

        public async Task<ForgotPasswordResponse> ResetPasswordAsync(ResetPasswordRequest resetDto)
        {
            try
            {
                AuthValidation.ValidateResetPassword(resetDto);

                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == resetDto.Token);
                if (user == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                {
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token."
                    };
                }

                // SECURITY: Check if user account is active before allowing password reset
                if (user.Status != "active")
                {
                    _logger.LogWarning($"Password reset denied - account is inactive: {user.Username}");
                    return new ForgotPasswordResponse
                    {
                        Success = false,
                        Message = "Account is not active. Please contact support."
                    };
                }

                user.Password = HashPassword(resetDto.NewPassword);
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                return new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Password reset successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in reset password: {ex.Message}", ex);
                return new ForgotPasswordResponse
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
                // SECURITY: Token is only valid if user exists, token not expired, AND account is active
                return user != null &&
                       user.ResetPasswordTokenExpiry > DateTime.UtcNow &&
                       user.Status == "active";
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
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Type), // Sử dụng ClaimTypes.Role thay vì "type"
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

        private static UserResponse MapToUserResponse(Entities.User user, Entities.UserInfo? userInfo)
        {
            return new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Type = user.Type,
                CreatedDate = user.CreatedDate,
                Status = user.Status,
                UserInfo = userInfo != null ? new Models.DTOs.User.UserInfo
                {
                    UserInfoId = userInfo.UserInfoId,
                    FullName = userInfo.FullName,
                    Address = userInfo.Address,
                    BirthDate = userInfo.BirthDate,
                    Sex = userInfo.Sex,
                    Avatar = userInfo.Avatar,
                    CreatedDate = userInfo.CreatedDate,
                    UpdatedDate = userInfo.UpdatedDate
                } : null
            };
        }
    }
}
