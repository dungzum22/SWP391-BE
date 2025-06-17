using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Cart;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Cart;

namespace PlatformFlower.Controllers.UserCart
{
    [ApiController]
    [Route("api/cart")]
    [Authorize(Roles = "user")]
    public class GetCartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetCartController(
            ICartService cartService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _cartService = cartService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("my-cart")]
        public async Task<IActionResult> GetMyCart()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<CartResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var result = await _cartService.GetCartAsync(userId);

                _logger.LogInformation($"User {userId} retrieved cart with {result.Items.Count} items");
                var response = _responseService.CreateSuccessResponse(result, "Cart retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in GetMyCart: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CartResponse>("An error occurred while retrieving cart");
                return StatusCode(500, response);
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCartItemCount()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<int>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var count = await _cartService.GetCartItemCountAsync(userId);

                var response = _responseService.CreateSuccessResponse(count, "Cart item count retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in GetCartItemCount: {ex.Message}");
                var response = _responseService.CreateErrorResponse<int>("An error occurred while retrieving cart count");
                return StatusCode(500, response);
            }
        }
    }
}
