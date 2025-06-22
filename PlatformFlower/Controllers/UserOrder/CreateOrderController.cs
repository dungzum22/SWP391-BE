using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Order;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Order;

namespace PlatformFlower.Controllers.UserOrder
{
    [ApiController]
    [Route("api/orders")]
    [Authorize(Roles = "user")]
    public class CreateOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public CreateOrderController(
            IOrderService orderService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _orderService = orderService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<ActionResult<ApiResponse<OrderResponse>>> CreateOrder([FromForm] CreateOrderRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<OrderResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} creating order from cart");

                var result = await _orderService.CreateOrderAsync(userId, request);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Order created successfully. Please complete payment."
                );

                _logger.LogInformation($"Order {result.OrderId} created successfully for user {userId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Order creation validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<OrderResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Order creation operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<OrderResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating order: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<OrderResponse>(
                    "An error occurred while creating the order"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("my-orders")]
        public async Task<ActionResult<ApiResponse<List<OrderResponse>>>> GetMyOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<OrderResponse>>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var result = await _orderService.GetUserOrdersAsync(userId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} orders successfully"
                );

                _logger.LogInformation($"User {userId} retrieved {result.Count} orders");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user orders: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<OrderResponse>>(
                    "An error occurred while retrieving orders"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<ApiResponse<OrderResponse>>> GetOrderById(int orderId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<OrderResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var result = await _orderService.GetOrderByIdAsync(orderId, userId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<OrderResponse>(
                        "Order not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Order retrieved successfully"
                );

                _logger.LogInformation($"User {userId} retrieved order {orderId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving order {orderId}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<OrderResponse>(
                    "An error occurred while retrieving the order"
                );
                return StatusCode(500, response);
            }
        }
    }
}
