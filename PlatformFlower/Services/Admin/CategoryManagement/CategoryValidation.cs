using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Admin.CategoryManagement
{
    /// <summary>
    /// Centralized validation logic for Category operations
    /// Follows SOLID principles - Single Responsibility for validation
    /// </summary>
    public static class CategoryValidation
    {
        /// <summary>
        /// Validate category creation request
        /// </summary>
        public static async Task ValidateCreateCategoryAsync(CreateCategoryRequest request, FlowershopContext context)
        {
            // Basic field validation
            ValidateBasicFields(request, isCreate: true);
            
            // Business logic validation - uniqueness check
            await ValidateUniqueCategoryName(request.CategoryName!, context, excludeId: null);
        }

        /// <summary>
        /// Validate category update request
        /// </summary>
        public static async Task ValidateUpdateCategoryAsync(CreateCategoryRequest request, FlowershopContext context)
        {
            // Basic field validation
            ValidateBasicFields(request, isCreate: false);
            
            // Ensure category exists
            await ValidateCategoryExists(request.CategoryId!.Value, context);
            
            // Business logic validation - uniqueness check (exclude current category)
            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                await ValidateUniqueCategoryName(request.CategoryName, context, excludeId: request.CategoryId.Value);
            }
        }

        /// <summary>
        /// Validate category deletion request
        /// </summary>
        public static async Task ValidateDeleteCategoryAsync(int categoryId, FlowershopContext context)
        {
            // Ensure category exists
            await ValidateCategoryExists(categoryId, context);
            
            // Business rule: Cannot delete category if it has active products
            await ValidateCanDeleteCategory(categoryId, context);
        }

        #region Private Validation Methods

        /// <summary>
        /// Validate basic field requirements
        /// </summary>
        private static void ValidateBasicFields(CreateCategoryRequest request, bool isCreate)
        {
            if (isCreate)
            {
                // CREATE operation - category name is required
                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    throw new ArgumentException("Category name is required for creating a new category");
                }
            }
            else
            {
                // UPDATE operation - at least one field must be provided
                if (string.IsNullOrWhiteSpace(request.CategoryName) && string.IsNullOrWhiteSpace(request.Status))
                {
                    throw new ArgumentException("At least category name or status must be provided for update");
                }
            }

            // Validate category name length if provided
            if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                if (request.CategoryName.Length > 255)
                {
                    throw new ArgumentException("Category name cannot exceed 255 characters");
                }

                if (request.CategoryName.Length < 2)
                {
                    throw new ArgumentException("Category name must be at least 2 characters");
                }
            }

            // Validate status if provided
            if (!string.IsNullOrEmpty(request.Status) &&
                request.Status != "active" && request.Status != "inactive")
            {
                throw new ArgumentException("Status must be either 'active' or 'inactive'");
            }
        }

        /// <summary>
        /// Validate that category name is unique
        /// </summary>
        private static async Task ValidateUniqueCategoryName(string categoryName, FlowershopContext context, int? excludeId)
        {
            var query = context.Categories.Where(c => c.CategoryName.ToLower() == categoryName.ToLower());
            
            // Exclude current category for update operations
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CategoryId != excludeId.Value);
            }

            if (await query.AnyAsync())
            {
                throw new InvalidOperationException($"Category name '{categoryName}' already exists");
            }
        }

        /// <summary>
        /// Validate that category exists in database
        /// </summary>
        private static async Task ValidateCategoryExists(int categoryId, FlowershopContext context)
        {
            if (!await context.Categories.AnyAsync(c => c.CategoryId == categoryId))
            {
                throw new ArgumentException($"Category with ID {categoryId} not found");
            }
        }

        /// <summary>
        /// Validate that category can be safely deleted
        /// </summary>
        private static async Task ValidateCanDeleteCategory(int categoryId, FlowershopContext context)
        {
            // Check if category has any products
            var hasProducts = await context.FlowerInfos.AnyAsync(f => f.CategoryId == categoryId);
            
            if (hasProducts)
            {
                throw new InvalidOperationException("Cannot delete category that contains products. Please move or delete all products first.");
            }
        }

        #endregion
    }
}
