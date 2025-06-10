using PlatformFlower.Models.DTOs;

namespace PlatformFlower.Services.Admin.CategoryManagement
{
    public interface ICategoryManagementService
    {
        /// <summary>
        /// Universal category management - handles CREATE, UPDATE, DELETE in one API
        /// </summary>
        /// <param name="request">Category management request</param>
        /// <returns>Category response with operation result</returns>
        Task<CategoryResponseDto> ManageCategoryAsync(CategoryManageRequestDto request);

        /// <summary>
        /// Get all categories
        /// </summary>
        /// <returns>List of all categories</returns>
        Task<List<CategoryResponseDto>> GetAllCategoriesAsync();

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="categoryId">Category ID</param>
        /// <returns>Category details</returns>
        Task<CategoryResponseDto?> GetCategoryByIdAsync(int categoryId);
    }
}
