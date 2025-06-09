using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.Seller.Profile
{
    public interface ISellerProfileService
    {
        Task<SellerResponseDto> UpsertSellerAsync(int userId, UpdateSellerDto sellerDto);
        Task<SellerResponseDto?> GetSellerByUserIdAsync(int userId);
        Task<SellerResponseDto?> GetSellerByIdAsync(int sellerId);
        Task<bool> IsUserSellerAsync(int userId);
    }
}
