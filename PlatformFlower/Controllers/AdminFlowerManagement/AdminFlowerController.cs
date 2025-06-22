using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Admin.FlowerManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.AdminFlowerManagement
{
    [ApiController]
    [Route("api/admin/flowers")]
    [Authorize(Roles = "admin")]
    public class AdminFlowerController : ControllerBase
    {
        private readonly IAdminFlowerService _adminFlowerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public AdminFlowerController(
            IAdminFlowerService adminFlowerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _adminFlowerService = adminFlowerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<FlowerResponse>>> ManageFlower([FromForm] CreateFlowerRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                string operationType;
                if (request.FlowerId == null || request.FlowerId == 0)
                {
                    operationType = "create";
                }
                else if (request.IsDeleted)
                {
                    operationType = "delete";
                }
                else
                {
                    operationType = "update";
                }

                _logger.LogInformation($"Admin managing flower - Operation: {operationType}, FlowerName: {request.FlowerName}, FlowerId: {request.FlowerId}, AdminId: {userId}, ImageFile: {(request.ImageFile != null ? $"Present ({request.ImageFile.Length} bytes)" : "None")}, ImageUrl: {(string.IsNullOrEmpty(request.ImageUrl) ? "None" : "Present")}");

                var result = await _adminFlowerService.ManageFlowerAsync(request);

                string operation;
                string message;

                if (request.FlowerId == null || request.FlowerId == 0)
                {
                    operation = "created";
                    message = result.CreatedAt < result.UpdatedAt
                        ? "Flower reactivated successfully (was previously inactive)"
                        : "Flower created successfully";
                }
                else if (request.IsDeleted)
                {
                    operation = "deleted";
                    message = "Flower deleted successfully (set to inactive and marked as deleted)";
                }
                else
                {
                    operation = "updated";
                    message = "Flower updated successfully";
                }

                var response = _responseService.CreateSuccessResponse(result, message);

                _logger.LogInformation($"Flower {operation} successfully - ID: {result.FlowerId}, Name: {result.FlowerName}, Status: {result.Status}, IsDeleted: {result.IsDeleted}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Flower management validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<FlowerResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Flower management business logic error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<FlowerResponse>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in flower management: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<FlowerResponse>(
                    "An unexpected error occurred while managing the flower"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<FlowerResponse>>>> GetAllFlowers()
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

                _logger.LogInformation($"Admin {userId} getting all flowers");

                var result = await _adminFlowerService.GetAllFlowersAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} flowers successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all flowers for admin: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<List<FlowerResponse>>(
                    "An error occurred while retrieving flowers"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("{flowerId}")]
        public async Task<ActionResult<ApiResponse<FlowerResponse>>> GetFlowerById(int flowerId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"Admin {userId} getting flower {flowerId}");

                var result = await _adminFlowerService.GetFlowerByIdAsync(flowerId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        $"Flower with ID {flowerId} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(result, "Flower retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting flower {flowerId} for admin: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                    "An error occurred while retrieving the flower"
                );
                return StatusCode(500, errorResponse);
            }
        }
    }
}
