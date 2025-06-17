using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Common.Flower;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using System.Security.Claims;

namespace PlatformFlower.Controllers.SellerFlowerManagement
{
    [ApiController]
    [Route("api/seller/flowers")]
    [Authorize(Roles = "seller")]
    public class GetMyFlowersController : ControllerBase
    {
        private readonly IFlowerService _flowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;
        private readonly FlowershopContext _context;

        public GetMyFlowersController(
            IFlowerService flowerService,
            IResponseService responseService,
            IAppLogger logger,
            FlowershopContext context)
        {
            _flowerService = flowerService;
            _responseService = responseService;
            _logger = logger;
            _context = context;
        }

        [HttpGet("my-flowers")]
        public async Task<ActionResult<ApiResponse<List<FlowerResponse>>>> GetMyFlowers()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller record not found for user ID: {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                        "Seller profile not found. Please register as a seller first."
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                int sellerId = seller.SellerId;

                _logger.LogInformation($"Seller getting their flowers - SellerId: {sellerId}");

                var result = await _flowerService.GetFlowersBySellerAsync(sellerId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} flowers successfully"
                );

                _logger.LogInformation($"Successfully retrieved {result.Count} flowers for seller {sellerId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flowers for seller: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                    "An error occurred while retrieving your flowers"
                );
                return StatusCode(500, response);
            }
        }
    }
}
