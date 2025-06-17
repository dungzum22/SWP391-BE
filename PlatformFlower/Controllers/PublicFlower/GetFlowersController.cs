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
    public class GetFlowersController : ControllerBase
    {
        private readonly IFlowerService _flowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetFlowersController(
            IFlowerService flowerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _flowerService = flowerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<FlowerResponse>>>> GetActiveFlowers()
        {
            try
            {
                _logger.LogInformation("Public API: Getting all active flowers");

                var result = await _flowerService.GetActiveFlowersAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Active flowers retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active flowers: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                    "An error occurred while retrieving flowers"
                );
                return StatusCode(500, response);
            }
        }
    }
}
