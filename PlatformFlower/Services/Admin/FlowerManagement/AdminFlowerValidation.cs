using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Flower;

namespace PlatformFlower.Services.Admin.FlowerManagement
{
    public static class AdminFlowerValidation
    {
        public static async Task ValidateCreateFlowerAsync(CreateFlowerRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: true);
            await ValidateUniqueFlowerName(request.FlowerName!, context, excludeId: null);
            await ValidateCategoryExists(request.CategoryId!.Value, context);
        }

        public static async Task ValidateUpdateFlowerAsync(CreateFlowerRequest request, FlowershopContext context)
        {
            if (request.FlowerId == null || request.FlowerId <= 0)
            {
                throw new ArgumentException("Valid FlowerId is required for update");
            }

            await ValidateFlowerExists(request.FlowerId.Value, context);

            if (!string.IsNullOrEmpty(request.FlowerName))
            {
                await ValidateUniqueFlowerName(request.FlowerName, context, request.FlowerId.Value);
            }

            if (request.CategoryId.HasValue)
            {
                await ValidateCategoryExists(request.CategoryId.Value, context);
            }

            ValidateBasicFields(request, isCreate: false);
        }

        public static async Task ValidateDeleteFlowerAsync(int flowerId, FlowershopContext context)
        {
            await ValidateFlowerExists(flowerId, context);
            await ValidateFlowerNotInActiveOrders(flowerId, context);
        }

        private static void ValidateBasicFields(CreateFlowerRequest request, bool isCreate)
        {
            if (isCreate)
            {
                if (string.IsNullOrWhiteSpace(request.FlowerName))
                {
                    throw new ArgumentException("Flower name is required");
                }

                if (!request.Price.HasValue || request.Price.Value <= 0)
                {
                    throw new ArgumentException("Valid price is required and must be greater than 0");
                }

                if (!request.AvailableQuantity.HasValue || request.AvailableQuantity.Value < 0)
                {
                    throw new ArgumentException("Available quantity is required and cannot be negative");
                }

                if (!request.CategoryId.HasValue)
                {
                    throw new ArgumentException("Category ID is required");
                }

                if (request.ImageFile == null && string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    throw new ArgumentException("Either an image file or image URL is required");
                }
            }
            else
            {
                if (request.Price.HasValue && request.Price.Value <= 0)
                {
                    throw new ArgumentException("Price must be greater than 0");
                }

                if (request.AvailableQuantity.HasValue && request.AvailableQuantity.Value < 0)
                {
                    throw new ArgumentException("Available quantity cannot be negative");
                }
            }

            if (!string.IsNullOrEmpty(request.FlowerName))
            {
                if (request.FlowerName.Length < 2 || request.FlowerName.Length > 255)
                {
                    throw new ArgumentException("Flower name must be between 2 and 255 characters");
                }
            }

            if (!string.IsNullOrEmpty(request.FlowerDescription) && request.FlowerDescription.Length > 500)
            {
                throw new ArgumentException("Flower description cannot exceed 500 characters");
            }

            if (!string.IsNullOrEmpty(request.Status) && !IsValidStatus(request.Status))
            {
                throw new ArgumentException("Status must be either 'active' or 'inactive'");
            }

            if (request.ImageFile != null)
            {
                ValidateImageFile(request.ImageFile);
            }
        }

        private static async Task ValidateUniqueFlowerName(string flowerName, FlowershopContext context, int? excludeId)
        {
            var query = context.FlowerInfos.Where(f => f.FlowerName == flowerName && !f.IsDeleted);

            if (excludeId.HasValue)
            {
                query = query.Where(f => f.FlowerId != excludeId.Value);
            }

            var existingFlower = await query.FirstOrDefaultAsync();

            if (existingFlower != null)
            {
                throw new InvalidOperationException($"A flower with the name '{flowerName}' already exists");
            }
        }

        private static async Task ValidateCategoryExists(int categoryId, FlowershopContext context)
        {
            var categoryExists = await context.Categories
                .AnyAsync(c => c.CategoryId == categoryId && c.Status == "active");

            if (!categoryExists)
            {
                throw new InvalidOperationException($"Category with ID {categoryId} does not exist or is inactive");
            }
        }

        private static async Task ValidateFlowerExists(int flowerId, FlowershopContext context)
        {
            var flowerExists = await context.FlowerInfos
                .AnyAsync(f => f.FlowerId == flowerId);

            if (!flowerExists)
            {
                throw new InvalidOperationException($"Flower with ID {flowerId} does not exist");
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

        private static bool IsValidStatus(string status)
        {
            return status.ToLower() == "active" || status.ToLower() == "inactive";
        }

        private static void ValidateImageFile(IFormFile imageFile)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (imageFile.Length > maxFileSize)
            {
                throw new ArgumentException($"Image file size cannot exceed {maxFileSize / (1024 * 1024)}MB");
            }

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Only image files with extensions {string.Join(", ", allowedExtensions)} are allowed");
            }

            var allowedContentTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedContentTypes.Contains(imageFile.ContentType.ToLowerInvariant()))
            {
                throw new ArgumentException("Invalid image file type");
            }
        }
    }
}
