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
    public class GetFlowerByIdController : ControllerBase
    {
        private readonly IFlowerService _flowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetFlowerByIdController(
            IFlowerService flowerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _flowerService = flowerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<FlowerResponse>>> GetActiveFlowerById(int id)
        {
            try
            {
                _logger.LogInformation($"Public API: Getting active flower by ID: {id}");

                var result = await _flowerService.GetActiveFlowerByIdAsync(id);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        $"Active flower with ID {id} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Active flower retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active flower by ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<FlowerResponse>(
                    "An error occurred while retrieving the flower"
                );
                return StatusCode(500, response);
            }
        }
    }
}
