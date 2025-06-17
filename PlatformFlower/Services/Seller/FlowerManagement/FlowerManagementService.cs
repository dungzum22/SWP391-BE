using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Storage;

namespace PlatformFlower.Services.Seller.FlowerManagement
{
    public class FlowerManagementService : IFlowerManagementService
    {
        private readonly FlowershopContext _context;
        private readonly IStorageService _storageService;

        public FlowerManagementService(FlowershopContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        public async Task<FlowerResponse> ManageFlowerAsync(CreateFlowerRequest request, int? currentSellerId = null)
        {
            if (request.FlowerId == null || request.FlowerId == 0)
            {
                if (currentSellerId.HasValue && request.SellerId != currentSellerId.Value)
                {
                    request.SellerId = currentSellerId.Value;
                }
                await FlowerValidation.ValidateCreateFlowerAsync(request, _context);
                return await CreateFlowerAsync(request);
            }
            else if (request.IsDeleted)
            {
                await FlowerValidation.ValidateDeleteFlowerAsync(request.FlowerId.Value, _context, currentSellerId);
                return await DeleteFlowerAsync(request.FlowerId.Value);
            }
            else
            {
                await FlowerValidation.ValidateUpdateFlowerAsync(request, _context, currentSellerId);
                return await UpdateFlowerAsync(request);
            }
        }

        public async Task<List<FlowerResponse>> GetAllFlowersAsync()
        {
            var flowers = await _context.FlowerInfos
                .Include(f => f.Category)
                .Include(f => f.Seller)
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var result = new List<FlowerResponse>();
            foreach (var flower in flowers)
            {
                result.Add(await MapToFlowerResponse(flower));
            }
            return result;
        }

        public async Task<FlowerResponse?> GetFlowerByIdAsync(int flowerId)
        {
            var flower = await _context.FlowerInfos
                .Include(f => f.Category)
                .Include(f => f.Seller)
                .FirstOrDefaultAsync(f => f.FlowerId == flowerId && !f.IsDeleted);

            return flower != null ? await MapToFlowerResponse(flower) : null;
        }

        private async Task<FlowerResponse> CreateFlowerAsync(CreateFlowerRequest request)
        {
            var existingInactiveFlower = await _context.FlowerInfos
                .FirstOrDefaultAsync(f => f.FlowerName.ToLower() == request.FlowerName!.ToLower() 
                                         && f.Status == "inactive" 
                                         && !f.IsDeleted);

            string? imageUrl = request.ImageUrl;
            if (request.ImageFile != null)
            {
                imageUrl = await _storageService.UploadFileAsync(request.ImageFile, "flowers");
            }

            if (existingInactiveFlower != null)
            {
                existingInactiveFlower.Status = "active";
                existingInactiveFlower.UpdatedAt = DateTime.Now;
                existingInactiveFlower.FlowerDescription = request.FlowerDescription ?? existingInactiveFlower.FlowerDescription;
                existingInactiveFlower.Price = request.Price ?? existingInactiveFlower.Price;
                existingInactiveFlower.ImageUrl = imageUrl ?? existingInactiveFlower.ImageUrl;
                existingInactiveFlower.AvailableQuantity = request.AvailableQuantity ?? existingInactiveFlower.AvailableQuantity;
                existingInactiveFlower.CategoryId = request.CategoryId ?? existingInactiveFlower.CategoryId;
                existingInactiveFlower.SellerId = request.SellerId ?? existingInactiveFlower.SellerId;

                await _context.SaveChangesAsync();
                return await MapToFlowerResponse(existingInactiveFlower);
            }

            var flower = new FlowerInfo
            {
                FlowerName = request.FlowerName!,
                FlowerDescription = request.FlowerDescription,
                Price = request.Price!.Value,
                ImageUrl = imageUrl,
                AvailableQuantity = request.AvailableQuantity!.Value,
                Status = request.Status ?? "active",
                CategoryId = request.CategoryId!.Value,
                SellerId = request.SellerId!.Value,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            _context.FlowerInfos.Add(flower);
            await _context.SaveChangesAsync();

            return await MapToFlowerResponse(flower);
        }

        private async Task<FlowerResponse> UpdateFlowerAsync(CreateFlowerRequest request)
        {
            var flower = await _context.FlowerInfos
                .FirstOrDefaultAsync(f => f.FlowerId == request.FlowerId!.Value && !f.IsDeleted);

            if (flower == null)
            {
                throw new InvalidOperationException($"Flower with ID {request.FlowerId} not found");
            }

            if (!string.IsNullOrWhiteSpace(request.FlowerName))
                flower.FlowerName = request.FlowerName;

            if (request.FlowerDescription != null)
                flower.FlowerDescription = request.FlowerDescription;

            if (request.Price.HasValue)
                flower.Price = request.Price.Value;

            if (request.ImageFile != null)
            {
                flower.ImageUrl = await _storageService.UploadFileAsync(request.ImageFile, "flowers");
            }
            else if (request.ImageUrl != null)
            {
                flower.ImageUrl = request.ImageUrl;
            }

            if (request.AvailableQuantity.HasValue)
            {
                flower.AvailableQuantity = request.AvailableQuantity.Value;
                if (request.AvailableQuantity.Value == 0 && flower.Status == "active")
                {
                    flower.Status = "inactive";
                }
                else if (request.AvailableQuantity.Value > 0 && flower.Status == "inactive")
                {
                    flower.Status = "active";
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
                flower.Status = request.Status;

            if (request.CategoryId.HasValue)
                flower.CategoryId = request.CategoryId.Value;

            if (request.SellerId.HasValue)
                flower.SellerId = request.SellerId.Value;

            flower.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await MapToFlowerResponse(flower);
        }

        private async Task<FlowerResponse> DeleteFlowerAsync(int flowerId)
        {
            var flower = await _context.FlowerInfos
                .FirstOrDefaultAsync(f => f.FlowerId == flowerId && !f.IsDeleted);

            if (flower == null)
            {
                throw new InvalidOperationException($"Flower with ID {flowerId} not found");
            }

            flower.Status = "inactive";
            flower.IsDeleted = true;
            flower.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return await MapToFlowerResponse(flower);
        }

        private async Task<FlowerResponse> MapToFlowerResponse(FlowerInfo flower)
        {
            if (flower.Category == null && flower.CategoryId.HasValue)
            {
                flower.Category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == flower.CategoryId.Value);
            }

            if (flower.Seller == null && flower.SellerId.HasValue)
            {
                flower.Seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.SellerId == flower.SellerId.Value);
            }

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
                SellerId = flower.SellerId,
                SellerShopName = flower.Seller?.ShopName,
                IsDeleted = flower.IsDeleted
            };
        }
    }
}
