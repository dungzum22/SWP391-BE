using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Auth;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.Email;
using PlatformFlower.Services.Storage;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace PlatformFlower.Services.User
{
    public class UserService : IUserService
    {
        private readonly FlowershopContext _context;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;
        private readonly IJwtService _jwtService;
        private readonly IJwtConfiguration _jwtConfig;
        private readonly IEmailService _emailService;
        private readonly IStorageService _storageService;

        public UserService(
            FlowershopContext context,
            IValidationService validationService,
            IAppLogger logger,
            IJwtService jwtService,
            IJwtConfiguration jwtConfig,
            IEmailService emailService,
            IStorageService storageService)
        {
            _context = context;
            _validationService = validationService;
            _logger = logger;
            _jwtService = jwtService;
            _jwtConfig = jwtConfig;
            _emailService = emailService;
            _storageService = storageService;
        }

        public async Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto)
        {
            try
            {
                _logger.LogInformation($"Starting user registration for username: {registerDto.Username}");

                // Validate business rules
                await ValidateUserRegistrationAsync(registerDto);

                // Hash password
                var hashedPassword = HashPassword(registerDto.Password);

                // Create User entity with default type "user"
                var user = new Entities.User
                {
                    Username = registerDto.Username,
                    Password = hashedPassword,
                    Email = registerDto.Email,
                    Type = "user", // Always default to "user"
                    CreatedDate = DateTime.UtcNow,
                    Status = "active"
                };

                // Add user to context
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User created successfully with ID: {user.UserId}");

                // Always create UserInfo with default values
                var userInfo = new UserInfo
                {
                    UserId = user.UserId,
                    FullName = null, // Will be updated later by user
                    Address = null, // Will be updated later by user
                    BirthDate = null, // Will be updated later by user
                    Sex = null, // Will be updated later by user
                    Avatar = null, // Will be updated later by user
                    IsSeller = false, // Default to false, can be changed later
                    Points = 100, // Default starting points
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                };

                _context.UserInfos.Add(userInfo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"UserInfo created successfully for user ID: {user.UserId}");

                // Create user response DTO
                var userResponseDto = MapToUserResponseDto(user, userInfo);

                // Generate JWT token
                var token = _jwtService.GenerateToken(userResponseDto);
                var expirationMinutes = _jwtConfig.ExpirationMinutes;
                var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                // Send welcome email (don't wait for it to complete to avoid blocking registration)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                        _logger.LogInformation($"Welcome email sent successfully to {user.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError($"Failed to send welcome email to {user.Email}: {emailEx.Message}", emailEx);
                        // Don't throw - email failure shouldn't affect registration
                    }
                });

                // Return auth response with token
                return new AuthResponseDto
                {
                    User = userResponseDto,
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = expiresAt,
                    ExpiresInMinutes = expirationMinutes
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

                // Find user by username
                var user = await _context.Users
                    .Include(u => u.UserInfos)
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

                if (user == null)
                {
                    _logger.LogWarning($"Login failed - user not found: {loginDto.Username}");
                    throw new UnauthorizedAccessException("Invalid username or password");
                }

                // Check if user is active
                if (user.Status != "active")
                {
                    _logger.LogWarning($"Login failed - user account is not active: {loginDto.Username}");
                    throw new UnauthorizedAccessException("Account is not active");
                }

                // Verify password
                if (!VerifyPassword(loginDto.Password, user.Password))
                {
                    _logger.LogWarning($"Login failed - invalid password for user: {loginDto.Username}");
                    throw new UnauthorizedAccessException("Invalid username or password");
                }

                // Auto-upgrade legacy SHA256 passwords to bcrypt
                await UpgradePasswordIfNeeded(user, loginDto.Password);

                _logger.LogInformation($"User authenticated successfully: {loginDto.Username}");

                // Create user response DTO
                var userInfo = user.UserInfos.FirstOrDefault();
                var userResponseDto = MapToUserResponseDto(user, userInfo);

                // Generate JWT token
                var token = _jwtService.GenerateToken(userResponseDto);
                var expirationMinutes = _jwtConfig.ExpirationMinutes;
                var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

                // Return auth response with token
                var authResponse = new AuthResponseDto
                {
                    User = userResponseDto,
                    Token = token,
                    TokenType = "Bearer",
                    ExpiresAt = expiresAt,
                    ExpiresInMinutes = expirationMinutes
                };

                _logger.LogInformation($"User logged in successfully: {loginDto.Username}");
                return authResponse;
            }
            catch (UnauthorizedAccessException)
            {
                // Re-throw authorization exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during user login: {ex.Message}", ex);
                throw;
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

        public async Task<UserResponseDto?> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserInfos)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var userInfo = user.UserInfos.FirstOrDefault();
            return MapToUserResponseDto(user, userInfo);
        }

        public async Task<UserResponseDto?> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users
                .Include(u => u.UserInfos)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return null;

            var userInfo = user.UserInfos.FirstOrDefault();
            return MapToUserResponseDto(user, userInfo);
        }

        public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(string email)
        {
            try
            {
                _logger.LogInformation($"Password reset request for email: {email}");

                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning($"Password reset failed - user not found for email: {email}");
                    // Return success message even if user not found for security reasons
                    return new ForgotPasswordResponseDto
                    {
                        Success = true,
                        Message = "Nếu email này tồn tại trong hệ thống, bạn sẽ nhận được email hướng dẫn đặt lại mật khẩu."
                    };
                }

                // Check if user is active
                if (user.Status != "active")
                {
                    _logger.LogWarning($"Password reset failed - user account is not active: {email}");
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Tài khoản không hoạt động. Vui lòng liên hệ hỗ trợ."
                    };
                }

                // Generate reset token
                var resetToken = GenerateResetToken();
                var tokenExpiry = DateTime.UtcNow.AddMinutes(15); // Token expires in 15 minutes

                // Update user with reset token
                user.ResetPasswordToken = resetToken;
                user.ResetPasswordTokenExpiry = tokenExpiry;

                await _context.SaveChangesAsync();

                // Send password reset email (don't wait for it to complete)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken, user.Username);
                        _logger.LogInformation($"Password reset email sent successfully to {user.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError($"Failed to send password reset email to {user.Email}: {emailEx.Message}", emailEx);
                    }
                });

                _logger.LogInformation($"Password reset token generated successfully for user: {user.Username}");

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Email hướng dẫn đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư của bạn."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during password reset request: {ex.Message}", ex);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<ForgotPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto resetDto)
        {
            try
            {
                _logger.LogInformation($"Password reset attempt with token: {resetDto.Token}");

                // Find user by reset token
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == resetDto.Token);
                if (user == null)
                {
                    _logger.LogWarning($"Password reset failed - invalid token: {resetDto.Token}");
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Mã xác thực không hợp lệ."
                    };
                }

                // Check if token is expired
                if (user.ResetPasswordTokenExpiry == null || user.ResetPasswordTokenExpiry < DateTime.UtcNow)
                {
                    _logger.LogWarning($"Password reset failed - token expired for user: {user.Username}");
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Mã xác thực đã hết hạn. Vui lòng yêu cầu đặt lại mật khẩu mới."
                    };
                }

                // Check if user is active
                if (user.Status != "active")
                {
                    _logger.LogWarning($"Password reset failed - user account is not active: {user.Username}");
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "Tài khoản không hoạt động. Vui lòng liên hệ hỗ trợ."
                    };
                }

                // Hash new password
                var hashedPassword = HashPassword(resetDto.NewPassword);

                // Update user password and clear reset token
                user.Password = hashedPassword;
                user.ResetPasswordToken = null;
                user.ResetPasswordTokenExpiry = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successfully for user: {user.Username}");

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = "Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập với mật khẩu mới."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during password reset: {ex.Message}", ex);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == token);
                if (user == null) return false;

                return user.ResetPasswordTokenExpiry != null && user.ResetPasswordTokenExpiry > DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating reset token: {ex.Message}", ex);
                return false;
            }
        }

        #region Private Methods

        private static void ValidateProfileUpdateSecurity(UpdateUserInfoDto updateDto)
        {
            // SECURITY CHECK: Ensure no attempts to modify restricted fields
            // This method serves as a security checkpoint to prevent privilege escalation

            // Note: UpdateUserInfoDto should never contain User.Type, User.Status, or other sensitive fields
            // If someone tries to add these fields to the DTO, this validation will catch it

            // Additional security: Check if any reflection-based attacks are attempted
            var dtoType = updateDto.GetType();
            var properties = dtoType.GetProperties();

            var restrictedProperties = new[] { "Type", "Role", "Status", "UserId", "Password" };

            foreach (var prop in properties)
            {
                if (restrictedProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                {
                    throw new SecurityException($"Attempt to modify restricted property: {prop.Name}");
                }
            }
        }

        private async Task ValidateUserRegistrationAsync(RegisterUserDto registerDto)
        {
            // Check if username already exists
            if (await IsUsernameExistsAsync(registerDto.Username))
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Check if email already exists
            if (await IsEmailExistsAsync(registerDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            // Additional validation using validation service
            if (!_validationService.IsValidEmail(registerDto.Email))
            {
                throw new ArgumentException("Invalid email format");
            }

            if (!_validationService.IsValidPassword(registerDto.Password))
            {
                throw new ArgumentException("Password does not meet security requirements");
            }
        }

        private static string HashPassword(string password)
        {
            // Use bcrypt with work factor of 12 (recommended for 2024)
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // First try bcrypt verification
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception)
            {
                // If bcrypt fails, try legacy SHA256 verification for backward compatibility
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
                // Check if password is already bcrypt (bcrypt hashes start with $2a$, $2b$, $2x$, or $2y$)
                if (user.Password.StartsWith("$2"))
                {
                    return; // Already using bcrypt
                }

                // If we reach here, it's likely a legacy SHA256 password
                // Upgrade to bcrypt
                var newHashedPassword = HashPassword(plainPassword);
                user.Password = newHashedPassword;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Password upgraded to bcrypt for user: {user.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to upgrade password for user {user.Username}: {ex.Message}", ex);
                // Don't throw - password upgrade failure shouldn't prevent login
            }
        }

        private static string GenerateResetToken()
        {
            // Generate a secure random token
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

        public async Task<UserResponseDto> UpdateUserInfoAsync(int userId, UpdateUserInfoDto updateDto)
        {
            try
            {
                _logger.LogInformation($"Updating user info for user ID: {userId}");

                // Find user with UserInfo
                var user = await _context.Users
                    .Include(u => u.UserInfos)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    throw new ArgumentException("User not found");
                }

                // SECURITY: This method only updates UserInfo, NOT User.Type/Role
                // User role/type changes must be handled by admin-only endpoints
                _logger.LogInformation($"Updating profile for user: {user.Username}, Type: {user.Type} (role cannot be changed via this endpoint)");

                // SECURITY VALIDATION: Ensure no attempts to modify restricted fields
                ValidateProfileUpdateSecurity(updateDto);

                // Get or create UserInfo
                var userInfo = user.UserInfos.FirstOrDefault();
                if (userInfo == null)
                {
                    userInfo = new UserInfo
                    {
                        UserId = userId,
                        Points = 100,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.UserInfos.Add(userInfo);
                }

                // Handle avatar upload if provided
                string? newAvatarUrl = null;
                if (updateDto.Avatar != null)
                {
                    try
                    {
                        // Upload new avatar to S3
                        newAvatarUrl = await _storageService.UploadFileAsync(updateDto.Avatar, "avatars");

                        // Delete old avatar if exists
                        if (!string.IsNullOrEmpty(userInfo.Avatar))
                        {
                            await _storageService.DeleteFileAsync(userInfo.Avatar);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to upload avatar for user {userId}: {ex.Message}", ex);
                        throw new InvalidOperationException("Failed to upload avatar. Please try again.");
                    }
                }

                // Update UserInfo fields (only update non-null values)
                if (updateDto.FullName != null)
                    userInfo.FullName = updateDto.FullName;

                if (updateDto.Address != null)
                    userInfo.Address = updateDto.Address;

                if (updateDto.BirthDate.HasValue)
                    userInfo.BirthDate = updateDto.BirthDate.Value;

                if (updateDto.Sex != null)
                    userInfo.Sex = updateDto.Sex;

                if (updateDto.IsSeller.HasValue)
                    userInfo.IsSeller = updateDto.IsSeller.Value;

                if (newAvatarUrl != null)
                    userInfo.Avatar = newAvatarUrl;

                userInfo.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"User info updated successfully for user ID: {userId}");

                return MapToUserResponseDto(user, userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating user info for user {userId}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion
    }
}
