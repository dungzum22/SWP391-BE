using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Flower;

namespace PlatformFlower.Services.Seller.FlowerManagement
{
    public static class FlowerValidation
    {
        public static async Task ValidateCreateFlowerAsync(CreateFlowerRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: true);
            await ValidateUniqueFlowerName(request.FlowerName!, context, excludeId: null);
            await ValidateCategoryExists(request.CategoryId!.Value, context);
            await ValidateSellerExists(request.SellerId!.Value, context);
        }

        public static async Task ValidateUpdateFlowerAsync(CreateFlowerRequest request, FlowershopContext context, int? currentSellerId = null)
        {
            ValidateBasicFields(request, isCreate: false);
            await ValidateFlowerExists(request.FlowerId!.Value, context);

            if (currentSellerId.HasValue)
            {
                await ValidateSellerOwnership(request.FlowerId!.Value, currentSellerId.Value, context);
            }

            if (!string.IsNullOrWhiteSpace(request.FlowerName))
            {
                await ValidateUniqueFlowerName(request.FlowerName, context, excludeId: request.FlowerId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                await ValidateCategoryExists(request.CategoryId.Value, context);
            }

            if (request.SellerId.HasValue)
            {
                await ValidateSellerExists(request.SellerId.Value, context);
            }
        }

        public static async Task ValidateDeleteFlowerAsync(int flowerId, FlowershopContext context, int? currentSellerId = null)
        {
            await ValidateFlowerExists(flowerId, context);
            
            if (currentSellerId.HasValue)
            {
                await ValidateSellerOwnership(flowerId, currentSellerId.Value, context);
            }
            
            await ValidateFlowerNotInActiveOrders(flowerId, context);
        }

        private static void ValidateBasicFields(CreateFlowerRequest request, bool isCreate)
        {
            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(request.FlowerName))
                    throw new ArgumentException("Flower name is required");

                if (!request.Price.HasValue || request.Price <= 0)
                    throw new ArgumentException("Price must be greater than 0");

                if (!request.AvailableQuantity.HasValue || request.AvailableQuantity < 0)
                    throw new ArgumentException("Available quantity must be 0 or greater");

                if (!request.CategoryId.HasValue)
                    throw new ArgumentException("Category ID is required");

                if (!request.SellerId.HasValue)
                    throw new ArgumentException("Seller ID is required");
            }

            ValidateImageData(request);
        }

        private static void ValidateImageData(CreateFlowerRequest request)
        {
            if (request.ImageFile != null)
            {
                if (request.ImageFile.Length == 0)
                    throw new ArgumentException("Image file cannot be empty");

                if (request.ImageFile.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("Image file size cannot exceed 5MB");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(request.ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    throw new ArgumentException($"Image file type {fileExtension} is not allowed. Only JPG, PNG, GIF, and WebP are supported");

                var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                if (!allowedContentTypes.Contains(request.ImageFile.ContentType.ToLowerInvariant()))
                    throw new ArgumentException($"Image content type {request.ImageFile.ContentType} is not allowed");
            }
            else if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                if (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    throw new ArgumentException("Image URL must be a valid HTTP or HTTPS URL");
            }
            else
            {
                if (request.FlowerId == null || request.FlowerId <= 0)
                    throw new ArgumentException("Flower ID is required for update");

                if (request.Price.HasValue && request.Price <= 0)
                    throw new ArgumentException("Price must be greater than 0");

                if (request.AvailableQuantity.HasValue && request.AvailableQuantity < 0)
                    throw new ArgumentException("Available quantity must be 0 or greater");
            }

            if (!string.IsNullOrWhiteSpace(request.FlowerName))
            {
                ValidateFlowerNameFormat(request.FlowerName);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                ValidateStatusFormat(request.Status);
            }
        }

        private static async Task ValidateUniqueFlowerName(string flowerName, FlowershopContext context, int? excludeId)
        {
            var query = context.FlowerInfos.Where(f => f.FlowerName.ToLower() == flowerName.ToLower() && !f.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(f => f.FlowerId != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                throw new InvalidOperationException($"Flower name '{flowerName}' already exists");
            }
        }

        private static async Task ValidateFlowerExists(int flowerId, FlowershopContext context)
        {
            var exists = await context.FlowerInfos.AnyAsync(f => f.FlowerId == flowerId && !f.IsDeleted);
            if (!exists)
            {
                throw new InvalidOperationException($"Flower with ID {flowerId} not found");
            }
        }

        private static async Task ValidateCategoryExists(int categoryId, FlowershopContext context)
        {
            var exists = await context.Categories.AnyAsync(c => c.CategoryId == categoryId && c.Status == "active");
            if (!exists)
            {
                throw new InvalidOperationException($"Active category with ID {categoryId} not found");
            }
        }

        private static async Task ValidateSellerExists(int sellerId, FlowershopContext context)
        {
            var exists = await context.Sellers.AnyAsync(s => s.SellerId == sellerId);
            if (!exists)
            {
                throw new InvalidOperationException($"Seller with ID {sellerId} not found");
            }
        }

        private static async Task ValidateSellerOwnership(int flowerId, int sellerId, FlowershopContext context)
        {
            var flower = await context.FlowerInfos.FirstOrDefaultAsync(f => f.FlowerId == flowerId && !f.IsDeleted);
            if (flower == null)
            {
                throw new InvalidOperationException($"Flower with ID {flowerId} not found");
            }

            if (flower.SellerId != sellerId)
            {
                throw new UnauthorizedAccessException("You can only manage your own flowers");
            }
        }

        private static async Task ValidateFlowerNotInActiveOrders(int flowerId, FlowershopContext context)
        {
            var hasActiveOrders = await context.OrdersDetails
                .AnyAsync(od => od.FlowerId == flowerId && od.Status != "cancelled");

            if (hasActiveOrders)
            {
                throw new InvalidOperationException("Cannot delete flower that has active orders");
            }
        }

        private static void ValidateFlowerNameFormat(string flowerName)
        {
            if (flowerName.Length > 255)
            {
                throw new ArgumentException("Flower name must not exceed 255 characters");
            }

            if (flowerName.Trim().Length == 0)
            {
                throw new ArgumentException("Flower name cannot be empty or whitespace");
            }
        }

        private static void ValidateStatusFormat(string status)
        {
            var validStatuses = new[] { "active", "inactive" };
            if (!validStatuses.Contains(status.ToLower()))
            {
                throw new ArgumentException($"Status must be one of: {string.Join(", ", validStatuses)}");
            }
        }
    }
}
