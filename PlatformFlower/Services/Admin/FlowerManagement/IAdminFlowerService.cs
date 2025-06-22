using PlatformFlower.Models.DTOs.Flower;

namespace PlatformFlower.Services.Admin.FlowerManagement
{
    public interface IAdminFlowerService
    {
        Task<FlowerResponse> ManageFlowerAsync(CreateFlowerRequest request);
        Task<List<FlowerResponse>> GetAllFlowersAsync();
        Task<FlowerResponse?> GetFlowerByIdAsync(int flowerId);
    }
}
