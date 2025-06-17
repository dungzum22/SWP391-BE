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
    public class GetFlowersBySellerController : ControllerBase
    {
        private readonly IFlowerService _flowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetFlowersBySellerController(
            IFlowerService flowerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _flowerService = flowerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("seller/{sellerId}")]
        public async Task<ActionResult<ApiResponse<List<FlowerResponse>>>> GetFlowersBySeller(int sellerId)
        {
            try
            {
                _logger.LogInformation($"Public API: Getting active flowers by seller ID: {sellerId}");

                var result = await _flowerService.GetFlowersBySellerAsync(sellerId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Active flowers for seller {sellerId} retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flowers by seller {sellerId}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                    "An error occurred while retrieving flowers by seller"
                );
                return StatusCode(500, response);
            }
        }
    }
}
