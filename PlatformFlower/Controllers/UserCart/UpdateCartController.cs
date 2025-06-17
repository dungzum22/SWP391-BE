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
    public class UpdateCartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public UpdateCartController(
            ICartService cartService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _cartService = cartService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPut("update/{cartId}")]
        public async Task<IActionResult> UpdateCartItem(int cartId, [FromBody] UpdateCartRequest request)
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

                CartValidation.ValidateCartId(cartId);
                CartValidation.ValidateUpdateCartRequest(request);

                var result = await _cartService.UpdateCartItemAsync(userId, cartId, request);

                _logger.LogInformation($"User {userId} updated cart item {cartId} successfully");
                var response = _responseService.CreateSuccessResponse(result, "Cart item updated successfully");
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
                _logger.LogError($"Unexpected error in UpdateCartItem: {ex.Message}");
                var response = _responseService.CreateErrorResponse<CartItemResponse>("An error occurred while updating cart item");
                return StatusCode(500, response);
            }
        }
    }
}
