using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Storage;

namespace PlatformFlower.Services.Admin.FlowerManagement
{
    public class AdminFlowerService : IAdminFlowerService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;
        private readonly IStorageService _storageService;

        public AdminFlowerService(FlowershopContext context, IAppLogger logger, IStorageService storageService)
        {
            _context = context;
            _logger = logger;
            _storageService = storageService;
        }

        public async Task<FlowerResponse> ManageFlowerAsync(CreateFlowerRequest request)
        {
            if (request.FlowerId == null || request.FlowerId == 0)
            {
                await AdminFlowerValidation.ValidateCreateFlowerAsync(request, _context);
                return await CreateFlowerAsync(request);
            }
            else if (request.IsDeleted)
            {
                await AdminFlowerValidation.ValidateDeleteFlowerAsync(request.FlowerId.Value, _context);
                return await DeleteFlowerAsync(request.FlowerId.Value);
            }
            else
            {
                await AdminFlowerValidation.ValidateUpdateFlowerAsync(request, _context);
                return await UpdateFlowerAsync(request);
            }
        }

        public async Task<List<FlowerResponse>> GetAllFlowersAsync()
        {
            try
            {
                _logger.LogInformation("Admin getting all flowers");

                var flowers = await _context.FlowerInfos
                    .Include(f => f.Category)
                    .Where(f => !f.IsDeleted)
                    .OrderByDescending(f => f.CreatedAt)
                    .ToListAsync();

                var result = flowers.Select(MapToFlowerResponse).ToList();

                _logger.LogInformation($"Successfully retrieved {result.Count} flowers for admin");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all flowers for admin: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<FlowerResponse?> GetFlowerByIdAsync(int flowerId)
        {
            try
            {
                _logger.LogInformation($"Admin getting flower by ID: {flowerId}");

                var flower = await _context.FlowerInfos
                    .Include(f => f.Category)
                    .FirstOrDefaultAsync(f => f.FlowerId == flowerId && !f.IsDeleted);

                if (flower != null)
                {
                    _logger.LogInformation($"Successfully retrieved flower - ID: {flower.FlowerId}, Name: {flower.FlowerName}");
                    return MapToFlowerResponse(flower);
                }

                _logger.LogWarning($"Flower with ID {flowerId} not found");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flower {flowerId}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<FlowerResponse> CreateFlowerAsync(CreateFlowerRequest request)
        {
            _logger.LogInformation($"Creating flower: {request.FlowerName}");

            // Check if there's an existing inactive flower with the same name
            var existingInactiveFlower = await _context.FlowerInfos
                .FirstOrDefaultAsync(f => f.FlowerName == request.FlowerName && f.IsDeleted);

            if (existingInactiveFlower != null)
            {
                _logger.LogInformation($"Reactivating existing flower: {request.FlowerName}");

                // Reactivate the existing flower
                existingInactiveFlower.FlowerDescription = request.FlowerDescription;
                existingInactiveFlower.Price = request.Price!.Value;
                existingInactiveFlower.AvailableQuantity = request.AvailableQuantity!.Value;
                existingInactiveFlower.Status = request.Status ?? "active";
                existingInactiveFlower.CategoryId = request.CategoryId!.Value;
                existingInactiveFlower.UpdatedAt = DateTime.Now;
                existingInactiveFlower.IsDeleted = false;

                // Handle image upload if provided
                if (request.ImageFile != null)
                {
                    var uploadedImageUrl = await _storageService.UploadFileAsync(request.ImageFile, "flowers");
                    existingInactiveFlower.ImageUrl = uploadedImageUrl;
                }
                else if (!string.IsNullOrEmpty(request.ImageUrl))
                {
                    existingInactiveFlower.ImageUrl = request.ImageUrl;
                }

                await _context.SaveChangesAsync();
                return MapToFlowerResponse(existingInactiveFlower);
            }

            // Handle image upload
            string? imageUrl = null;
            if (request.ImageFile != null)
            {
                imageUrl = await _storageService.UploadFileAsync(request.ImageFile, "flowers");
            }
            else if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                imageUrl = request.ImageUrl;
            }

            var flower = new Entities.FlowerInfo
            {
                FlowerName = request.FlowerName!,
                FlowerDescription = request.FlowerDescription,
                Price = request.Price!.Value,
                ImageUrl = imageUrl,
                AvailableQuantity = request.AvailableQuantity!.Value,
                Status = request.Status ?? "active",
                CategoryId = request.CategoryId!.Value,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.FlowerInfos.Add(flower);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully created flower: {flower.FlowerName} with ID: {flower.FlowerId}");
            return MapToFlowerResponse(flower);
        }

        private async Task<FlowerResponse> UpdateFlowerAsync(CreateFlowerRequest request)
        {
            var flower = await _context.FlowerInfos
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f => f.FlowerId == request.FlowerId);

            if (flower == null)
            {
                throw new InvalidOperationException($"Flower with ID {request.FlowerId} not found");
            }

            _logger.LogInformation($"Updating flower: {flower.FlowerName} (ID: {flower.FlowerId})");

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.FlowerName))
                flower.FlowerName = request.FlowerName;
            if (!string.IsNullOrEmpty(request.FlowerDescription))
                flower.FlowerDescription = request.FlowerDescription;
            if (request.Price.HasValue)
                flower.Price = request.Price.Value;
            if (request.AvailableQuantity.HasValue)
                flower.AvailableQuantity = request.AvailableQuantity.Value;
            if (!string.IsNullOrEmpty(request.Status))
                flower.Status = request.Status;
            if (request.CategoryId.HasValue)
                flower.CategoryId = request.CategoryId.Value;

            // Handle image upload if provided
            if (request.ImageFile != null)
            {
                var uploadedImageUrl = await _storageService.UploadFileAsync(request.ImageFile, "flowers");
                flower.ImageUrl = uploadedImageUrl;
            }
            else if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                flower.ImageUrl = request.ImageUrl;
            }

            flower.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully updated flower: {flower.FlowerName} (ID: {flower.FlowerId})");
            return MapToFlowerResponse(flower);
        }

        private async Task<FlowerResponse> DeleteFlowerAsync(int flowerId)
        {
            var flower = await _context.FlowerInfos
                .Include(f => f.Category)
                .FirstOrDefaultAsync(f => f.FlowerId == flowerId);

            if (flower == null)
            {
                throw new InvalidOperationException($"Flower with ID {flowerId} not found");
            }

            _logger.LogInformation($"Deleting flower: {flower.FlowerName} (ID: {flower.FlowerId})");

            // Soft delete: set status to inactive and mark as deleted
            flower.Status = "inactive";
            flower.IsDeleted = true;
            flower.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Successfully deleted flower: {flower.FlowerName} (ID: {flower.FlowerId})");
            return MapToFlowerResponse(flower);
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
