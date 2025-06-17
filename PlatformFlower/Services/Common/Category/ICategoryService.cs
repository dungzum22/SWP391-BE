

using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Common.Category
{
    public interface ICategoryService
    {
        Task<List<CategoryResponse>> GetActiveCategoriesAsync();

        Task<CategoryResponse?> GetActiveCategoryByIdAsync(int categoryId);

        Task<List<CategoryResponse>> GetTopPopularCategoriesAsync();
    }
}
