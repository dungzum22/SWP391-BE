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
    public class AddToCartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public AddToCartController(
            ICartService cartService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _cartService = cartService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<CartItemResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                CartValidation.ValidateAddToCartRequest(request);

                var result = await _cartService.AddToCartAsync(userId, request);

                _logger.LogInformation($"User {userId} added flower {request.FlowerId} to cart successfully");
                var response = _responseService.CreateSuccessResponse(result, "Item added to cart successfully");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Cart validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CartItemResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cart operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CartItemResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in AddToCart: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CartItemResponse>("An error occurred while adding item to cart");
                return StatusCode(500, response);
            }
        }
    }
}
