

using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Common.Category
{
    public interface ICategoryService
    {
        /// <summary>
        /// Get all active categories (for public access - users and sellers)
        /// </summary>
        /// <returns>List of active categories</returns>
        Task<List<CategoryResponse>> GetActiveCategoriesAsync();

        /// <summary>
        /// Get active category by ID (for public access - users and sellers)
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Category details if active, null if not found or inactive</returns>
        Task<CategoryResponse?> GetActiveCategoryByIdAsync(int categoryId);

        /// <summary>
        /// Get top 3 most popular active categories (by flower count) for header display
        /// </summary>
        /// <returns>List of top 3 active categories with most flowers</returns>
        Task<List<CategoryResponse>> GetTopPopularCategoriesAsync();
    }
}
