using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Auth;
using PlatformFlower.Models.DTOs.User;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Auth;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Controllers.Authentication
{
    [ApiController]
    [Route("api/auth")]
    public class RegisterController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public RegisterController(
            IAuthService authService,
            IProfileService profileService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _authService = authService;
            _profileService = profileService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="registerDto">User registration data</param>
        /// <returns>Created user information</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest registerDto)
        {
            try
            {
                _logger.LogInformation($"Registration attempt for username: {registerDto?.Username}");

                // Validation is now handled entirely by AuthValidation class
                // Call service to handle business logic
                var authResponse = await _authService.RegisterUserAsync(registerDto!);

                // Return success response
                var response = _responseService.CreateSuccessResponse(
                    authResponse,
                    "User registered successfully. You are now logged in."
                );

                _logger.LogInformation($"User registered and logged in successfully: {authResponse.User.Username}");
                return CreatedAtAction(nameof(GetUser), new { id = authResponse.User.UserId }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Registration failed - business rule violation: {ex.Message}");
                var response = _responseService.CreateErrorResponse<LoginResponse>(ex.Message);
                return Conflict(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Registration failed - invalid argument: {ex.Message}");
                var response = _responseService.CreateErrorResponse<LoginResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during registration: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<LoginResponse>(
                    "An unexpected error occurred during registration"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get user by ID (Protected - requires JWT token)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("user/{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetUser(int id)
        {
            try
            {
                _logger.LogInformation($"Getting user by ID: {id}");

                var user = await _profileService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<UserResponse>("User not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(user, "User retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user by ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponse>(
                    "An error occurred while retrieving user information"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get current user profile (Protected - requires JWT token)
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetCurrentUserProfile()
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
                _logger.LogError($"Error getting current user profile: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponse>(
                    "An error occurred while retrieving profile information"
                );
                return StatusCode(500, response);
            }
        }
    }
}
