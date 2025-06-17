using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformFlower.Models;
using PlatformFlower.Models.DTOs.Seller;
using PlatformFlower.Services.Common.Logging;
using PlatformFlower.Services.Common.Response;
using PlatformFlower.Services.Seller.Profile;

namespace PlatformFlower.Controllers.ProfileSeller
{
    [ApiController]
    [Route("api/seller")]
    public class PublicSellerController : ControllerBase
    {
        private readonly ISellerProfileService _sellerService;
        private readonly IResponseService _responseService;
        private readonly IAppLogger _logger;

        public PublicSellerController(
            ISellerProfileService sellerService,
            IResponseService responseService,
            IAppLogger logger)
        {
            _sellerService = sellerService;
            _responseService = responseService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<SellerProfileResponse>>> GetSellerById(int id)
        {
            try
            {
                _logger.LogInformation($"Getting seller by ID: {id}");

                var seller = await _sellerService.GetSellerByIdAsync(id);
                
                if (seller == null)
                {
                    var notFoundResponse = _responseService.CreateErrorResponse<SellerProfileResponse>("Seller not found");
                    return NotFound(notFoundResponse);
                }

                var response = _responseService.CreateSuccessResponse(seller, "Seller retrieved successfully");
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning($"Seller retrieval failed - validation error: {ex.Message}");
                var response = _responseService.CreateErrorResponse<SellerProfileResponse>(ex.Message);
                return BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error during seller retrieval: {ex.Message}", ex);
                var response = _responseService.CreateErrorResponse<SellerProfileResponse>(
                    "An unexpected error occurred during seller retrieval"
                );
                return StatusCode(500, response);
            }
        }
    }
}
