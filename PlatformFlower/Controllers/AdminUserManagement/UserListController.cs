using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Admin.UserManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.AdminUserManagement
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class UserListController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public UserListController(
            IUserManagementService userManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _userManagementService = userManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<List<UserListResponseDto>>>> GetUsers()
        {
            try
            {
                _logger.LogInformation("Admin getting all users list");

                var result = await _userManagementService.GetAllUsersAsync();

                var response = _responseService.CreateSuccessResponse(result, "Users retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting users list: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<UserListResponseDto>>(
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
    }
}
