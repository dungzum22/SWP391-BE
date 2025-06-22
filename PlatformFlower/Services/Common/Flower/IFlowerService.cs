using PlatformFlower.Models.DTOs.Flower;

namespace PlatformFlower.Services.Common.Flower
{
    public interface IFlowerService
    {
        Task<List<FlowerResponse>> GetActiveFlowersAsync();
        Task<FlowerResponse?> GetActiveFlowerByIdAsync(int flowerId);
        Task<List<FlowerResponse>> GetFlowersByCategoryAsync(int categoryId);
    }
}
