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
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public AuthController(
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
                var authResponse = await _userService.RegisterUserAsync(registerDto!);

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
                var authResponse = await _userService.LoginUserAsync(loginDto!);

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
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User information</returns>
        [HttpGet("user/{id}")]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUser(int id)
        {
            try
            {
                _logger.LogInformation($"Getting user by ID: {id}");

                var user = await _userService.GetUserByIdAsync(id);
                
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

                var exists = await _userService.IsUsernameExistsAsync(username);
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

                var exists = await _userService.IsEmailExistsAsync(email);
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
