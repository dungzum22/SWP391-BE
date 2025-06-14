using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Category;
using PlatformFlower.Services.Common.Category;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.PublicCategory
{
    [ApiController]
    [Route("api/categories")]
    public class GetTopPopularCategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetTopPopularCategoriesController(
            ICategoryService categoryService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryService = categoryService;
            _responseService = responseService;
            _logger = logger;
        }


        [HttpGet("top-popular")]
        public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetTopPopularCategories()
        {
            try
            {
                _logger.LogInformation("Public API: Getting top 3 most popular active categories for header");

                var result = await _categoryService.GetTopPopularCategoriesAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Top popular categories retrieved successfully"
                );

                _logger.LogInformation($"Successfully retrieved {result.Count} top popular categories");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting top popular categories: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<CategoryResponse>>(
                    "An error occurred while retrieving top popular categories"
                );
                return StatusCode(500, response);
            }
        }
    }
}
