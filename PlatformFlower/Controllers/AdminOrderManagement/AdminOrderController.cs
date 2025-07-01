using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Order;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Order;

namespace PlatformFlower.Controllers.AdminOrderManagement
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = "admin")]
    public class AdminOrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public AdminOrderController(
            IOrderService orderService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _orderService = orderService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AdminOrderDetailResponse>>>> GetAllOrders()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
                {
                    _logger.LogWarning("Admin user ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<AdminOrderDetailResponse>>(
                        "Admin user ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"Admin {adminUserId} getting all orders with details");

                var orders = await _orderService.GetAllOrdersWithDetailsAsync();

                var response = _responseService.CreateSuccessResponse(
                    orders,
                    $"Retrieved {orders.Count} orders with details successfully"
                );

                _logger.LogInformation($"Admin {adminUserId} retrieved {orders.Count} orders with details");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all orders for admin: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<AdminOrderDetailResponse>>(
                    "An error occurred while retrieving orders"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<ApiResponse<AdminOrderDetailResponse>>> GetOrderDetails(int orderId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
                {
                    _logger.LogWarning("Admin user ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        "Admin user ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"Admin {adminUserId} getting order details for ID: {orderId}");

                var orderDetails = await _orderService.GetOrderDetailsByIdAsync(orderId);

                if (orderDetails == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        "Order not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    orderDetails,
                    "Order details retrieved successfully"
                );

                _logger.LogInformation($"Admin {adminUserId} retrieved order details for ID: {orderId}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order details for ID {orderId}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                    "An error occurred while retrieving order details"
                );
                return StatusCode(500, response);
            }
        }

        [HttpPut("{orderId}/status")]
        public async Task<ActionResult<ApiResponse<AdminOrderDetailResponse>>> UpdateOrderStatus(int orderId, [FromForm] UpdateOrderStatusRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
                {
                    _logger.LogWarning("Admin user ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        "Admin user ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                if (string.IsNullOrEmpty(request.Status))
                {
                    var badRequestResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        "Status is required"
                    );
                    return BadRequest(badRequestResponse);
                }

                // Validate status values
                var validStatuses = new[] { "pending", "accepted", "pending delivery", "delivered", "canceled" };
                if (!validStatuses.Contains(request.Status.ToLower()))
                {
                    var badRequestResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        $"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}"
                    );
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Admin {adminUserId} updating order {orderId} status to {request.Status}");

                var result = await _orderService.UpdateOrderStatusAdminAsync(orderId, request);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                        "Order not found or status update failed"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Order status updated successfully"
                );

                _logger.LogInformation($"Admin {adminUserId} successfully updated order {orderId} status to {request.Status}");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating order {orderId} status: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AdminOrderDetailResponse>(
                    "An error occurred while updating order status"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<ApiResponse<OrderStatisticsResponse>>> GetOrderStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int adminUserId))
                {
                    _logger.LogWarning("Admin user ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<OrderStatisticsResponse>(
                        "Admin user ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"Admin {adminUserId} getting order statistics from {startDate} to {endDate}");

                var statistics = await _orderService.GetOrderStatisticsAsync(startDate, endDate);

                var response = _responseService.CreateSuccessResponse(
                    statistics,
                    "Order statistics retrieved successfully"
                );

                _logger.LogInformation($"Admin {adminUserId} retrieved order statistics: {statistics.TotalOrders} orders, {statistics.TotalRevenue:C} revenue");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting order statistics: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<OrderStatisticsResponse>(
                    "An error occurred while retrieving order statistics"
                );
                return StatusCode(500, response);
            }
        }


    }
}
