using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Auth;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Auth;

namespace PlatformFlower.Controllers.Authentication
{
    [ApiController]
    [Route("api/auth")]
    public class LoginController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public LoginController(
            IAuthService authService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _authService = authService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Login user
        /// </summary>
        /// <param name="loginDto">User login data</param>
        /// <returns>Auth response with JWT token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest loginDto)
        {
            try
            {
                _logger.LogInformation($"Login attempt for username: {loginDto?.Username}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<LoginResponse>(ModelState);
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
                var response = _responseService.CreateErrorResponse<LoginResponse>(ex.Message);
                return Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during login: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<LoginResponse>(
                    "An unexpected error occurred during login"
                );
                return StatusCode(500, response);
            }
        }
    }
}
