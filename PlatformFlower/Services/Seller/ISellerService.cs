using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.Seller
{
    /// <summary>
    /// Service for handling seller operations
    /// Follows Single Responsibility Principle - only handles seller logic
    /// </summary>
    public interface ISellerService
    {
        /// <summary>
        /// Register a user as a seller
        /// </summary>
        /// <param name="userId">User ID from JWT token</param>
        /// <param name="registerSellerDto">Seller registration data</param>
        /// <returns>Seller information</returns>
        Task<SellerResponseDto> RegisterSellerAsync(int userId, RegisterSellerDto registerSellerDto);

        /// <summary>
        /// Get seller information by user ID
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Seller information or null if not found</returns>
        Task<SellerResponseDto?> GetSellerByUserIdAsync(int userId);

        /// <summary>
        /// Get seller information by seller ID
        /// </summary>
        /// <param name="sellerId">Seller ID</param>
        /// <returns>Seller information or null if not found</returns>
        Task<SellerResponseDto?> GetSellerByIdAsync(int sellerId);

        /// <summary>
        /// Check if user is already a seller
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if user is already a seller, false otherwise</returns>
        Task<bool> IsUserSellerAsync(int userId);
    }
}
