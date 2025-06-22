using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Admin.VoucherManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;

namespace PlatformFlower.Controllers.AdminVoucherManagement
{
    [ApiController]
    [Route("api/admin/vouchers")]
    [Authorize(Roles = "admin")]
    public class AdminVoucherController : ControllerBase
    {
        private readonly IAdminVoucherService _adminVoucherService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public AdminVoucherController(
            IAdminVoucherService adminVoucherService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _adminVoucherService = adminVoucherService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> ManageVoucher([FromForm] CreateVoucherRequest request)
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

                string operationType;
                if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
                {
                    operationType = "create";
                }
                else if (request.IsDeleted)
                {
                    operationType = "delete";
                }
                else
                {
                    operationType = "update";
                }

                _logger.LogInformation($"Admin managing voucher - Operation: {operationType}, VoucherCode: {request.VoucherCode}, AdminId: {userId}");

                var result = await _adminVoucherService.ManageVoucherAsync(request);

                string operation;
                string message;

                if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
                {
                    operation = "created";
                    message = "Voucher created successfully for all active users";
                }
                else if (request.IsDeleted)
                {
                    operation = "deleted";
                    message = "Voucher deleted successfully (soft delete)";
                }
                else
                {
                    operation = "updated";
                    message = "Voucher updated successfully";
                }

                _logger.LogInformation($"Admin voucher {operation} - VoucherCode: {result.VoucherCode}, ID: {result.UserVoucherStatusId}");

                var response = _responseService.CreateSuccessResponse(result, message);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Validation error in admin voucher management: {ex.Message}");
                var badRequestResponse = _responseService.CreateErrorResponse<VoucherResponse>(ex.Message);
                return BadRequest(badRequestResponse);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Business logic error in admin voucher management: {ex.Message}");
                var conflictResponse = _responseService.CreateErrorResponse<VoucherResponse>(ex.Message);
                return Conflict(conflictResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in admin voucher management: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An unexpected error occurred while managing the voucher"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<VoucherResponse>>>> GetAllVouchers()
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

                _logger.LogInformation($"Admin {userId} getting all vouchers");

                var result = await _adminVoucherService.GetAllVouchersAsync();

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} vouchers successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting all vouchers for admin: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<List<VoucherResponse>>(
                    "An error occurred while retrieving vouchers"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("{voucherStatusId}")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> GetVoucherById(int voucherStatusId)
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

                _logger.LogInformation($"Admin {userId} getting voucher {voucherStatusId}");

                var result = await _adminVoucherService.GetVoucherByIdAsync(voucherStatusId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        $"Voucher with ID {voucherStatusId} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(result, "Voucher retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher {voucherStatusId} for admin: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while retrieving the voucher"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("code/{voucherCode}")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> GetVoucherByCode(string voucherCode)
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

                _logger.LogInformation($"Admin {userId} getting voucher by code: {voucherCode}");

                var result = await _adminVoucherService.GetVoucherByCodeAsync(voucherCode);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        $"Voucher with code '{voucherCode}' not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(result, "Voucher retrieved successfully");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher by code {voucherCode} for admin: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while retrieving the voucher"
                );
                return StatusCode(500, errorResponse);
            }
        }

        [HttpGet("stats/{voucherCode}")]
        public async Task<ActionResult<ApiResponse<VoucherStatsResponse>>> GetVoucherStats(string voucherCode)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherStatsResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                _logger.LogInformation($"Admin {userId} getting voucher stats for code: {voucherCode}");

                var result = await _adminVoucherService.GetVoucherStatsAsync(voucherCode);

                var response = _responseService.CreateSuccessResponse(result, "Voucher statistics retrieved successfully");
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Voucher stats error: {ex.Message}");
                var notFoundResponse = _responseService.CreateErrorResponse<VoucherStatsResponse>(ex.Message);
                return NotFound(notFoundResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher stats for {voucherCode}: {ex.Message}", ex);
                var errorResponse = _responseService.CreateErrorResponse<VoucherStatsResponse>(
                    "An error occurred while retrieving voucher statistics"
                );
                return StatusCode(500, errorResponse);
            }
        }
    }
}
