using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.User
{
    public interface IUserService
    {
        /// <summary>
        /// Register a new user and return auth response with JWT token
        /// </summary>
        /// <param name="registerDto">User registration data</param>
        /// <returns>Auth response with user info and JWT token</returns>
        Task<AuthResponseDto> RegisterUserAsync(RegisterUserDto registerDto);

        /// <summary>
        /// Check if username already exists
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>True if username exists, false otherwise</returns>
        Task<bool> IsUsernameExistsAsync(string username);

        /// <summary>
        /// Check if email already exists
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <returns>True if email exists, false otherwise</returns>
        Task<bool> IsEmailExistsAsync(string email);

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>User information or null if not found</returns>
        Task<UserResponseDto?> GetUserByIdAsync(int userId);

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>User information or null if not found</returns>
        Task<UserResponseDto?> GetUserByUsernameAsync(string username);
    }
}
