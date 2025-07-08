using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.User.Voucher;

namespace PlatformFlower.Controllers.UserVoucher
{
    [ApiController]
    [Route("api/user/vouchers")]
    [Authorize]
    public class UserVoucherController : ControllerBase
    {
        private readonly IUserVoucherService _userVoucherService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public UserVoucherController(
            IUserVoucherService userVoucherService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _userVoucherService = userVoucherService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<VoucherResponse>>>> GetUserVouchers()
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<VoucherResponse>>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} getting their vouchers");

                var result = await _userVoucherService.GetUserVouchersAsync(userId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} vouchers successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user vouchers: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<List<VoucherResponse>>(
                    "An error occurred while retrieving vouchers"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("validate/{voucherCode}")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> ValidateVoucherCode(string voucherCode)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} validating voucher code: {voucherCode}");

                var result = await _userVoucherService.ValidateVoucherCodeAsync(voucherCode, userId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        $"Voucher code '{voucherCode}' not found or not available for your account"
                    );
                    return NotFound(notFoundResponse);
                }

                string message;
                if (!result.IsActive)
                {
                    if (result.IsExpired)
                    {
                        message = "Voucher code is valid but has expired";
                    }
                    else if (result.Status != "active")
                    {
                        message = "Voucher code is valid but is currently inactive";
                    }
                    else if (result.RemainingCount <= 0)
                    {
                        message = "Voucher code is valid but has no remaining uses";
                    }
                    else
                    {
                        message = "Voucher code is valid but cannot be used at this time";
                    }
                }
                else
                {
                    message = "Voucher code is valid and can be used";
                }

                var response = _responseService.CreateSuccessResponse(result, message);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating voucher code {voucherCode}: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while validating voucher code"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("{userVoucherStatusId}")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> GetUserVoucherById(int userVoucherStatusId)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"User {userId} getting voucher {userVoucherStatusId}");

                var result = await _userVoucherService.GetUserVoucherByIdAsync(userVoucherStatusId, userId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        $"Voucher with ID {userVoucherStatusId} not found or not accessible"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(result, "Voucher retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher {userVoucherStatusId}: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while retrieving voucher"
                );
                return StatusCode(500, errorResponse);
            }
        }
    }
}
