using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Voucher;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Seller.VoucherManagement;

namespace PlatformFlower.Controllers.SellerVoucherManagement
{
    [ApiController]
    [Route("api/seller/vouchers")]
    [Authorize(Roles = "seller")]
    public class ManageVoucherController : ControllerBase
    {
        private readonly IVoucherManagementService _voucherManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;
        private readonly FlowershopContext _context;

        public ManageVoucherController(
            IVoucherManagementService voucherManagementService,
            IResponseService responseService,
            IAppLogger logger,
            FlowershopContext context)
        {
            _voucherManagementService = voucherManagementService;
            _responseService = responseService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<VoucherResponse>>> ManageVoucher([FromBody] CreateVoucherRequest request)
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

                var seller = await GetSellerByUserIdAsync(userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller not found for user {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        "User is not a seller"
                    );
                    return Unauthorized(unauthorizedResponse);
                }
                int sellerId = seller.SellerId;

                string operationType;
                if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
                {
                    operationType = "CREATE";
                }
                else if (request.IsDeleted)
                {
                    operationType = "DELETE";
                }
                else
                {
                    operationType = "UPDATE";
                }

                _logger.LogInformation($"Seller managing voucher - Operation: {operationType}, VoucherCode: {request.VoucherCode}, SellerId: {sellerId}");

                var result = await _voucherManagementService.ManageVoucherAsync(request, sellerId);

                string operation;
                string message;

                if (request.UserVoucherStatusId == null || request.UserVoucherStatusId == 0)
                {
                    operation = "created";
                    message = "Voucher created successfully";
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

                var response = _responseService.CreateSuccessResponse(result, message);

                _logger.LogInformation($"Voucher {operation} successfully - ID: {result.UserVoucherStatusId}, Code: {result.VoucherCode}, SellerId: {sellerId}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Voucher management validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<VoucherResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Voucher management operation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<VoucherResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Voucher management authorization error: {ex.Message}");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during voucher management: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while processing the request"
                );
                return StatusCode(500, response);
            }
        }

        [HttpGet("my-vouchers")]
        public async Task<ActionResult<ApiResponse<List<VoucherResponse>>>> GetMyVouchers()
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

                var seller = await GetSellerByUserIdAsync(userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller not found for user {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<List<VoucherResponse>>(
                        "User is not a seller"
                    );
                    return Unauthorized(unauthorizedResponse);
                }
                int sellerId = seller.SellerId;

                _logger.LogInformation($"Seller {sellerId} getting all vouchers");

                var result = await _voucherManagementService.GetAllVouchersAsync(sellerId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    $"Retrieved {result.Count} vouchers successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting seller vouchers: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<List<VoucherResponse>>(
                    "An error occurred while retrieving vouchers"
                );
                return StatusCode(500, response);
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

                var seller = await GetSellerByUserIdAsync(userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller not found for user {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        "User is not a seller"
                    );
                    return Unauthorized(unauthorizedResponse);
                }
                int sellerId = seller.SellerId;

                _logger.LogInformation($"Seller {sellerId} getting voucher by ID: {voucherStatusId}");

                var result = await _voucherManagementService.GetVoucherByIdAsync(voucherStatusId, sellerId);

                if (result == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<VoucherResponse>(
                        $"Voucher with ID {voucherStatusId} not found"
                    );
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Voucher retrieved successfully"
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher by ID {voucherStatusId}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<VoucherResponse>(
                    "An error occurred while retrieving voucher"
                );
                return StatusCode(500, response);
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

                var seller = await GetSellerByUserIdAsync(userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller not found for user {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<VoucherStatsResponse>(
                        "User is not a seller"
                    );
                    return Unauthorized(unauthorizedResponse);
                }
                int sellerId = seller.SellerId;

                _logger.LogInformation($"Seller {sellerId} getting stats for voucher: {voucherCode}");

                var result = await _voucherManagementService.GetVoucherStatsAsync(voucherCode, sellerId);

                var response = _responseService.CreateSuccessResponse(
                    result,
                    "Voucher statistics retrieved successfully"
                );

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Voucher stats error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<VoucherStatsResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting voucher stats for {voucherCode}: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<VoucherStatsResponse>(
                    "An error occurred while retrieving voucher statistics"
                );
                return StatusCode(500, response);
            }
        }

        private async Task<Entities.Seller?> GetSellerByUserIdAsync(int userId)
        {
            return await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
    }
}
