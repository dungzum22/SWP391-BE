using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Seller;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Common.Validation;
using PlatformFlower.Services.Seller.Profile;

namespace PlatformFlower.Controllers.ProfileSeller
{
    [ApiController]
    [Route("api/seller")]
    [Authorize]
    public class UpdateSellerController : ControllerBase
    {
        private readonly ISellerProfileService _sellerService;
        private readonly IResponseService _responseService;
        private readonly IValidationService _validationService;
        private readonly IAppLogger _logger;

        public UpdateSellerController(
            ISellerProfileService sellerService,
            IResponseService responseService,
            IValidationService validationService,
            IAppLogger logger)
        {
            _sellerService = sellerService;
            _responseService = responseService;
            _validationService = validationService;
            _logger = logger;
        }

        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<SellerProfileResponse>>> UpsertSellerProfile([FromBody] UpdateSellerRequest sellerDto)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("Invalid user ID in JWT token");
                    var badRequestResponse = _responseService.CreateErrorResponse<SellerProfileResponse>("Invalid token");
                    return BadRequest(badRequestResponse);
                }

                _logger.LogInformation($"Seller profile upsert attempt for user ID: {userId}");

                if (!ModelState.IsValid)
                {
                    var validationResponse = _validationService.ValidateModelState<SellerProfileResponse>(ModelState);
                    return BadRequest(validationResponse);
                }

                var isExistingSeller = await _sellerService.IsUserSellerAsync(userId);
                var sellerResponse = await _sellerService.UpsertSellerAsync(userId, sellerDto);

                var message = isExistingSeller
                    ? "Seller profile updated successfully!"
                    : "Seller registration successful. You are now a seller!";

                var response = _responseService.CreateSuccessResponse(sellerResponse, message);

                _logger.LogInformation($"Seller profile upserted successfully for user ID: {userId}, seller ID: {sellerResponse.SellerId}");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Seller profile upsert failed - business rule violation: {ex.Message}");
                var response = _responseService.CreateErrorResponse<SellerProfileResponse>(ex.Message);
                return Conflict(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller profile upsert: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerProfileResponse>(
                    "An unexpected error occurred during seller profile operation"
                );
                return StatusCode(500, response);
            }
        }
    }
}
