using PlatformFlower.Models.DTOs.Cart;

namespace PlatformFlower.Services.User.Cart
{
    public interface ICartService
    {
        Task<CartItemResponse> AddToCartAsync(int userId, AddToCartRequest request);
        Task<CartResponse> GetCartAsync(int userId);
        Task<CartItemResponse> UpdateCartItemAsync(int userId, int cartId, UpdateCartRequest request);
        Task RemoveCartItemAsync(int userId, int cartId);
        Task ClearCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
    }
}
