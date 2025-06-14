using Microsoft.EntityFrameworkCore;
using PlatformFlower.Entities;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;

namespace PlatformFlower.Services.Common.Category
{
    public class CategoryService : ICategoryService
    {
        private readonly FlowershopContext _context;
        private readonly IAppLogger _logger;

        public CategoryService(FlowershopContext context, IAppLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CategoryResponseDto>> GetActiveCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Getting all active categories for public access");

                var categories = await _context.Categories
                    .Where(c => c.Status == "active")
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = new List<CategoryResponseDto>();
                foreach (var category in categories)
                {
                    categoryDtos.Add(await MapToCategoryResponseDto(category));
                }

                _logger.LogInformation($"Retrieved {categoryDtos.Count} active categories");
                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active categories: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<CategoryResponseDto?> GetActiveCategoryByIdAsync(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Getting active category by ID: {categoryId}");

                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryId == categoryId && c.Status == "active");

                if (category == null)
                {
                    _logger.LogWarning($"Active category not found with ID: {categoryId}");
                    return null;
                }

                var result = await MapToCategoryResponseDto(category);
                _logger.LogInformation($"Retrieved active category: {category.CategoryName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active category by ID {categoryId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<CategoryResponseDto>> GetTopPopularCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Getting top 3 most popular active categories for header display");

                // Get categories with flower count, filter active only, order by flower count desc then by name asc
                var topCategories = await _context.Categories
                    .Where(c => c.Status == "active")
                    .Select(c => new
                    {
                        Category = c,
                        FlowerCount = _context.FlowerInfos.Count(f => f.CategoryId == c.CategoryId)
                    })
                    .OrderByDescending(x => x.FlowerCount)
                    .ThenBy(x => x.Category.CategoryName)
                    .Take(3)
                    .ToListAsync();

                var categoryDtos = new List<CategoryResponseDto>();
                foreach (var item in topCategories)
                {
                    var dto = new CategoryResponseDto
                    {
                        CategoryId = item.Category.CategoryId,
                        CategoryName = item.Category.CategoryName,
                        Status = item.Category.Status,
                        CreatedAt = item.Category.CreatedAt,
                        UpdatedAt = item.Category.UpdatedAt,
                        FlowerCount = item.FlowerCount
                    };
                    categoryDtos.Add(dto);
                }

                _logger.LogInformation($"Retrieved {categoryDtos.Count} top popular categories");
                return categoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting top popular categories: {ex.Message}", ex);
                throw;
            }
        }

        private async Task<CategoryResponseDto> MapToCategoryResponseDto(Entities.Category category)
        {
            var flowerCount = await _context.FlowerInfos
                .CountAsync(f => f.CategoryId == category.CategoryId);

            return new CategoryResponseDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Status = category.Status,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                FlowerCount = flowerCount
            };
        }
    }
}
