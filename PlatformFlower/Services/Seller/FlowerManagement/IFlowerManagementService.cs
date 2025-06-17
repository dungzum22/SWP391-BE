using PlatformFlower.Models.DTOs.Flower;

namespace PlatformFlower.Services.Seller.FlowerManagement
{
    public interface IFlowerManagementService
    {
        Task<FlowerResponse> ManageFlowerAsync(CreateFlowerRequest request, int? currentSellerId = null);
        Task<List<FlowerResponse>> GetAllFlowersAsync();
        Task<FlowerResponse?> GetFlowerByIdAsync(int flowerId);
    }
}
