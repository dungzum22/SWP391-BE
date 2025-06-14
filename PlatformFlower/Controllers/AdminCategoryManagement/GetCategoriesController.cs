using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Admin.CategoryManagement;

namespace PlatformFlower.Controllers.AdminCategoryManagement
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class GetCategoriesController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;

        public GetCategoriesController(ICategoryManagementService categoryManagementService)
        {
            _categoryManagementService = categoryManagementService;
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        /// <returns>List of all categories</returns>
        [HttpGet]
        public async Task<ActionResult<List<CategoryResponse>>> GetAllCategories()
        {
            try
            {
                var result = await _categoryManagementService.GetAllCategoriesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Categories retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving categories",
                    error = "InternalServerError",
                    details = ex.Message
                });
            }
        }
    }
}
