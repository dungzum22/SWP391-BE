using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Auth;

namespace PlatformFlower.Controllers.Authentication
{
    [ApiController]
    [Route("api/auth")]
    public class ValidationController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public ValidationController(
            IAuthService authService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _authService = authService;
            _responseService = responseService;
            _logger = logger;
        }

        /// <summary>
        /// Check if username is available
        /// </summary>
        /// <param name="username">Username to check</param>
        /// <returns>Availability status</returns>
        [HttpGet("check-username/{username}")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUsernameAvailability(string username)
        {
            try
            {
                _logger.LogInformation($"Checking username availability: {username}");

                var exists = await _authService.IsUsernameExistsAsync(username);
                var isAvailable = !exists;

                var response = _responseService.CreateSuccessResponse(
                    isAvailable,
                    isAvailable ? "Username is available" : "Username is already taken"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking username availability: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<bool>(
                    "An error occurred while checking username availability"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Check if email is available
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <returns>Availability status</returns>
        [HttpGet("check-email")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckEmailAvailability([FromQuery] string email)
        {
            try
            {
                _logger.LogInformation($"Checking email availability: {email}");

                if (string.IsNullOrWhiteSpace(email))
                {
                    var badRequestResponse = _responseService.CreateErrorResponse<bool>("Email is required");
                    return BadRequest(badRequestResponse);
                }

                var exists = await _authService.IsEmailExistsAsync(email);
                var isAvailable = !exists;

                var response = _responseService.CreateSuccessResponse(
                    isAvailable,
                    isAvailable ? "Email is available" : "Email is already registered"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking email availability: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<bool>(
                    "An error occurred while checking email availability"
                );
                return StatusCode(500, response);
            }
        }
    }
}
