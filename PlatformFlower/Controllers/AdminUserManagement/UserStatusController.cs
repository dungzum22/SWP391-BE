using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Admin.UserManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;

namespace PlatformFlower.Controllers.AdminUserManagement
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class UserStatusController : ControllerBase
    {
        private readonly IUserManagementService _userManagementService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public UserStatusController(
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

        [HttpPut("users/{id}/toggle-status")]
        public async Task<ActionResult<ApiResponse<UserDetailResponse>>> ToggleUserStatus(int id, [FromBody] UserStatusUpdate statusRequest)
        {
            try
            {
                _logger.LogInformation($"Admin toggling status for user ID: {id}");

                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<UserDetailResponse>(ModelState);
                    return BadRequest(validationResponse);
                }

                var result = await _userManagementService.ToggleUserStatusAsync(id, statusRequest.Reason);

                var response = _responseService.CreateSuccessResponse(result, "User status updated successfully");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Toggle user status failed - business rule violation: {ex.Message}");
                var response = _responseService.CreateErrorResponse<UserDetailResponse>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error toggling user status for ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserDetailResponse>(
                    "An error occurred while updating user status"
                );
                return StatusCode(500, response);
            }
        }
    }
}
