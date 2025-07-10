using PlatformFlower.Models.DTOs.Order;

namespace PlatformFlower.Services.User.Order
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateOrderAsync(int userId, CreateOrderRequest request);
        Task<OrderResponse?> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponse>> GetUserOrdersAsync(int userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> RestoreVoucherAsync(int orderId);

        // Admin methods
        Task<List<AdminOrderResponse>> GetAllOrdersAsync(AdminOrderListRequest request);
        Task<List<AdminOrderDetailResponse>> GetAllOrdersWithDetailsAsync();
        Task<AdminOrderDetailResponse?> GetOrderDetailsByIdAsync(int orderId);
        Task<AdminOrderDetailResponse?> UpdateOrderStatusAdminAsync(int orderId, UpdateOrderStatusRequest request);
        Task<OrderStatisticsResponse> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetOrderCountAsync(AdminOrderListRequest request);
    }
}
