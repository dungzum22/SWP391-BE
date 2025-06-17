using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Admin.CategoryManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.AdminCategoryManagement
{
    [ApiController]
    [Route("api/admin/categories")]
    [Authorize(Roles = "admin")]
    public class GetCategoriesController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetCategoriesController(
            ICategoryManagementService categoryManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryManagementService = categoryManagementService;
            _responseService = responseService;
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetAllCategories()
        {
            try
            {
                _logger.LogInformation("Admin getting all categories");

                var result = await _categoryManagementService.GetAllCategoriesAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Categories retrieved successfully"
                );

                _logger.LogInformation($"Successfully retrieved {result.Count} categories for admin");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting categories for admin: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<CategoryResponse>>(
                    "An error occurred while retrieving categories"
                );
                return StatusCode(500, response);
            }
        }
    }
}
