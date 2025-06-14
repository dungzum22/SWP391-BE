

using PlatformFlower.Models.DTOs.Seller;

namespace PlatformFlower.Services.Seller.Profile
{
    public interface ISellerProfileService
    {
        Task<SellerProfileResponse> UpsertSellerAsync(int userId, UpdateSellerRequest sellerDto);
        Task<SellerProfileResponse?> GetSellerByUserIdAsync(int userId);
        Task<SellerProfileResponse?> GetSellerByIdAsync(int sellerId);
        Task<bool> IsUserSellerAsync(int userId);
    }
}
