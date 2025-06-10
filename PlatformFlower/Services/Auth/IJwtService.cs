using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.Auth
{
    public interface IJwtService
    {
        /// <summary>
        /// Generate JWT token for user
        /// </summary>
        /// <param name="user">User information</param>
        /// <returns>JWT token string</returns>
        string GenerateToken(UserResponseDto user);

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateToken(string token);

        /// <summary>
        /// Get user ID from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>User ID if valid, null otherwise</returns>
        int? GetUserIdFromToken(string token);

        /// <summary>
        /// Get username from JWT token
        /// </summary>
        /// <param name="token">JWT token</param>
        /// <returns>Username if valid, null otherwise</returns>
        string? GetUsernameFromToken(string token);
    }
}
