using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Admin.CategoryManagement;

namespace PlatformFlower.Controllers.AdminCategoryManagement
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class GetCategoryByIdController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;

        public GetCategoryByIdController(ICategoryManagementService categoryManagementService)
        {
            _categoryManagementService = categoryManagementService;
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryResponse>> GetCategoryById(int id)
        {
            try
            {
                var result = await _categoryManagementService.GetCategoryByIdAsync(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Category with ID {id} not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Category retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving the category",
                    error = "InternalServerError",
                    details = ex.Message
                });
            }
        }
    }
}
