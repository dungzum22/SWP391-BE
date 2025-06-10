using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Admin.UserManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;

namespace PlatformFlower.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public AdminController(
            IUserManagementService userManagementService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _userManagementService = userManagementService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<PaginatedUsersResponseDto>>> GetUsers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? userType = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                _logger.LogInformation($"Admin getting users list - Page: {pageNumber}, Size: {pageSize}");

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var result = await _userManagementService.GetUsersAsync(pageNumber, pageSize, searchTerm, userType, isActive);

                var response = _responseService.CreateSuccessResponse(result, "Users retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users list: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<PaginatedUsersResponseDto>(
                    "An error occurred while retrieving users"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> GetUserById(int id)
        {
            try
            {
                _logger.LogInformation($"Admin getting user details for ID: {id}");

                var user = await _userManagementService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<UserDetailResponseDto>("User not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(user, "User details retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user details for ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserDetailResponseDto>(
                    "An error occurred while retrieving user details"
                );
                return StatusCode(500, response);
            }
        }

        [HttpPut("users/{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> ToggleUserStatus(int id, [FromBody] UserStatusUpdateDto statusRequest)
        {
            try
            {
                _logger.LogInformation($"Admin toggling status for user ID: {id}");

                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<UserDetailResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                var result = await _userManagementService.ToggleUserStatusAsync(id, statusRequest.Reason);

                var response = _responseService.CreateSuccessResponse(result, "User status updated successfully");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Toggle user status failed - business rule violation: {ex.Message}");
                var response = _responseService.CreateErrorResponse<UserDetailResponseDto>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error toggling user status for ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserDetailResponseDto>(
                    "An error occurred while updating user status"
                );
                return StatusCode(500, response);
            }
        }
    }
}
