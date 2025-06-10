using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Category;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.PublicCategory
{
    [ApiController]
    [Route("api/categories")]
    public class GetCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetCategoriesController(
            ICategoryService categoryService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryService = categoryService;
            _responseService = responseService;
            _logger = logger;
        }

        /// <summary>
        /// Get all active categories (Public access - for users and sellers)
        /// Only returns categories with status = 'active'
        /// </summary>
        /// <returns>List of active categories</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<CategoryResponseDto>>>> GetActiveCategories()
        {
            try
            {
                _logger.LogInformation("Public API: Getting all active categories");

                var result = await _categoryService.GetActiveCategoriesAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Active categories retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active categories: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<CategoryResponseDto>>(
                    "An error occurred while retrieving categories"
                );
                return StatusCode(500, response);
            }
        }
    }
}
