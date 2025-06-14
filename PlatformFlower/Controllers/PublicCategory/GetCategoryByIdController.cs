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
    public class GetCategoryByIdController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetCategoryByIdController(
            ICategoryService categoryService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _categoryService = categoryService;
            _responseService = responseService;
            _logger = logger;
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetActiveCategoryById(int id)
        {
            try
            {
                _logger.LogInformation($"Public API: Getting active category by ID: {id}");

                var result = await _categoryService.GetActiveCategoryByIdAsync(id);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<CategoryResponse>(
                        $"Active category with ID {id} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Active category retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active category by ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<CategoryResponse>(
                    "An error occurred while retrieving the category"
                );
                return StatusCode(500, response);
            }
        }
    }
}
