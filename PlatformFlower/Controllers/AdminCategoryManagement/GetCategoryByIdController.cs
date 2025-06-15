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
    public class GetCategoryByIdController : ControllerBase
    {
        private readonly ICategoryManagementService _categoryManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetCategoryByIdController(
            ICategoryManagementService categoryManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryManagementService = categoryManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetCategoryById(int id)
        {
            try
            {
                _logger.LogInformation($"Admin getting category by ID: {id}");

                var result = await _categoryManagementService.GetCategoryByIdAsync(id);

                if (result == null)
                {
                    _logger.LogWarning($"Category with ID {id} not found for admin request");
                    var notFoundResponse = _responseService.CreateErrorResponse<CategoryResponse>(
                        $"Category with ID {id} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Category retrieved successfully"
                );

                _logger.LogInformation($"Successfully retrieved category {result.CategoryName} (ID: {id}) for admin");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting category by ID {id} for admin: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<CategoryResponse>(
                    "An error occurred while retrieving the category"
                );
                return StatusCode(500, response);
            }
        }
    }
}
