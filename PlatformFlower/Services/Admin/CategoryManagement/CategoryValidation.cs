using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Admin.CategoryManagement
{
    public static class CategoryValidation
    {
        public static async Task ValidateCreateCategoryAsync(CreateCategoryRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: true);
            await ValidateUniqueCategoryName(request.CategoryName!, context, excludeId: null);
        }

        public static async Task ValidateUpdateCategoryAsync(CreateCategoryRequest request, FlowershopContext context)
        {
            ValidateBasicFields(request, isCreate: false);
            await ValidateCategoryExists(request.CategoryId!.Value, context);

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                await ValidateUniqueCategoryName(request.CategoryName, context, excludeId: request.CategoryId.Value);
            }
        }

        public static async Task ValidateDeleteCategoryAsync(int categoryId, FlowershopContext context)
        {
            await ValidateCategoryExists(categoryId, context);
            await ValidateCanDeleteCategory(categoryId, context);
        }

        public static async Task ValidateUpdateCategoryRequestAsync(UpdateCategoryRequest request, FlowershopContext context)
        {
            ValidateUpdateCategoryFields(request);
            await ValidateCategoryExists(request.CategoryId, context);
            await ValidateUniqueCategoryName(request.CategoryName, context, excludeId: request.CategoryId);
        }

        private static void ValidateBasicFields(CreateCategoryRequest request, bool isCreate)
        {
            if (isCreate)
            {
                if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
                {
                    throw new ArgumentException("Category ID should not be provided for create operation");
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    throw new ArgumentException("Category name is required for creating a new category");
                }
            }
            else
            {
                if (!request.CategoryId.HasValue || request.CategoryId.Value <= 0)
                {
                    throw new ArgumentException("Valid Category ID is required for update operation");
                }

                if (string.IsNullOrWhiteSpace(request.CategoryName) && string.IsNullOrWhiteSpace(request.Status))
                {
                    throw new ArgumentException("At least category name or status must be provided for update");
                }
            }

            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                ValidateCategoryNameFormat(request.CategoryName);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                ValidateStatusFormat(request.Status);
            }
        }

        private static async Task ValidateUniqueCategoryName(string categoryName, FlowershopContext context, int? excludeId)
        {
            var query = context.Categories.Where(c => c.CategoryName.ToLower() == categoryName.ToLower());

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                throw new InvalidOperationException($"Category name '{categoryName}' already exists");
            }
        }

        private static async Task ValidateCategoryExists(int categoryId, FlowershopContext context)
        {
            if (!await context.Categories.AnyAsync(c => c.CategoryId == categoryId))
            {
                throw new ArgumentException($"Category with ID {categoryId} not found");
            }
        }

        private static async Task ValidateCanDeleteCategory(int categoryId, FlowershopContext context)
        {
            var hasProducts = await context.FlowerInfos.AnyAsync(f => f.CategoryId == categoryId);

            if (hasProducts)
            {
                throw new InvalidOperationException("Cannot delete category that contains products. Please move or delete all products first.");
            }
        }

        private static void ValidateCategoryNameFormat(string categoryName)
        {
            if (categoryName.Length > 255)
            {
                throw new ArgumentException("Category name cannot exceed 255 characters");
            }

            if (categoryName.Length < 2)
            {
                throw new ArgumentException("Category name must be at least 2 characters");
            }

            if (!IsValidCategoryNameFormat(categoryName))
            {
                throw new ArgumentException("Category name can only contain letters, numbers, spaces, hyphens, and apostrophes");
            }

            if (categoryName != categoryName.Trim())
            {
                throw new ArgumentException("Category name cannot have leading or trailing spaces");
            }

            if (categoryName.Contains("  "))
            {
                throw new ArgumentException("Category name cannot contain consecutive spaces");
            }
        }

        private static void ValidateStatusFormat(string status)
        {
            if (status.Length > 20)
            {
                throw new ArgumentException("Status cannot exceed 20 characters");
            }

            if (status != "active" && status != "inactive")
            {
                throw new ArgumentException("Status must be either 'active' or 'inactive'");
            }
        }

        private static void ValidateUpdateCategoryFields(UpdateCategoryRequest request)
        {
            if (request.CategoryId <= 0)
            {
                throw new ArgumentException("Category ID is required for update");
            }

            if (string.IsNullOrWhiteSpace(request.CategoryName))
            {
                throw new ArgumentException("Category name is required");
            }

            ValidateCategoryNameFormat(request.CategoryName);

            if (!string.IsNullOrEmpty(request.Status))
            {
                ValidateStatusFormat(request.Status);
            }
        }

        private static bool IsValidCategoryNameFormat(string categoryName)
        {
            return categoryName.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '\'');
        }
    }
}
