using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Auth;
using PlatformFlower.Services.Common.Configuration;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Validation;
using System.Security.Cryptography;
using System.Text;

namespace PlatformFlower.Services.User
{
    public class UserService : IUserService
    {
        private readonly FlowershopContext _context;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;
        private readonly IJwtService _jwtService;
        private readonly IJwtConfiguration _jwtConfig;

        public UserService(
            FlowershopContext context,
            IValidationService validationService,
            IAppLogger logger,
            IJwtService jwtService,
            IJwtConfiguration jwtConfig)
        {
            _context = context;
            _validationService = validationService;
            _logger = logger;
            _jwtService = jwtService;
            _jwtConfig = jwtConfig;
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
                var hashedPassword = HashPassword(loginDto.Password);
                if (user.Password != hashedPassword)
                {
                    _logger.LogWarning($"Login failed - invalid password for user: {loginDto.Username}");
                    throw new UnauthorizedAccessException("Invalid username or password");
                }

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

        #region Private Methods

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
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
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

        #endregion
    }
}
