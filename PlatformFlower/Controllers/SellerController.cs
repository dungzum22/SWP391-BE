using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.Seller;

namespace PlatformFlower.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class SellerController : ControllerBase
    {
        private readonly ISellerService _sellerService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public SellerController(
            ISellerService sellerService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _sellerService = sellerService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Register current user as a seller
        /// </summary>
        /// <param name="registerSellerDto">Seller registration data</param>
        /// <returns>Seller information</returns>
        [HttpPost("register")]
        public async Task<ActionResult<ApiResponse<SellerResponseDto>>> RegisterSeller([FromBody] RegisterSellerDto registerSellerDto)
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Seller registration attempt for user ID: {userId}");

                // Validate model state
                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<SellerResponseDto>(ModelState);
                    return BadRequest(validationResponse);
                }

                // Call service to handle business logic
                var sellerResponse = await _sellerService.RegisterSellerAsync(userId, registerSellerDto);

                // Return success response
                var response = _responseService.CreateSuccessResponse(
                    sellerResponse,
                    "Seller registration successful. You are now a seller!"
                );

                _logger.LogInformation($"Seller registered successfully for user ID: {userId}, seller ID: {sellerResponse.SellerId}");
                return CreatedAtAction(nameof(GetSellerProfile), new { id = sellerResponse.SellerId }, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Seller registration failed - business rule violation: {ex.Message}");
                var response = _responseService.CreateErrorResponse<SellerResponseDto>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller registration: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerResponseDto>(
                    "An unexpected error occurred during seller registration"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get current user's seller profile
        /// </summary>
        /// <returns>Seller information</returns>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<SellerResponseDto>>> GetSellerProfile()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Getting seller profile for user ID: {userId}");

                var seller = await _sellerService.GetSellerByUserIdAsync(userId);
                
                if (seller == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Seller profile not found. Please register as a seller first.");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(seller, "Seller profile retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller profile retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerResponseDto>(
                    "An unexpected error occurred during seller profile retrieval"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Get seller by ID (public endpoint for viewing seller info)
        /// </summary>
        /// <param name="id">Seller ID</param>
        /// <returns>Seller information</returns>
        [HttpGet("{id}")]
        [AllowAnonymous] // Public endpoint
        public async Task<ActionResult<ApiResponse<SellerResponseDto>>> GetSellerById(int id)
        {
            try
            {
                _logger.LogInformation($"Getting seller by ID: {id}");

                var seller = await _sellerService.GetSellerByIdAsync(id);
                
                if (seller == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<SellerResponseDto>("Seller not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(seller, "Seller retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerResponseDto>(
                    "An unexpected error occurred during seller retrieval"
                );
                return StatusCode(500, response);
            }
        }

        /// <summary>
        /// Check if current user is a seller
        /// </summary>
        /// <returns>Boolean indicating if user is a seller</returns>
        [HttpGet("check-status")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckSellerStatus()
        {
            try
            {
                // Get user ID from JWT token claims
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<bool>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Checking seller status for user ID: {userId}");

                var isSeller = await _sellerService.IsUserSellerAsync(userId);
                
                var response = _responseService.CreateSuccessResponse(isSeller, 
                    isSeller ? "User is a seller" : "User is not a seller");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller status check: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<bool>(
                    "An unexpected error occurred during seller status check"
                );
                return StatusCode(500, response);
            }
        }
    }
}
