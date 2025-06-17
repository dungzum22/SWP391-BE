

using PlatformFlower.Models.DTOs.Category;

namespace PlatformFlower.Services.Admin.CategoryManagement
{
    public interface ICategoryManagementService
    {
        Task<CategoryResponse> ManageCategoryAsync(CreateCategoryRequest request);

        Task<List<CategoryResponse>> GetAllCategoriesAsync();

        Task<CategoryResponse?> GetCategoryByIdAsync(int categoryId);
    }
}
