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

        /// <summary>
        /// Get active category by ID (Public access - for users and sellers)
        /// Only returns category if status = 'active'
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <returns>Category details if active, 404 if not found or inactive</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryResponseDto>>> GetActiveCategoryById(int id)
        {
            try
            {
                _logger.LogInformation($"Public API: Getting active category by ID: {id}");

                var result = await _categoryService.GetActiveCategoryByIdAsync(id);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<CategoryResponseDto>(
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
                var response = _responseService.CreateErrorResponse<CategoryResponseDto>(
                    "An error occurred while retrieving the category"
                );
                return StatusCode(500, response);
            }
        }
    }
}
