using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Common.Flower;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.PublicFlower
{
    [ApiController]
    [Route("api/flowers")]
    public class GetFlowersByCategoryController : ControllerBase
    {
        private readonly IFlowerService _flowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetFlowersByCategoryController(
            IFlowerService flowerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _flowerService = flowerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<ApiResponse<List<FlowerResponse>>>> GetFlowersByCategory(int categoryId)
        {
            try
            {
                _logger.LogInformation($"Public API: Getting active flowers by category ID: {categoryId}");

                var result = await _flowerService.GetFlowersByCategoryAsync(categoryId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Active flowers for category {categoryId} retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flowers by category {categoryId}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                    "An error occurred while retrieving flowers by category"
                );
                return StatusCode(500, response);
            }
        }
    }
}
