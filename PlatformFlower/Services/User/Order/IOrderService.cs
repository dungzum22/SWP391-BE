using PlatformFlower.Models.DTOs.Order;

namespace PlatformFlower.Services.User.Order
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponse>> GetUserOrdersAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
    }
}
