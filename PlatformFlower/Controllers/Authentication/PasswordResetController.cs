using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.User.Auth;

namespace PlatformFlower.Controllers.Authentication
{
    [ApiController]
    [Route("api/auth")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public PasswordResetController(
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
