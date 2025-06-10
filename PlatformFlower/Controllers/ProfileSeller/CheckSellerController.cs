using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Seller.Profile;

namespace PlatformFlower.Controllers.ProfileSeller
{
    [ApiController]
    [Route("api/seller")]
    [Authorize]
    public class CheckSellerController : ControllerBase
    {
        private readonly ISellerProfileService _sellerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public CheckSellerController(
            ISellerProfileService sellerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _sellerService = sellerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<SellerResponseDto>>> GetSellerProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Getting seller profile for user ID: {userId}");

                var seller = await _sellerService.GetSellerByUserIdAsync(userId);

                if (seller == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Seller profile not found. Please register as a seller first.");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(seller, "Seller profile retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller profile retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerResponseDto>(
                    "An unexpected error occurred during seller profile retrieval"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("check-status")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckSellerStatus()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<bool>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Checking seller status for user ID: {userId}");

                var isSeller = await _sellerService.IsUserSellerAsync(userId);

                var response = _responseService.CreateSuccessResponse(isSeller,
                    isSeller ? "User is a seller" : "User is not a seller");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller status check: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<bool>(
                    "An unexpected error occurred during seller status check"
                );
                return StatusCode(500, response);
            }
        }
    }
}
