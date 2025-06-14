using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Controllers.ProfileUser
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class UpdateProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public UpdateProfileController(
            IProfileService profileService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _profileService = profileService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }


        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile([FromForm] UpdateUserRequest updateDto)
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<UserResponse>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Profile update attempt for user ID: {userId}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<UserResponse>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to update user info
                var updatedUser = await _profileService.UpdateUserInfoAsync(userId, updateDto);

                // Return success response
                var response = _responseService.CreateSuccessResponse(
                    updatedUser,
                    "Profile updated successfully"
                );

                _logger.LogInformation($"Profile updated successfully for user ID: {userId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Profile update failed - invalid argument: {ex.Message}");
                var response = _responseService.CreateErrorResponse<UserResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Profile update failed - operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<UserResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during profile update: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponse>(
                    "An unexpected error occurred during profile update"
                );
                return StatusCode(500, response);
            }
        }
    }
}
