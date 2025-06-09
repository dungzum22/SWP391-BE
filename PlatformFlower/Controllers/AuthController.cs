using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Auth;
using PlatformFlower.Services.User.Profile;

namespace PlatformFlower.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IProfileService _profileService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public AuthController(
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
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterUserDto registerDto)
        {
            try
            {
                _logger.LogInformation($"Registration attempt for username: {registerDto?.Username}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<AuthResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

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
                var response = _responseService.CreateErrorResponse<AuthResponseDto>(ex.Message);
                return Conflict(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Registration failed - invalid argument: {ex.Message}");
                var response = _responseService.CreateErrorResponse<AuthResponseDto>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during registration: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AuthResponseDto>(
                    "An unexpected error occurred during registration"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Login user
        /// </summary>
        /// <param name="loginDto">User login data</param>
        /// <returns>Auth response with JWT token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginUserDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for username: {loginDto?.Username}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<AuthResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to handle business logic
                var authResponse = await _authService.LoginUserAsync(loginDto!);

                // Return success response
                var response = _responseService.CreateSuccessResponse(
                    authResponse,
                    "Login successful"
                );

                _logger.LogInformation($"User logged in successfully: {authResponse.User.Username}");
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Login failed - unauthorized: {ex.Message}");
                var response = _responseService.CreateErrorResponse<AuthResponseDto>(ex.Message);
                return Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during login: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AuthResponseDto>(
                    "An unexpected error occurred during login"
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
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUser(int id)
        {
            try
            {
                _logger.LogInformation($"Getting user by ID: {id}");

                var user = await _profileService.GetUserByIdAsync(id);
                
                if (user == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<UserResponseDto>("User not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(user, "User retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user by ID {id}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponseDto>(
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
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetCurrentUserProfile()
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

                var user = await _profileService.GetUserByIdAsync(userId);

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
                _logger.LogError($"Error getting current user profile: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<UserResponseDto>(
                    "An error occurred while retrieving profile information"
                );
                return StatusCode(500, response);
            }
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

        /// <summary>
        /// Request password reset
        /// </summary>
        /// <param name="forgotPasswordDto">Email for password reset</param>
        /// <returns>Response indicating success or failure</returns>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<ApiResponse<ForgotPasswordResponseDto>>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                _logger.LogInformation($"Password reset request for email: {forgotPasswordDto?.Email}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<ForgotPasswordResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to handle business logic
                var result = await _authService.ForgotPasswordAsync(forgotPasswordDto!.Email);

                // Return response
                var response = _responseService.CreateSuccessResponse(result, result.Message);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during password reset request: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<ForgotPasswordResponseDto>(
                    "An unexpected error occurred during password reset request"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        /// <param name="resetPasswordDto">Reset password data</param>
        /// <returns>Response indicating success or failure</returns>
        [HttpPost("reset-password")]
        public async Task<ActionResult<ApiResponse<ForgotPasswordResponseDto>>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                _logger.LogInformation($"Password reset attempt with token: {resetPasswordDto?.Token}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<ForgotPasswordResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to handle business logic
                var result = await _authService.ResetPasswordAsync(resetPasswordDto!);

                if (result.Success)
                {
                    var response = _responseService.CreateSuccessResponse(result, result.Message);
                    return Ok(response);
                }
                else
                {
                    var response = _responseService.CreateErrorResponse<ForgotPasswordResponseDto>(result.Message);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during password reset: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<ForgotPasswordResponseDto>(
                    "An unexpected error occurred during password reset"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Validate reset password token
        /// </summary>
        /// <param name="token">Reset token to validate</param>
        /// <returns>Token validation status</returns>
        [HttpGet("validate-reset-token/{token}")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateResetToken(string token)
        {
            try
            {
                _logger.LogInformation($"Validating reset token: {token}");

                if (string.IsNullOrWhiteSpace(token))
                {
                    var badRequestResponse = _responseService.CreateErrorResponse<bool>("Token is required");
                    return BadRequest(badRequestResponse);
                }

                var isValid = await _authService.ValidateResetTokenAsync(token);

                var response = _responseService.CreateSuccessResponse(
                    isValid,
                    isValid ? "Token is valid" : "Token is invalid or expired"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating reset token: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<bool>(
                    "An error occurred while validating token"
                );
                return StatusCode(500, response);
            }
        }
    }
}
