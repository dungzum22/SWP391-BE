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
    public class GetAddressesController : ControllerBase
    {
        private readonly IAddressManagementService _addressManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public GetAddressesController(
            IAddressManagementService addressManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _addressManagementService = addressManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<AddressResponse>>>> GetAddresses()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<AddressResponse>>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} getting all addresses");

                var result = await _addressManagementService.GetAllAddressesAsync(userId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} addresses successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user addresses: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<List<AddressResponse>>(
                    "An error occurred while retrieving addresses"
                );
                return StatusCode(500, errorResponse);
            }
        }
    }
}
