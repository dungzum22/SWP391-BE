using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Cart;

namespace PlatformFlower.Controllers.UserCart
{
    [ApiController]
    [Route("api/cart")]
    [Authorize(Roles = "user")]
    public class RemoveCartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public RemoveCartController(
            ICartService cartService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _cartService = cartService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpDelete("remove/{cartId}")]
        public async Task<IActionResult> RemoveCartItem(int cartId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<object>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                CartValidation.ValidateCartId(cartId);

                await _cartService.RemoveCartItemAsync(userId, cartId);

                _logger.LogInformation($"User {userId} removed cart item {cartId} successfully");
                var response = _responseService.CreateSuccessResponse<object>(null, "Cart item removed successfully");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Cart validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<object>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Cart operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<object>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in RemoveCartItem: {ex.Message}");
                var response = _responseService.CreateErrorResponse<object>("An error occurred while removing cart item");
                return StatusCode(500, response);
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<object>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                await _cartService.ClearCartAsync(userId);

                _logger.LogInformation($"User {userId} cleared cart successfully");
                var response = _responseService.CreateSuccessResponse<object>(null, "Cart cleared successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in ClearCart: {ex.Message}");
                var response = _responseService.CreateErrorResponse<object>("An error occurred while clearing cart");
                return StatusCode(500, response);
            }
        }
    }
}
