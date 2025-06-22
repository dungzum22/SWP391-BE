using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Common.Flower
{
    public class FlowerService : IFlowerService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public FlowerService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<FlowerResponse>> GetActiveFlowersAsync()
        {
            try
            {
                _logger.LogInformation("Getting all active flowers for public display");

                var flowers = await _context.FlowerInfos
                    .Include(f => f.Category)
                    .Where(f => f.Status == "active" && !f.IsDeleted && f.AvailableQuantity > 0)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                var result = flowers.Select(MapToFlowerResponse).ToList();

                _logger.LogInformation($"Successfully retrieved {result.Count} active flowers for public display");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active flowers: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<FlowerResponse?> GetActiveFlowerByIdAsync(int flowerId)
        {
            try
            {
                _logger.LogInformation($"Getting active flower by ID: {flowerId}");

                var flower = await _context.FlowerInfos
                    .Include(f => f.Category)
                    .FirstOrDefaultAsync(f => f.FlowerId == flowerId
                                            && f.Status == "active"
                                            && !f.IsDeleted
                                            && f.AvailableQuantity > 0);

                if (flower != null)
                {
                    _logger.LogInformation($"Successfully retrieved active flower - ID: {flower.FlowerId}, Name: {flower.FlowerName}");
                    return MapToFlowerResponse(flower);
                }

                _logger.LogWarning($"Active flower with ID {flowerId} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active flower by ID {flowerId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<FlowerResponse>> GetFlowersByCategoryAsync(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Getting active flowers by category ID: {categoryId}");

                var flowers = await _context.FlowerInfos
                    .Include(f => f.Category)
                    .Where(f => f.CategoryId == categoryId
                              && f.Status == "active"
                              && !f.IsDeleted
                              && f.AvailableQuantity > 0)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                var result = flowers.Select(MapToFlowerResponse).ToList();

                _logger.LogInformation($"Successfully retrieved {result.Count} active flowers for category {categoryId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flowers by category {categoryId}: {ex.Message}", ex);
                throw;
            }
        }





        private FlowerResponse MapToFlowerResponse(Entities.FlowerInfo flower)
        {
            return new FlowerResponse
            {
                FlowerId = flower.FlowerId,
                FlowerName = flower.FlowerName,
                FlowerDescription = flower.FlowerDescription,
                Price = flower.Price,
                ImageUrl = flower.ImageUrl,
                AvailableQuantity = flower.AvailableQuantity,
                Status = flower.Status,
                CreatedAt = flower.CreatedAt,
                UpdatedAt = flower.UpdatedAt,
                CategoryId = flower.CategoryId,
                CategoryName = flower.Category?.CategoryName,
                IsDeleted = flower.IsDeleted
            };
        }
    }
}
