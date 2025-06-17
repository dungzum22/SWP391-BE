using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Flower;
using PlatformFlower.Services.Admin.FlowerManagement;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using System.Security.Claims;

namespace PlatformFlower.Controllers.SellerFlowerManagement
{
    [ApiController]
    [Route("api/seller/flowers")]
    [Authorize(Roles = "seller")]
    public class ManageFlowerController : ControllerBase
    {
        private readonly IFlowerManagementService _flowerManagementService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;
        private readonly FlowershopContext _context;

        public ManageFlowerController(
            IFlowerManagementService flowerManagementService,
            IResponseService responseService,
            IAppLogger logger,
            FlowershopContext context)
        {
            _flowerManagementService = flowerManagementService;
            _responseService = responseService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("manage")]
        public async Task<ActionResult<ApiResponse<FlowerResponse>>> ManageFlower([FromForm] CreateFlowerRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("user_id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("User ID not found in token claims");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        "User ID not found in authentication token"
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (seller == null)
                {
                    _logger.LogWarning($"Seller record not found for user ID: {userId}");
                    var unauthorizedResponse = _responseService.CreateErrorResponse<FlowerResponse>(
                        "Seller profile not found. Please register as a seller first."
                    );
                    return Unauthorized(unauthorizedResponse);
                }

                int sellerId = seller.SellerId;

                string operationType = request.FlowerId == null || request.FlowerId == 0
                    ? "CREATE"
                    : request.IsDeleted
                        ? "DELETE"
                        : "UPDATE";

                _logger.LogInformation($"Seller managing flower - Operation: {operationType}, FlowerName: {request.FlowerName}, FlowerId: {request.FlowerId}, SellerId: {sellerId}");

                var result = await _flowerManagementService.ManageFlowerAsync(request, sellerId);

                string operation;
                string message;

                if (request.FlowerId == null || request.FlowerId == 0)
                {
                    operation = "created";
                    message = result.CreatedAt < result.UpdatedAt
                        ? "Flower reactivated successfully (was previously inactive)"
                        : "Flower created successfully";
                }
                else if (request.IsDeleted)
                {
                    operation = "deleted";
                    message = "Flower deleted successfully (set to inactive and marked as deleted)";
                }
                else
                {
                    operation = "updated";
                    message = "Flower updated successfully";
                }

                var response = _responseService.CreateSuccessResponse(result, message);

                _logger.LogInformation($"Flower {operation} successfully - ID: {result.FlowerId}, Name: {result.FlowerName}, Status: {result.Status}, IsDeleted: {result.IsDeleted}");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Flower management validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<FlowerResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning($"Flower management authorization error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<FlowerResponse>(ex.Message);
                return StatusCode(403, response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Flower management business logic error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<FlowerResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in flower management: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<FlowerResponse>("An unexpected error occurred while managing the flower");
                return StatusCode(500, response);
            }
        }
    }
}
