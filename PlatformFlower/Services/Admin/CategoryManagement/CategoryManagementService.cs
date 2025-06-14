using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs.Category;


namespace PlatformFlower.Services.Admin.CategoryManagement
{
    public class CategoryManagementService : ICategoryManagementService
    {
        private readonly FlowershopContext _context;

        public CategoryManagementService(FlowershopContext context)
        {
            _context = context;
        }

        public async Task<CategoryResponse> ManageCategoryAsync(CreateCategoryRequest request)
        {
            // Validate request
            ValidateRequest(request);

            // Determine operation type
            if (request.CategoryId == null || request.CategoryId == 0)
            {
                // CREATE operation
                return await CreateCategoryAsync(request);
            }
            else if (request.IsDeleted)
            {
                // DELETE operation (soft delete)
                return await DeleteCategoryAsync(request.CategoryId.Value);
            }
            else
            {
                // UPDATE operation
                return await UpdateCategoryAsync(request);
            }
        }

        private async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
        {
            // Check if category name already exists
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == request.CategoryName!.ToLower());

            if (existingCategory != null)
            {
                throw new InvalidOperationException($"Category '{request.CategoryName}' already exists");
            }

            var category = new Category
            {
                CategoryName = request.CategoryName!,
                Status = request.Status ?? "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return await MapToCategoryResponse(category);
        }

        private async Task<CategoryResponse> UpdateCategoryAsync(CreateCategoryRequest request)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);

            if (category == null)
            {
                throw new InvalidOperationException($"Category with ID {request.CategoryId} not found");
            }

            // Check if new name conflicts with existing category (excluding current one)
            if (!string.IsNullOrEmpty(request.CategoryName))
            {
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == request.CategoryName.ToLower() 
                                            && c.CategoryId != request.CategoryId);

                if (existingCategory != null)
                {
                    throw new InvalidOperationException($"Category '{request.CategoryName}' already exists");
                }

                category.CategoryName = request.CategoryName;
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                category.Status = request.Status;
            }

            category.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return await MapToCategoryResponse(category);
        }

        private async Task<CategoryResponse> DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            if (category == null)
            {
                throw new InvalidOperationException($"Category with ID {categoryId} not found");
            }

            // Check if category has flowers
            var flowerCount = await _context.FlowerInfos
                .CountAsync(f => f.CategoryId == categoryId);

            if (flowerCount > 0)
            {
                throw new InvalidOperationException($"Cannot delete category. It contains {flowerCount} flower(s). Please move or delete flowers first.");
            }

            // Soft delete - set status to inactive
            category.Status = "inactive";
            category.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return await MapToCategoryResponse(category);
        }

        public async Task<List<CategoryResponse>> GetAllCategoriesAsync()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            var categoryDtos = new List<CategoryResponse>();
            foreach (var category in categories)
            {
                categoryDtos.Add(await MapToCategoryResponse(category));
            }

            return categoryDtos;
        }

        public async Task<CategoryResponse?> GetCategoryByIdAsync(int categoryId)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

            return category == null ? null : await MapToCategoryResponse(category);
        }

        private async Task<CategoryResponse> MapToCategoryResponse(Category category)
        {
            var flowerCount = await _context.FlowerInfos
                .CountAsync(f => f.CategoryId == category.CategoryId);

            return new CategoryResponse
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Status = category.Status,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                FlowerCount = flowerCount
            };
        }

        private static void ValidateRequest(CreateCategoryRequest request)
        {
            // For CREATE operation
            if (request.CategoryId == null || request.CategoryId == 0)
            {
                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    throw new ArgumentException("Category name is required for creating a new category");
                }
            }
            // For UPDATE operation
            else if (!request.IsDeleted)
            {
                if (string.IsNullOrWhiteSpace(request.CategoryName) && string.IsNullOrWhiteSpace(request.Status))
                {
                    throw new ArgumentException("At least category name or status must be provided for update");
                }
            }

            // Validate status if provided
            if (!string.IsNullOrEmpty(request.Status) && 
                request.Status != "active" && request.Status != "inactive")
            {
                throw new ArgumentException("Status must be either 'active' or 'inactive'");
            }
        }
    }
}
