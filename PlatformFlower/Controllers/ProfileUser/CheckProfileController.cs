using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Controllers.ProfileUser
{
    [ApiController]
    [Route("api/user")]
    [Authorize]
    public class CheckProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public CheckProfileController(
            IProfileService profileService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _profileService = profileService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<UserResponse>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Getting profile for user ID: {userId}");

                var user = await _profileService.GetUserByIdAsync(userId);
                
                if (user == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<UserResponse>("User not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(user, "Profile retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during profile retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponse>(
                    "An unexpected error occurred during profile retrieval"
                );
                return StatusCode(500, response);
            }
        }
    }
}
