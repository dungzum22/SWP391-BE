

using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Admin.CategoryManagement
{
    public interface ICategoryManagementService
    {
        /// <summary>
        /// Universal category management - handles CREATE, UPDATE, DELETE in one API
        /// </summary>
        /// <param name="request">Category management request</param>
        /// <returns>Category response with operation result</returns>
        Task<CategoryResponse> ManageCategoryAsync(CreateCategoryRequest request);

        /// <summary>
        /// Get all categories
        /// </summary>
        /// <returns>List of all categories</returns>
        Task<List<CategoryResponse>> GetAllCategoriesAsync();

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Category details</returns>
        Task<CategoryResponse?> GetCategoryByIdAsync(int categoryId);
    }
}
