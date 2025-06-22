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
    public class ManageAddressController : ControllerBase
    {
        private readonly IAddressManagementService _addressManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public ManageAddressController(
            IAddressManagementService addressManagementService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _addressManagementService = addressManagementService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<AddressResponse>>> ManageAddress([FromForm] CreateAddressRequest request)
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

                string operationType = request.AddressId == null || request.AddressId == 0
                    ? "CREATE"
                    : request.IsDeleted
                        ? "DELETE"
                        : "UPDATE";

                _logger.LogInformation($"User managing address - Operation: {operationType}, Description: {request.Description}, AddressId: {request.AddressId}, UserId: {userId}");

                var result = await _addressManagementService.ManageAddressAsync(request, userId);

                string operation;
                string message;

                if (request.AddressId == null || request.AddressId == 0)
                {
                    operation = "created";
                    message = "Address created successfully";
                }
                else if (request.IsDeleted)
                {
                    operation = "deleted";
                    message = "Address deleted successfully";
                }
                else
                {
                    operation = "updated";
                    message = "Address updated successfully";
                }

                var response = _responseService.CreateSuccessResponse(result, message);

                _logger.LogInformation($"Address {operation} successfully - ID: {result.AddressId}, Description: {result.Description}, UserId: {userId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Address management validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<AddressResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Address management business logic error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<AddressResponse>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error managing address: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<AddressResponse>(
                    "An error occurred while managing the address"
                );
                return StatusCode(500, response);
            }
        }
    }
}
