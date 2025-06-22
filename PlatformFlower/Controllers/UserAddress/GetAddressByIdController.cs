using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Address;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Address;

namespace PlatformFlower.Controllers.UserAddress
{
    [ApiController]
    [Route("api/user/addresses")]
    [Authorize(Roles = "user")]
    public class GetAddressByIdController : ControllerBase
    {
        private readonly IAddressManagementService _addressManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetAddressByIdController(
            IAddressManagementService addressManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _addressManagementService = addressManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("{addressId}")]
        public async Task<ActionResult<ApiResponse<AddressResponse>>> GetAddressById(int addressId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<AddressResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} getting address by ID: {addressId}");

                var result = await _addressManagementService.GetAddressByIdAsync(addressId, userId);

                if (result == null)
                {
                    _logger.LogWarning($"Address with ID {addressId} not found for user {userId}");
                    var notFoundResponse = _responseService.CreateErrorResponse<AddressResponse>(
                        $"Address with ID {addressId} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Address retrieved successfully"
                );

                _logger.LogInformation($"Successfully retrieved address {result.Description} (ID: {addressId}) for user {userId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Address retrieval validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<AddressResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting address by ID: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AddressResponse>(
                    "An error occurred while retrieving the address"
                );
                return StatusCode(500, response);
            }
        }
    }
}
