using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User;

namespace PlatformFlower.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public UserController(
            IUserService userService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _userService = userService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Update current user's information including avatar upload
        /// </summary>
        /// <param name="updateDto">Update data</param>
        /// <returns>Updated user information</returns>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> UpdateProfile([FromForm] UpdateUserInfoDto updateDto)
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<UserResponseDto>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Profile update attempt for user ID: {userId}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<UserResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to update user info
                var updatedUser = await _userService.UpdateUserInfoAsync(userId, updateDto);

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
                var response = _responseService.CreateErrorResponse<UserResponseDto>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Profile update failed - operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<UserResponseDto>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during profile update: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponseDto>(
                    "An unexpected error occurred during profile update"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get current user's profile information
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetProfile()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<UserResponseDto>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Getting profile for user ID: {userId}");

                var user = await _userService.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<UserResponseDto>("User not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(user, "Profile retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during profile retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponseDto>(
                    "An unexpected error occurred during profile retrieval"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Test Cloudinary connection (Development only)
        /// </summary>
        /// <returns>Cloudinary connection status</returns>
        [HttpGet("test-cloudinary-config")]
        public ActionResult<ApiResponse<object>> TestCloudinaryConfig()
        {
            try
            {
                var cloudinaryInfo = new
                {
                    CloudNameConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.CloudName),
                    ApiKeyConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.ApiKey),
                    ApiSecretConfigured = !string.IsNullOrEmpty(HttpContext.RequestServices
                        .GetService<PlatformFlower.Services.Common.Configuration.ICloudinaryConfiguration>()?.ApiSecret)
                };

                var response = _responseService.CreateSuccessResponse(cloudinaryInfo, "Cloudinary configuration check");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cloudinary config test failed: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<object>($"Cloudinary config test failed: {ex.Message}");
                return StatusCode(500, response);
            }
        }
    }
}
